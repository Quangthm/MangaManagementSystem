using System.Globalization;
using MangaManagementSystem.Application.Features.Ranking.Dtos;
using MudBlazor;

namespace MangaManagementSystem.Web.Components.Pages.Ranking;

public sealed record SortOption(string Value, string Label);

public sealed record PublicationPeriodOption(
    Guid PublicationPeriodId,
    string PeriodName,
    string Label,
    DateTime PeriodStartDate,
    DateTime PeriodEndDate);

public sealed record CatalogEntry(Guid SeriesId, string Title, string CoverUrl);

public sealed record RankingStatItem(string Label, string Value, string Icon, Color Color, string? ExtraClass = null);

public sealed class SeriesInputRow
{
    public Guid SeriesVoteInputId { get; init; }
    public Guid PublicationPeriodId { get; init; }
    public Guid SeriesId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CoverUrl { get; init; } = string.Empty;
    public int ReadingCount { get; set; }
    public int RatingCount { get; set; }
    public decimal AverageRating { get; set; }
    public string DataSourceNote { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class RankingRow
{
    public Guid SeriesId { get; init; }
    public int Rank { get; init; }
    public int PreviousRank { get; init; }
    public string Title { get; init; } = string.Empty;
    public string CoverUrl { get; init; } = string.Empty;
    public int ReadingCount { get; init; }
    public int RatingCount { get; init; }
    public decimal AverageRating { get; init; }
    public decimal Score { get; init; }
}

public sealed class EditRowState
{
    public int? ReadingCount { get; set; }
    public int? RatingCount { get; set; }
    public decimal? AverageRating { get; set; }
    public Dictionary<string, string> Errors { get; set; } = [];
}

public sealed class InsertFormState
{
    public Guid PublicationPeriodId { get; set; }
    public CatalogEntry? Series { get; set; }
    public int? ReadingCount { get; set; }
    public int? RatingCount { get; set; }
    public decimal? AverageRating { get; set; }
    public string DataSourceNote { get; set; } = string.Empty;
}

public sealed record SeriesVoteInputUpdateRequestUi(
    Guid SeriesVoteInputId,
    int ReadingCount,
    int RatingCount,
    decimal AverageRating,
    string DataSourceNote);

public static class RankingUiHelper
{
    public const string PlaceholderCoverUrl = "/images/placeholder-cover.png";

    public static PublicationPeriodOption ToOption(PublicationPeriodDto dto)
    {
        var label = FormatPeriodLabel(dto);

        return new PublicationPeriodOption(
            dto.PublicationPeriodId,
            dto.PeriodName,
            label,
            dto.PeriodStartDate,
            dto.PeriodEndDate);
    }

    public static SeriesInputRow ToInputRow(SeriesVoteInputDto dto)
    {
        return new SeriesInputRow
        {
            SeriesVoteInputId = dto.SeriesVoteInputId,
            PublicationPeriodId = dto.PublicationPeriodId,
            SeriesId = dto.SeriesId,
            Title = dto.SeriesTitle,
            CoverUrl = ResolveCover(dto.CoverUrl),
            ReadingCount = dto.ReadingCount,
            RatingCount = dto.RatingCount,
            AverageRating = dto.AverageRating,
            DataSourceNote = dto.DataSourceNote ?? string.Empty,
            CreatedAt = dto.EnteredAtUtc,
            UpdatedAt = dto.UpdatedAtUtc ?? dto.EnteredAtUtc
        };
    }

    public static RankingRow ToRankingRow(SeriesRankingRowDto dto)
    {
        return new RankingRow
        {
            SeriesId = dto.SeriesId,
            Rank = dto.RankPosition,
            PreviousRank = dto.RankPosition,
            Title = dto.SeriesTitle,
            CoverUrl = ResolveCover(dto.CoverUrl),
            ReadingCount = dto.ReadingCount,
            RatingCount = dto.RatingCount,
            AverageRating = dto.AverageRating,
            Score = dto.RankingScore
        };
    }

    public static CatalogEntry ToCatalogEntry(RankableSeriesDto dto)
    {
        return new CatalogEntry(
            dto.SeriesId,
            dto.Title,
            ResolveCover(dto.CoverUrl));
    }

    public static string FormatNumber(int value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    public static string FormatShortNumber(int value)
    {
        return value >= 1000
            ? $"{value / 1000M:0.0}K"
            : value.ToString(CultureInfo.InvariantCulture);
    }

    public static string GetRankLabel(int rank)
    {
        return rank switch
        {
            1 => "🥇",
            2 => "🥈",
            3 => "🥉",
            _ => $"#{rank}"
        };
    }

    public static string GetRankClass(int rank)
    {
        return rank switch
        {
            1 => "rank-gold",
            2 => "rank-silver",
            3 => "rank-bronze",
            _ => "rank-normal"
        };
    }

    public static Color GetRankChipColor(int rank)
    {
        return rank switch
        {
            1 => Color.Warning,
            2 => Color.Info,
            3 => Color.Secondary,
            _ => Color.Primary
        };
    }

    public static string GetTrendLabel(int previousRank, int currentRank)
    {
        var delta = previousRank - currentRank;

        return delta switch
        {
            > 0 => $"▲ {delta}",
            < 0 => $"▼ {Math.Abs(delta)}",
            _ => "—"
        };
    }

    private static string FormatPeriodLabel(PublicationPeriodDto dto)
    {
        if (dto.PeriodTypeCode == "WEEKLY")
        {
            return $"{dto.PeriodName} ({dto.PeriodStartDate:MMM d} – {dto.PeriodEndDate:MMM d})";
        }

        return $"{dto.PeriodName} ({dto.PeriodStartDate:yyyy-MM-dd} – {dto.PeriodEndDate:yyyy-MM-dd})";
    }

    private static string ResolveCover(string? coverUrl)
    {
        return string.IsNullOrWhiteSpace(coverUrl)
            ? PlaceholderCoverUrl
            : coverUrl;
    }
}
