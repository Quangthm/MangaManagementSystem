using System.Data;
using System.Data.Common;
using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;
using MangaManagementSystem.Application.Features.EditorialBoard.Repositories;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


namespace MangaManagementSystem.Infrastructure.Repositories;

public sealed class EditorialBoardRepository : IEditorialBoardRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EditorialBoardRepository(ApplicationDbContext dbContext)
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

    public async Task<IReadOnlyList<EditorialBoardPollDto>> GetOpenPollsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT
                p.series_board_poll_id,
                p.series_id,
                s.slug,
                s.title,
                p.poll_type_code,
                p.poll_status_code,
                p.poll_reason,
                p.board_publication_frequency_code,
                p.started_at_utc,
                p.ends_at_utc,
                ISNULL(vs.approve_count, 0) AS approve_count,
                ISNULL(vs.reject_count, 0) AS reject_count,
                ISNULL(vs.abstain_count, 0) AS abstain_count,
                ISNULL(vs.total_vote_count, 0) AS total_vote_count,
                ISNULL(vs.computed_result_code, N'PENDING') AS computed_result_code,
                my_vote.choice_code AS current_user_choice_code,
                my_vote.vote_reason AS current_user_vote_reason
            FROM manga.SeriesBoardPoll p
            INNER JOIN manga.Series s
                ON s.series_id = p.series_id
            LEFT JOIN manga.vw_SeriesBoardPollVoteSummary vs
                ON vs.series_board_poll_id = p.series_board_poll_id
            LEFT JOIN manga.SeriesBoardVote my_vote
                ON my_vote.series_board_poll_id = p.series_board_poll_id
               AND my_vote.user_id = @currentUserId
            WHERE p.poll_status_code = N'OPEN'
            ORDER BY p.started_at_utc DESC;
            """;

        AddGuidParameter(command, "@currentUserId", currentUserId);

        var rows = new List<EditorialBoardPollDto>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var pollTypeCode = GetStringOrDefault(reader, 4, "UNKNOWN");
            var seriesTitle = GetStringOrDefault(reader, 3, "Untitled Series");

            rows.Add(new EditorialBoardPollDto(
                PollId: reader.GetGuid(0),
                SeriesId: reader.GetGuid(1),
                Code: GetStringOrDefault(reader, 2, "N/A"),
                SeriesTitle: seriesTitle,
                PollName: $"{MapPollType(pollTypeCode)} â€” {seriesTitle}",
                PollTypeCode: pollTypeCode,
                PollStatusCode: GetStringOrDefault(reader, 5, "OPEN"),
                PollReason: GetStringOrDefault(reader, 6, string.Empty),
                PublicationFrequencyCode: reader.IsDBNull(7) ? null : reader.GetString(7),
                StartedAtUtc: reader.GetDateTime(8),
                EndsAtUtc: reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                ApproveVotes: ToInt32(reader, 10),
                RejectVotes: ToInt32(reader, 11),
                AbstainVotes: ToInt32(reader, 12),
                TotalVotes: ToInt32(reader, 13),
                ComputedResultCode: GetStringOrDefault(reader, 14, "PENDING"),
                CurrentUserChoiceCode: reader.IsDBNull(15) ? null : reader.GetString(15),
                CurrentUserVoteReason: reader.IsDBNull(16) ? null : reader.GetString(16)));
        }

        return rows;
    }

    public async Task<OpenSeriesBoardPollResultDto> OpenPollAsync(
        OpenSeriesBoardPollRequestDto request,
        Guid chiefUserId,
        CancellationToken cancellationToken)
    {
        ValidateOpenPollRequest(request);

        var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var proposalInfo = await GetProposalForPollAsync(
                connection,
                transaction,
                request.ProposalId,
                cancellationToken);

            if (proposalInfo is null)
            {
                throw new InvalidOperationException(
                    "Proposal not found or cannot be opened for board poll.");
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText =
                """
                INSERT INTO manga.SeriesBoardPoll
                (
                    series_id,
                    poll_type_code,
                    poll_reason,
                    poll_status_code,
                    board_publication_frequency_code,
                    created_by_user_id,
                    started_at_utc,
                    ends_at_utc
                )
                OUTPUT inserted.series_board_poll_id
                VALUES
                (
                    @seriesId,
                    @pollTypeCode,
                    @pollReason,
                    N'OPEN',
                    @publicationFrequencyCode,
                    @chiefUserId,
                    SYSUTCDATETIME(),
                    @endsAtUtc
                );
                """;

            AddGuidParameter(command, "@seriesId", proposalInfo.SeriesId);
            AddStringParameter(command, "@pollTypeCode", request.PollTypeCode, 50);
            AddStringParameter(command, "@pollReason", request.PollReason, -1);
            AddNullableStringParameter(
                command,
                "@publicationFrequencyCode",
                request.PublicationFrequencyCode,
                50);
            AddGuidParameter(command, "@chiefUserId", chiefUserId);
            AddNullableDateTimeParameter(command, "@endsAtUtc", request.EndsAtUtc);

            var pollIdObj = await command.ExecuteScalarAsync(cancellationToken);

            if (pollIdObj is not Guid pollId)
            {
                throw new InvalidOperationException("Could not create board poll.");
            }

            await UpdateProposalAndSeriesToBoardReviewAsync(
                connection,
                transaction,
                request.ProposalId,
                proposalInfo.SeriesId,
                chiefUserId,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new OpenSeriesBoardPollResultDto(
                PollId: pollId,
                SeriesId: proposalInfo.SeriesId,
                ProposalId: request.ProposalId,
                PollStatusCode: "OPEN");
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // Transaction was already completed by SQL Server/provider.
            }

            throw;
        }
    }

    public async Task<CastSeriesBoardVoteResultDto> CastVoteAsync(
        CastSeriesBoardVoteRequestDto request,
        Guid voterUserId,
        CancellationToken cancellationToken)
    {
        ValidateVoteRequest(request);

        var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            SET XACT_ABORT ON;

            IF NOT EXISTS (
                SELECT 1
                FROM manga.SeriesBoardPoll
                WHERE series_board_poll_id = @pollId
                  AND poll_status_code = N'OPEN'
                  AND (
                      ends_at_utc IS NULL
                      OR ends_at_utc > SYSUTCDATETIME()
                  )
            )
            BEGIN
                THROW 58601, 'Poll is not open or has expired.', 1;
            END;

            DECLARE @result TABLE
            (
                series_board_vote_id UNIQUEIDENTIFIER,
                choice_code NVARCHAR(50),
                vote_reason NVARCHAR(500),
                voted_at_utc DATETIME2(0)
            );

            UPDATE manga.SeriesBoardVote
            SET choice_code = @choiceCode,
                vote_reason = @voteReason,
                voted_at_utc = SYSUTCDATETIME()
            OUTPUT
                inserted.series_board_vote_id,
                inserted.choice_code,
                inserted.vote_reason,
                inserted.voted_at_utc
            INTO @result
            WHERE series_board_poll_id = @pollId
              AND user_id = @voterUserId;

            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO manga.SeriesBoardVote
                (
                    series_board_poll_id,
                    user_id,
                    choice_code,
                    vote_reason,
                    voted_at_utc
                )
                OUTPUT
                    inserted.series_board_vote_id,
                    inserted.choice_code,
                    inserted.vote_reason,
                    inserted.voted_at_utc
                INTO @result
                VALUES
                (
                    @pollId,
                    @voterUserId,
                    @choiceCode,
                    @voteReason,
                    SYSUTCDATETIME()
                );
            END;

            SELECT
                series_board_vote_id,
                choice_code,
                vote_reason,
                voted_at_utc
            FROM @result;
            """;

        AddGuidParameter(command, "@pollId", request.PollId);
        AddGuidParameter(command, "@voterUserId", voterUserId);
        AddStringParameter(command, "@choiceCode", request.ChoiceCode, 50);
        AddNullableStringParameter(command, "@voteReason", request.VoteReason, 500);

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Could not save vote.");
            }

            return new CastSeriesBoardVoteResultDto(
                PollId: request.PollId,
                VoteId: reader.GetGuid(0),
                UserId: voterUserId,
                ChoiceCode: reader.GetString(1),
                VoteReason: reader.IsDBNull(2) ? null : reader.GetString(2),
                VotedAtUtc: reader.GetDateTime(3));
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 58601)
        {
            throw new InvalidOperationException("Poll is not open or has expired.", ex);
        }
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
                ISNULL(genres.genre_names, N'Unknown Genre') AS genre_display,
                sp.status_code
            FROM manga.SeriesProposal sp
            INNER JOIN manga.Series s
                ON s.series_id = sp.series_id
            INNER JOIN auth.Users u
                ON u.user_id = sp.submitted_by_user_id
            OUTER APPLY
            (
                SELECT
                    STRING_AGG(CONVERT(NVARCHAR(MAX), g.genre_name), N' / ') AS genre_names
                FROM manga.SeriesGenre sg
                INNER JOIN manga.Genre g
                    ON g.genre_id = sg.genre_id
                WHERE sg.series_id = s.series_id
            ) genres
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
                Name: $"{MapPollType(pollTypeCode)} â€” {seriesTitle}",
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

    private sealed record ProposalForPoll(Guid ProposalId, Guid SeriesId);

    private static async Task<ProposalForPoll?> GetProposalForPollAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid proposalId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            SELECT
                sp.series_proposal_id,
                sp.series_id
            FROM manga.SeriesProposal sp
            INNER JOIN manga.Series s
                ON s.series_id = sp.series_id
            WHERE sp.series_proposal_id = @proposalId
              AND sp.status_code IN (
                  N'UNDER_EDITORIAL_REVIEW',
                  N'UNDER_BOARD_REVIEW'
              )
              AND s.status_code IN (
                  N'UNDER_EDITORIAL_REVIEW',
                  N'UNDER_BOARD_REVIEW'
              );
            """;

        AddGuidParameter(command, "@proposalId", proposalId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ProposalForPoll(
            ProposalId: reader.GetGuid(0),
            SeriesId: reader.GetGuid(1));
    }

    private static async Task UpdateProposalAndSeriesToBoardReviewAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid proposalId,
        Guid seriesId,
        Guid chiefUserId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            UPDATE manga.SeriesProposal
            SET status_code = N'UNDER_BOARD_REVIEW',
                reviewed_by_user_id = COALESCE(reviewed_by_user_id, @chiefUserId),
                reviewed_at_utc = COALESCE(reviewed_at_utc, SYSUTCDATETIME())
            WHERE series_proposal_id = @proposalId
              AND status_code = N'UNDER_EDITORIAL_REVIEW';

            UPDATE manga.Series
            SET status_code = N'UNDER_BOARD_REVIEW',
                updated_at_utc = SYSUTCDATETIME(),
                updated_by_user_id = @chiefUserId
            WHERE series_id = @seriesId
              AND status_code = N'UNDER_EDITORIAL_REVIEW';
            """;

        AddGuidParameter(command, "@proposalId", proposalId);
        AddGuidParameter(command, "@seriesId", seriesId);
        AddGuidParameter(command, "@chiefUserId", chiefUserId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void ValidateOpenPollRequest(OpenSeriesBoardPollRequestDto request)
    {
        if (request.ProposalId == Guid.Empty)
        {
            throw new InvalidOperationException("ProposalId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PollReason))
        {
            throw new InvalidOperationException("Poll reason is required.");
        }

        if (request.PollTypeCode is not "START_SERIALIZATION" and not "CANCEL_SERIALIZATION")
        {
            throw new InvalidOperationException("Invalid poll type.");
        }

        if (request.PollTypeCode == "START_SERIALIZATION"
            && string.IsNullOrWhiteSpace(request.PublicationFrequencyCode))
        {
            throw new InvalidOperationException(
                "Publication frequency is required for START_SERIALIZATION poll.");
        }

        if (request.PollTypeCode == "CANCEL_SERIALIZATION"
            && !string.IsNullOrWhiteSpace(request.PublicationFrequencyCode))
        {
            throw new InvalidOperationException(
                "Publication frequency must be empty for CANCEL_SERIALIZATION poll.");
        }

        if (!string.IsNullOrWhiteSpace(request.PublicationFrequencyCode)
            && request.PublicationFrequencyCode is not "WEEKLY" and not "MONTHLY" and not "IRREGULAR")
        {
            throw new InvalidOperationException("Invalid publication frequency.");
        }

        if (request.EndsAtUtc is not null && request.EndsAtUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Poll deadline must be in the future.");
        }
    }

    private static void ValidateVoteRequest(CastSeriesBoardVoteRequestDto request)
    {
        if (request.PollId == Guid.Empty)
        {
            throw new InvalidOperationException("PollId is required.");
        }

        if (request.ChoiceCode is not "APPROVE" and not "REJECT" and not "ABSTAIN")
        {
            throw new InvalidOperationException("Invalid vote choice.");
        }

        if (request.ChoiceCode == "REJECT" && string.IsNullOrWhiteSpace(request.VoteReason))
        {
            throw new InvalidOperationException("Reject vote requires reason.");
        }
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

    private static void AddGuidParameter(
        DbCommand command,
        string name,
        Guid value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.Guid;
        parameter.Value = value;

        command.Parameters.Add(parameter);
    }

    private static void AddStringParameter(
        DbCommand command,
        string name,
        string value,
        int size)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.String;
        parameter.Value = value;

        if (size > 0)
        {
            parameter.Size = size;
        }

        command.Parameters.Add(parameter);
    }

    private static void AddNullableStringParameter(
        DbCommand command,
        string name,
        string? value,
        int size)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.String;
        parameter.Value = string.IsNullOrWhiteSpace(value)
            ? DBNull.Value
            : value;

        if (size > 0)
        {
            parameter.Size = size;
        }

        command.Parameters.Add(parameter);
    }

    private static void AddNullableDateTimeParameter(
        DbCommand command,
        string name,
        DateTime? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.DateTime2;
        parameter.Value = value is null
            ? DBNull.Value
            : value.Value;

        command.Parameters.Add(parameter);
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
        };
    }
}
