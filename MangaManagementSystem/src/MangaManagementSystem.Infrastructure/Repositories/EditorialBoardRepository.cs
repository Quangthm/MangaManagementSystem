using System.Data;
using System.Data.Common;
using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.Application.Features.EditorialBoard.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Repositories;

public sealed class EditorialBoardRepository : IEditorialBoardRepository
{
    private readonly MangaManagementDbContext _dbContext;

    public EditorialBoardRepository(MangaManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EditorialDashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var proposalReviewCount = await ExecuteScalarIntAsync(
            connection,
            """
            SELECT COUNT(1)
            FROM manga.SeriesProposal
            WHERE status_code IN (
                N'UNDER_EDITORIAL_REVIEW',
                N'UNDER_BOARD_REVIEW'
            );
            """,
            cancellationToken);

        var openPollCount = await ExecuteScalarIntAsync(
            connection,
            """
            SELECT COUNT(1)
            FROM manga.SeriesBoardPoll
            WHERE poll_status_code = N'OPEN';
            """,
            cancellationToken);

        var awaitingDecisionCount = await ExecuteScalarIntAsync(
            connection,
            """
            SELECT COUNT(1)
            FROM manga.vw_SeriesBoardPollVoteSummary
            WHERE poll_status_code = N'CLOSED'
              AND is_applicable = 1;
            """,
            cancellationToken);

        var recentProposals = await ReadRecentProposalsAsync(connection, cancellationToken);
        var openPolls = await ReadOpenPollsAsync(connection, cancellationToken);
        var decisions = await ReadDecisionQueueAsync(connection, cancellationToken);

        return new EditorialDashboardDto(
            proposalReviewCount,
            openPollCount,
            awaitingDecisionCount,
            recentProposals,
            openPolls,
            decisions);
    }

    private static async Task<int> ExecuteScalarIntAsync(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is null || result == DBNull.Value
            ? 0
            : Convert.ToInt32(result);
    }

    private static async Task<IReadOnlyList<EditorialProposalReviewRowDto>> ReadRecentProposalsAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT TOP (4)
                sp.series_proposal_id,
                sp.series_id,
                s.slug,
                sp.proposal_title,
                u.display_name,
                sp.genre_snapshot,
                sp.status_code
            FROM manga.SeriesProposal sp
            INNER JOIN manga.Series s
                ON s.series_id = sp.series_id
            INNER JOIN auth.Users u
                ON u.user_id = sp.submitted_by_user_id
            WHERE sp.status_code IN (
                N'UNDER_EDITORIAL_REVIEW',
                N'UNDER_BOARD_REVIEW',
                N'REVISION_REQUESTED',
                N'APPROVED',
                N'CANCELLED',
                N'WITHDRAWN'
            )
            ORDER BY sp.submitted_at_utc DESC;
            """;

        var rows = new List<EditorialProposalReviewRowDto>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new EditorialProposalReviewRowDto(
                ProposalId: reader.GetGuid(0),
                SeriesId: reader.GetGuid(1),
                Code: GetStringOrDefault(reader, 2, "N/A"),
                Title: GetStringOrDefault(reader, 3, "Untitled Proposal"),
                Author: GetStringOrDefault(reader, 4, "Unknown Author"),
                Genre: GetStringOrDefault(reader, 5, "Unknown Genre"),
                Status: MapProposalStatus(GetStringOrDefault(reader, 6, "UNKNOWN"))));
        }

        return rows;
    }

    private static async Task<IReadOnlyList<EditorialOpenPollRowDto>> ReadOpenPollsAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT TOP (3)
                p.series_board_poll_id,
                p.series_id,
                s.slug,
                s.title,
                p.poll_type_code,
                p.poll_status_code,
                ISNULL(v.approve_count, 0) AS approve_count,
                ISNULL(v.reject_count, 0) AS reject_count,
                ISNULL(v.abstain_count, 0) AS abstain_count,
                ISNULL(v.total_vote_count, 0) AS total_vote_count
            FROM manga.SeriesBoardPoll p
            INNER JOIN manga.Series s
                ON s.series_id = p.series_id
            LEFT JOIN manga.vw_SeriesBoardPollVoteSummary v
                ON v.series_board_poll_id = p.series_board_poll_id
            WHERE p.poll_status_code = N'OPEN'
            ORDER BY p.started_at_utc DESC;
            """;

