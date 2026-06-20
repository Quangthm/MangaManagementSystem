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
        if (currentUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Current user id is required.",
                nameof(currentUserId));
        }

        var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT
                v.series_board_poll_id,
                v.series_id,
                s.slug,
                v.series_title,
                v.poll_type_code,
                v.poll_status_code,
                v.poll_reason,
                s.publication_frequency_code,
                v.started_at_utc,
                v.ends_at_utc,
                ISNULL(v.approve_count, 0) AS approve_count,
                ISNULL(v.reject_count, 0) AS reject_count,
                ISNULL(v.abstain_count, 0) AS abstain_count,
                ISNULL(v.total_vote_count, 0) AS total_vote_count,
                ISNULL(v.computed_result_code, N'PENDING')
                    AS computed_result_code,
                current_vote.choice_code,
                current_vote.vote_reason
            FROM manga.vw_SeriesBoardPollVoteSummary v
            INNER JOIN manga.Series s
                ON s.series_id = v.series_id
            LEFT JOIN manga.SeriesBoardVote current_vote
                ON current_vote.series_board_poll_id =
                    v.series_board_poll_id
               AND current_vote.user_id = @current_user_id
            WHERE v.poll_status_code = N'OPEN'
            ORDER BY v.started_at_utc DESC;
            """;

        AddParameter(
            command,
            "@current_user_id",
            DbType.Guid,
            currentUserId);

        var rows = new List<EditorialBoardPollDto>();

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var pollTypeCode =
                GetStringOrDefault(reader, 4, "UNKNOWN");

            var seriesTitle =
                GetStringOrDefault(
                    reader,
                    3,
                    "Untitled Series");

            rows.Add(
                new EditorialBoardPollDto(
                    PollId: reader.GetGuid(0),
                    SeriesId: reader.GetGuid(1),
                    Code: GetStringOrDefault(reader, 2, "N/A"),
                    SeriesTitle: seriesTitle,
                    PollName:
                        $"{MapPollType(pollTypeCode)} â€” {seriesTitle}",
                    PollTypeCode: pollTypeCode,
                    PollStatusCode:
                        GetStringOrDefault(reader, 5, "OPEN"),
                    PollReason:
                        GetStringOrDefault(
                            reader,
                            6,
                            "No reason provided"),
                    PublicationFrequencyCode:
                        GetNullableString(reader, 7),
                    StartedAtUtc: reader.GetDateTime(8),
                    EndsAtUtc: GetNullableDateTime(reader, 9),
                    ApproveVotes: ToInt32(reader, 10),
                    RejectVotes: ToInt32(reader, 11),
                    AbstainVotes: ToInt32(reader, 12),
                    TotalVotes: ToInt32(reader, 13),
                    ComputedResultCode:
                        GetStringOrDefault(
                            reader,
                            14,
                            "PENDING"),
                    CurrentUserChoiceCode:
                        GetNullableString(reader, 15),
                    CurrentUserVoteReason:
                        GetNullableString(reader, 16)));
        }

        return rows;
    }

    public async Task<OpenSeriesBoardPollResultDto> OpenPollAsync(
        OpenSeriesBoardPollRequestDto request,
        Guid chiefUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (chiefUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Chief user id is required.",
                nameof(chiefUserId));
        }

        if (request.ProposalId == Guid.Empty)
        {
            throw new ArgumentException(
                "Proposal id is required.",
                nameof(request));
        }

        var pollTypeCode =
            NormalizeRequiredCode(
                request.PollTypeCode,
                nameof(request.PollTypeCode),
                50);

        var pollReason =
            NormalizeRequiredText(
                request.PollReason,
                nameof(request.PollReason));

        var publicationFrequencyCode =
            NormalizeOptionalCode(
                request.PublicationFrequencyCode,
                nameof(request.PublicationFrequencyCode),
                20);

        if (publicationFrequencyCode is not null
            && publicationFrequencyCode is not
                ("WEEKLY" or "MONTHLY" or "IRREGULAR"))
        {
            throw new ArgumentException(
                "Publication frequency must be WEEKLY, MONTHLY, or IRREGULAR.",
                nameof(request));
        }

        var nowUtc = DateTime.UtcNow;

        var endsAtUtc =
            request.EndsAtUtc?.ToUniversalTime();

        if (endsAtUtc.HasValue
            && endsAtUtc.Value <= nowUtc)
        {
            throw new ArgumentException(
                "Poll end time must be in the future.",
                nameof(request));
        }

        var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var transaction =
            await connection.BeginTransactionAsync(
                cancellationToken);

        try
        {
            Guid seriesId;

            await using (
                var proposalCommand =
                    connection.CreateCommand())
            {
                proposalCommand.Transaction = transaction;

                proposalCommand.CommandText =
                    """
                    SELECT series_id
                    FROM manga.SeriesProposal
                    WHERE series_proposal_id = @proposal_id
                      AND status_code = N'UNDER_BOARD_REVIEW';
                    """;

                AddParameter(
                    proposalCommand,
                    "@proposal_id",
                    DbType.Guid,
                    request.ProposalId);

                var proposalResult =
                    await proposalCommand.ExecuteScalarAsync(
                        cancellationToken);

                if (proposalResult is not Guid resolvedSeriesId)
                {
                    throw new InvalidOperationException(
                        "The proposal was not found or is not under board review.");
                }

                seriesId = resolvedSeriesId;
            }

            await using (
                var duplicateCommand =
                    connection.CreateCommand())
            {
                duplicateCommand.Transaction = transaction;

                duplicateCommand.CommandText =
                    """
                    SELECT COUNT(1)
                    FROM manga.SeriesBoardPoll
                    WHERE series_id = @series_id
                      AND poll_type_code = @poll_type_code
                      AND poll_status_code = N'OPEN';
                    """;

                AddParameter(
                    duplicateCommand,
                    "@series_id",
                    DbType.Guid,
                    seriesId);

                AddParameter(
                    duplicateCommand,
                    "@poll_type_code",
                    DbType.String,
                    pollTypeCode);

                var existingCount =
                    Convert.ToInt32(
                        await duplicateCommand.ExecuteScalarAsync(
                            cancellationToken));

                if (existingCount > 0)
                {
                    throw new InvalidOperationException(
                        "An open poll of this type already exists for the series.");
                }
            }

            if (publicationFrequencyCode is not null)
            {
                await using var updateSeriesCommand =
                    connection.CreateCommand();

                updateSeriesCommand.Transaction = transaction;

                updateSeriesCommand.CommandText =
                    """
                    UPDATE manga.Series
                    SET publication_frequency_code =
                        @publication_frequency_code,
                        updated_by_user_id =
                            @chief_user_id,
                        updated_at_utc =
                            SYSUTCDATETIME()
                    WHERE series_id = @series_id;
                    """;

                AddParameter(
                    updateSeriesCommand,
                    "@publication_frequency_code",
                    DbType.String,
                    publicationFrequencyCode);

                AddParameter(
                    updateSeriesCommand,
                    "@chief_user_id",
                    DbType.Guid,
                    chiefUserId);

                AddParameter(
                    updateSeriesCommand,
                    "@series_id",
                    DbType.Guid,
                    seriesId);

                var updatedSeriesRows =
                    await updateSeriesCommand.ExecuteNonQueryAsync(
                        cancellationToken);

                if (updatedSeriesRows != 1)
                {
                    throw new InvalidOperationException(
                        "The series could not be updated before opening the poll.");
                }
            }

            var pollId = Guid.NewGuid();

            await using (
                var insertPollCommand =
                    connection.CreateCommand())
            {
                insertPollCommand.Transaction = transaction;

                insertPollCommand.CommandText =
                    """
                    INSERT INTO manga.SeriesBoardPoll
                    (
                        series_board_poll_id,
                        series_id,
                        poll_type_code,
                        poll_reason,
                        poll_status_code,
                        created_by_user_id,
                        started_at_utc,
                        ends_at_utc
                    )
                    VALUES
                    (
                        @poll_id,
                        @series_id,
                        @poll_type_code,
                        @poll_reason,
                        N'OPEN',
                        @chief_user_id,
                        @started_at_utc,
                        @ends_at_utc
                    );
                    """;

                AddParameter(
                    insertPollCommand,
                    "@poll_id",
                    DbType.Guid,
                    pollId);

                AddParameter(
                    insertPollCommand,
                    "@series_id",
                    DbType.Guid,
                    seriesId);

                AddParameter(
                    insertPollCommand,
                    "@poll_type_code",
                    DbType.String,
                    pollTypeCode);

                AddParameter(
                    insertPollCommand,
                    "@poll_reason",
                    DbType.String,
                    pollReason);

                AddParameter(
                    insertPollCommand,
                    "@chief_user_id",
                    DbType.Guid,
                    chiefUserId);

                AddParameter(
                    insertPollCommand,
                    "@started_at_utc",
                    DbType.DateTime2,
                    nowUtc);

                AddParameter(
                    insertPollCommand,
                    "@ends_at_utc",
                    DbType.DateTime2,
                    endsAtUtc);

                var insertedRows =
                    await insertPollCommand.ExecuteNonQueryAsync(
                        cancellationToken);

                if (insertedRows != 1)
                {
                    throw new InvalidOperationException(
                        "The board poll was not created.");
                }
            }

            await transaction.CommitAsync(
                cancellationToken);

            return new OpenSeriesBoardPollResultDto(
                PollId: pollId,
                SeriesId: seriesId,
                ProposalId: request.ProposalId,
                PollStatusCode: "OPEN");
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task<CastSeriesBoardVoteResultDto> CastVoteAsync(
        CastSeriesBoardVoteRequestDto request,
        Guid voterUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (voterUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Voter user id is required.",
                nameof(voterUserId));
        }

        if (request.PollId == Guid.Empty)
        {
            throw new ArgumentException(
                "Poll id is required.",
                nameof(request));
        }

        var choiceCode =
            NormalizeRequiredCode(
                request.ChoiceCode,
                nameof(request.ChoiceCode),
                50);

        if (choiceCode is not
            ("APPROVE" or "REJECT" or "ABSTAIN"))
        {
            throw new ArgumentException(
                "Vote choice must be APPROVE, REJECT, or ABSTAIN.",
                nameof(request));
        }

        var voteReason =
            NormalizeOptionalText(
                request.VoteReason,
                500,
                nameof(request.VoteReason));

        var nowUtc = DateTime.UtcNow;

        var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var transaction =
            await connection.BeginTransactionAsync(
                cancellationToken);

        try
        {
            string pollStatusCode;
            DateTime? pollEndsAtUtc;

            await using (
                var pollCommand =
                    connection.CreateCommand())
            {
                pollCommand.Transaction = transaction;

                pollCommand.CommandText =
                    """
                    SELECT
                        poll_status_code,
                        ends_at_utc
                    FROM manga.SeriesBoardPoll
                    WHERE series_board_poll_id = @poll_id;
                    """;

                AddParameter(
                    pollCommand,
                    "@poll_id",
                    DbType.Guid,
                    request.PollId);

                await using var reader =
                    await pollCommand.ExecuteReaderAsync(
                        cancellationToken);

                if (!await reader.ReadAsync(
                    cancellationToken))
                {
                    throw new InvalidOperationException(
                        "The board poll was not found.");
                }

                pollStatusCode =
                    GetStringOrDefault(
                        reader,
                        0,
                        "UNKNOWN");

                pollEndsAtUtc =
                    GetNullableDateTime(
                        reader,
                        1);
            }

            if (!string.Equals(
                pollStatusCode,
                "OPEN",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Votes can only be cast while the poll is open.");
            }

            if (pollEndsAtUtc.HasValue
                && pollEndsAtUtc.Value <= nowUtc)
            {
                throw new InvalidOperationException(
                    "The board poll has already ended.");
            }

            Guid voteId;

            await using (
                var existingVoteCommand =
                    connection.CreateCommand())
            {
                existingVoteCommand.Transaction = transaction;

                existingVoteCommand.CommandText =
                    """
                    SELECT series_board_vote_id
                    FROM manga.SeriesBoardVote
                    WHERE series_board_poll_id = @poll_id
                      AND user_id = @voter_user_id;
                    """;

                AddParameter(
                    existingVoteCommand,
                    "@poll_id",
                    DbType.Guid,
                    request.PollId);

                AddParameter(
                    existingVoteCommand,
                    "@voter_user_id",
                    DbType.Guid,
                    voterUserId);

                var existingVoteResult =
                    await existingVoteCommand.ExecuteScalarAsync(
                        cancellationToken);

                if (existingVoteResult is Guid existingVoteId)
                {
                    voteId = existingVoteId;

                    await using var updateVoteCommand =
                        connection.CreateCommand();

                    updateVoteCommand.Transaction = transaction;

                    updateVoteCommand.CommandText =
                        """
                        UPDATE manga.SeriesBoardVote
                        SET choice_code = @choice_code,
                            vote_reason = @vote_reason,
                            voted_at_utc = @voted_at_utc
                        WHERE series_board_vote_id = @vote_id;
                        """;

                    AddParameter(
                        updateVoteCommand,
                        "@choice_code",
                        DbType.String,
                        choiceCode);

                    AddParameter(
                        updateVoteCommand,
                        "@vote_reason",
                        DbType.String,
                        voteReason);

                    AddParameter(
                        updateVoteCommand,
                        "@voted_at_utc",
                        DbType.DateTime2,
                        nowUtc);

                    AddParameter(
                        updateVoteCommand,
                        "@vote_id",
                        DbType.Guid,
                        voteId);

                    var updatedRows =
                        await updateVoteCommand.ExecuteNonQueryAsync(
                            cancellationToken);

                    if (updatedRows != 1)
                    {
                        throw new InvalidOperationException(
                            "The existing board vote was not updated.");
                    }
                }
                else
                {
                    voteId = Guid.NewGuid();

                    await using var insertVoteCommand =
                        connection.CreateCommand();

                    insertVoteCommand.Transaction = transaction;

                    insertVoteCommand.CommandText =
                        """
                        INSERT INTO manga.SeriesBoardVote
                        (
                            series_board_vote_id,
                            series_board_poll_id,
                            user_id,
                            choice_code,
                            vote_reason,
                            voted_at_utc
                        )
                        VALUES
                        (
                            @vote_id,
                            @poll_id,
                            @voter_user_id,
                            @choice_code,
                            @vote_reason,
                            @voted_at_utc
                        );
                        """;

                    AddParameter(
                        insertVoteCommand,
                        "@vote_id",
                        DbType.Guid,
                        voteId);

                    AddParameter(
                        insertVoteCommand,
                        "@poll_id",
                        DbType.Guid,
                        request.PollId);

                    AddParameter(
                        insertVoteCommand,
                        "@voter_user_id",
                        DbType.Guid,
                        voterUserId);

                    AddParameter(
                        insertVoteCommand,
                        "@choice_code",
                        DbType.String,
                        choiceCode);

                    AddParameter(
                        insertVoteCommand,
                        "@vote_reason",
                        DbType.String,
                        voteReason);

                    AddParameter(
                        insertVoteCommand,
                        "@voted_at_utc",
                        DbType.DateTime2,
                        nowUtc);

                    var insertedRows =
                        await insertVoteCommand.ExecuteNonQueryAsync(
                            cancellationToken);

                    if (insertedRows != 1)
                    {
                        throw new InvalidOperationException(
                            "The board vote was not created.");
                    }
                }
            }

            await transaction.CommitAsync(
                cancellationToken);

            return new CastSeriesBoardVoteResultDto(
                PollId: request.PollId,
                VoteId: voteId,
                UserId: voterUserId,
                ChoiceCode: choiceCode,
                VoteReason: voteReason,
                VotedAtUtc: nowUtc);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static DbParameter AddParameter(
        DbCommand command,
        string name,
        DbType dbType,
        object? value)
    {
        var parameter = command.CreateParameter();

        parameter.ParameterName = name;
        parameter.DbType = dbType;
        parameter.Value = value ?? DBNull.Value;

        command.Parameters.Add(parameter);

        return parameter;
    }

    private static string NormalizeRequiredCode(
        string? value,
        string parameterName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A value is required.",
                parameterName);
        }

        var normalized =
            value.Trim().ToUpperInvariant();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalCode(
        string? value,
        string parameterName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized =
            value.Trim().ToUpperInvariant();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string NormalizeRequiredText(
        string? value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A value is required.",
                parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalText(
        string? value,
        int maxLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string? GetNullableString(
        DbDataReader reader,
        int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }

    private static DateTime? GetNullableDateTime(
        DbDataReader reader,
        int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetDateTime(ordinal);
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