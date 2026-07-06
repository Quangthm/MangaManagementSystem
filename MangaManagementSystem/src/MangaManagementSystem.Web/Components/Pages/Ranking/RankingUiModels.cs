using System.Globalization;
using MudBlazor;

namespace MangaManagementSystem.Web.Components.Pages.Ranking;

public sealed record SortOption(string Value, string Label);

public sealed record CatalogEntry(Guid SeriesId, string Title, string CoverUrl);

public sealed record RankingStatItem(string Label, string Value, string Icon, Color Color, string? ExtraClass = null);

public sealed class SeriesInputRow
{
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
    public int Rank { get; set; }
    public int PreviousRank { get; set; }
    public string Title { get; init; } = string.Empty;
    public string CoverUrl { get; init; } = string.Empty;
    public int ReadingCount { get; init; }
    public int RatingCount { get; init; }
    public decimal AverageRating { get; init; }
    public double Score { get; init; }
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
    public string PublicationWeek { get; set; } = string.Empty;
    public CatalogEntry? Series { get; set; }
    public int? ReadingCount { get; set; }
    public int? RatingCount { get; set; }
    public decimal? AverageRating { get; set; }
    public string DataSourceNote { get; set; } = string.Empty;
}

public static class RankingUiHelper
{
    public static double CalculateScore(decimal averageRating, int ratingCount, int readingCount)
    {
        return (double)averageRating * Math.Log10(1 + ratingCount) + readingCount * 0.001;
    }

    public static IReadOnlyList<RankingRow> BuildRankingRows(IEnumerable<SeriesInputRow> inputRows)
    {
        return inputRows
            .Select(row => new RankingRow
            {
                SeriesId = row.SeriesId,
                Title = row.Title,
                CoverUrl = row.CoverUrl,
                ReadingCount = row.ReadingCount,
                RatingCount = row.RatingCount,
                AverageRating = row.AverageRating,
                Score = CalculateScore(row.AverageRating, row.RatingCount, row.ReadingCount),
                PreviousRank = 0
            })
            .OrderByDescending(row => row.Score)
            .Select((row, index) =>
            {
                row.Rank = index + 1;
                row.PreviousRank = row.Rank + ((row.Rank % 3) - 1);
                return row;
            })
            .ToList();
    }

    public static IEnumerable<SeriesInputRow> SortInputRows(IEnumerable<SeriesInputRow> rows, string sort)
    {
        return sort switch
        {
            "rank_asc" => rows.OrderByDescending(row => CalculateScore(row.AverageRating, row.RatingCount, row.ReadingCount)),
            "rank_desc" => rows.OrderBy(row => CalculateScore(row.AverageRating, row.RatingCount, row.ReadingCount)),
            "title_asc" => rows.OrderBy(row => row.Title),
            "title_desc" => rows.OrderByDescending(row => row.Title),
            "score_desc" => rows.OrderByDescending(row => CalculateScore(row.AverageRating, row.RatingCount, row.ReadingCount)),
            "score_asc" => rows.OrderBy(row => CalculateScore(row.AverageRating, row.RatingCount, row.ReadingCount)),
            "average_rating_desc" => rows.OrderByDescending(row => row.AverageRating),
            "average_rating_asc" => rows.OrderBy(row => row.AverageRating),
            "reading_count_desc" => rows.OrderByDescending(row => row.ReadingCount),
            "reading_count_asc" => rows.OrderBy(row => row.ReadingCount),
            "rating_count_desc" => rows.OrderByDescending(row => row.RatingCount),
            "rating_count_asc" => rows.OrderBy(row => row.RatingCount),
            _ => rows.OrderByDescending(row => CalculateScore(row.AverageRating, row.RatingCount, row.ReadingCount))
        };
    }

    public static IEnumerable<RankingRow> SortRankingRows(IEnumerable<RankingRow> rows, string sort)
    {
        return sort switch
        {
            "rank_asc" => rows.OrderBy(row => row.Rank),
            "rank_desc" => rows.OrderByDescending(row => row.Rank),
            "title_asc" => rows.OrderBy(row => row.Title),
            "title_desc" => rows.OrderByDescending(row => row.Title),
            "score_desc" => rows.OrderByDescending(row => row.Score),
            "score_asc" => rows.OrderBy(row => row.Score),
            "average_rating_desc" => rows.OrderByDescending(row => row.AverageRating),
            "average_rating_asc" => rows.OrderBy(row => row.AverageRating),
            "reading_count_desc" => rows.OrderByDescending(row => row.ReadingCount),
            "reading_count_asc" => rows.OrderBy(row => row.ReadingCount),
            "rating_count_desc" => rows.OrderByDescending(row => row.RatingCount),
            "rating_count_asc" => rows.OrderBy(row => row.RatingCount),
            _ => rows.OrderBy(row => row.Rank)
        };
    }

    public static bool Matches(string title, string query)
    {
        return string.IsNullOrWhiteSpace(query) || title.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);
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
}