        var rows = new List<EditorialOpenPollRowDto>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var pollTypeCode = GetStringOrDefault(reader, 4, "UNKNOWN");
            var seriesTitle = GetStringOrDefault(reader, 3, "Untitled Series");

            rows.Add(new EditorialOpenPollRowDto(
                PollId: reader.GetGuid(0),
                SeriesId: reader.GetGuid(1),
                Code: GetStringOrDefault(reader, 2, "N/A"),
                Name: $"{MapPollType(pollTypeCode)} — {seriesTitle}",
                ApproveVotes: ToInt32(reader, 6),
                RejectVotes: ToInt32(reader, 7),
                AbstainVotes: ToInt32(reader, 8),
                TotalVotes: ToInt32(reader, 9),
                Status: MapPollStatus(GetStringOrDefault(reader, 5, "UNKNOWN"))));
        }

        return rows;
    }

    private static async Task<IReadOnlyList<EditorialDecisionQueueRowDto>> ReadDecisionQueueAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT TOP (4)
                v.series_board_poll_id,
                v.series_id,
                s.slug,
                v.series_title,
                ISNULL(v.approve_count, 0) AS approve_count,
                ISNULL(v.reject_count, 0) AS reject_count,
                ISNULL(v.abstain_count, 0) AS abstain_count,
                ISNULL(v.total_vote_count, 0) AS total_vote_count,
                ISNULL(v.computed_result_code, N'PENDING') AS computed_result_code
            FROM manga.vw_SeriesBoardPollVoteSummary v
            INNER JOIN manga.Series s
                ON s.series_id = v.series_id
            WHERE v.poll_status_code = N'CLOSED'
              AND v.is_applicable = 1
            ORDER BY v.started_at_utc DESC;
            """;

        var rows = new List<EditorialDecisionQueueRowDto>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new EditorialDecisionQueueRowDto(
                PollId: reader.GetGuid(0),
                SeriesId: reader.GetGuid(1),
                Code: GetStringOrDefault(reader, 2, "N/A"),
                Title: GetStringOrDefault(reader, 3, "Untitled Series"),
                ApproveVotes: ToInt32(reader, 4),
                RejectVotes: ToInt32(reader, 5),
                AbstainVotes: ToInt32(reader, 6),
                TotalVotes: ToInt32(reader, 7),
                ComputedResultCode: MapDecisionResult(GetStringOrDefault(reader, 8, "PENDING"))));
        }

        return rows;
    }

    private static int ToInt32(DbDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? 0
            : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static string GetStringOrDefault(
        DbDataReader reader,
        int ordinal,
        string fallback)
    {
        return reader.IsDBNull(ordinal)
            ? fallback
            : reader.GetString(ordinal);
    }

    private static string MapProposalStatus(string statusCode)
    {
        return statusCode switch
        {
            "UNDER_EDITORIAL_REVIEW" => "In Review",
            "UNDER_BOARD_REVIEW" => "Board Review",
            "REVISION_REQUESTED" => "Revision Requested",
            "APPROVED" => "Approved",
            "CANCELLED" => "Cancelled",
            "WITHDRAWN" => "Withdrawn",
            _ => statusCode
        };
    }

    private static string MapPollStatus(string statusCode)
    {
        return statusCode switch
        {
            "OPEN" => "Open",
            "CLOSED" => "Closed",
            "CANCELLED" => "Cancelled",
            _ => statusCode
        };
    }

    private static string MapPollType(string pollTypeCode)
    {
        return pollTypeCode switch
        {
            "START_SERIALIZATION" => "Serialization Approval",
            "CANCEL_SERIALIZATION" => "Cancel Serialization",
            _ => pollTypeCode
        };
    }

    private static string MapDecisionResult(string resultCode)
    {
        return resultCode switch
        {
            "APPROVED" => "Approved",
            "REJECTED" => "Rejected",
            "NO_DECISION" => "No Decision",
            "PENDING" => "Voting in Progress",
            "INVALIDATED" => "Cancelled",
            _ => resultCode
        };
    }
}