using System.Data;
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
            WHERE status_code = N'UNDER_EDITORIAL_REVIEW';
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
            FROM manga.SeriesBoardPoll
            WHERE poll_status_code = N'CLOSED';
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
        System.Data.Common.DbConnection connection,
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
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT TOP (4)
                sp.series_proposal_id,
                s.series_id,
                s.series_code,
                s.title,
                u.username AS author_name,
                s.genre,
                sp.status_code
            FROM manga.SeriesProposal sp
            INNER JOIN manga.Series s ON s.series_id = sp.series_id
            INNER JOIN auth.Users u ON u.user_id = sp.submitted_by_user_id
            WHERE sp.status_code IN (
                N'UNDER_EDITORIAL_REVIEW',
                N'APPROVED',
                N'REJECTED',
                N'REVISION_REQUESTED'
            )
            ORDER BY sp.submitted_at_utc DESC;
            """;

        var rows = new List<EditorialProposalReviewRowDto>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new EditorialProposalReviewRowDto(
                ProposalId: reader.GetInt64(0),
                SeriesId: reader.GetInt64(1),
                Code: reader.GetString(2),
                Title: reader.GetString(3),
                Author: reader.GetString(4),
                Genre: reader.IsDBNull(5) ? "Unknown" : reader.GetString(5),
                Status: MapProposalStatus(reader.GetString(6))));
        }

        return rows;
    }

    private static async Task<IReadOnlyList<EditorialOpenPollRowDto>> ReadOpenPollsAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT TOP (3)
                p.series_board_poll_id,
                s.series_id,
                s.series_code,
                s.title,
                p.poll_type_code,
                p.poll_status_code,
                ISNULL(v.approve_count, 0) AS approve_count,
                ISNULL(v.reject_count, 0) AS reject_count,
                ISNULL(v.abstain_count, 0) AS abstain_count,
                ISNULL(v.total_votes, 0) AS total_votes
            FROM manga.SeriesBoardPoll p
            INNER JOIN manga.Series s ON s.series_id = p.series_id
            LEFT JOIN manga.vw_SeriesBoardPollVoteSummary v
                ON v.series_board_poll_id = p.series_board_poll_id
            WHERE p.poll_status_code = N'OPEN'
            ORDER BY p.opened_at_utc DESC;
            """;

        var rows = new List<EditorialOpenPollRowDto>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var pollTypeCode = reader.GetString(4);
            var seriesTitle = reader.GetString(3);

            rows.Add(new EditorialOpenPollRowDto(
                PollId: reader.GetInt64(0),
                SeriesId: reader.GetInt64(1),
                Code: reader.GetString(2),
                Name: $"{MapPollType(pollTypeCode)} — {seriesTitle}",
                ApproveVotes: reader.GetInt32(6),
                RejectVotes: reader.GetInt32(7),
                AbstainVotes: reader.GetInt32(8),
                TotalVotes: reader.GetInt32(9),
                Status: MapPollStatus(reader.GetString(5))));
        }

        return rows;
    }

    private static async Task<IReadOnlyList<EditorialDecisionQueueRowDto>> ReadDecisionQueueAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT TOP (4)
                p.series_board_poll_id,
                s.series_id,
                s.series_code,
                s.title,
                ISNULL(v.approve_count, 0) AS approve_count,
                ISNULL(v.reject_count, 0) AS reject_count,
                ISNULL(v.abstain_count, 0) AS abstain_count,
                ISNULL(v.total_votes, 0) AS total_votes,
                ISNULL(v.computed_result_code, N'PENDING') AS computed_result_code
            FROM manga.SeriesBoardPoll p
            INNER JOIN manga.Series s ON s.series_id = p.series_id
            LEFT JOIN manga.vw_SeriesBoardPollVoteSummary v
                ON v.series_board_poll_id = p.series_board_poll_id
            WHERE p.poll_status_code = N'CLOSED'
            ORDER BY p.closed_at_utc DESC;
            """;

        var rows = new List<EditorialDecisionQueueRowDto>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new EditorialDecisionQueueRowDto(
                PollId: reader.GetInt64(0),
                SeriesId: reader.GetInt64(1),
                Code: reader.GetString(2),
                Title: reader.GetString(3),
                ApproveVotes: reader.GetInt32(4),
                RejectVotes: reader.GetInt32(5),
                AbstainVotes: reader.GetInt32(6),
                TotalVotes: reader.GetInt32(7),
                ComputedResultCode: reader.GetString(8)));
        }

        return rows;
    }

    private static string MapProposalStatus(string statusCode)
    {
        return statusCode switch
        {
            "UNDER_EDITORIAL_REVIEW" => "In Review",
            "APPROVED" => "Approved",
            "REJECTED" => "Rejected",
            "REVISION_REQUESTED" => "Revision Requested",
            "PROPOSAL_DRAFT" => "Draft",
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
}