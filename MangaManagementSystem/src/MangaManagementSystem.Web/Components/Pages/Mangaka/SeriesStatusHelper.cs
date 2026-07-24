namespace MangaManagementSystem.Web.Components.Pages.Mangaka;

public static class SeriesStatusHelper
{
    public static string DisplayName(string code) => code switch
    {
        "PROPOSAL_DRAFT" => "Proposal Draft",
        "UNDER_EDITORIAL_REVIEW" => "Editorial Review",
        "UNDER_BOARD_REVIEW" => "Board Review",
        "SERIALIZED" => "Serialized",
        "HIATUS" => "Hiatus",
        "CANCELLED" => "Cancelled",
        "COMPLETED" => "Completed",
        _ => code.Replace("_", " ").ToLower(),
    };

    public static string BadgeColor(string code) => code switch
    {
        "SERIALIZED" => "#dcfce7",
        "HIATUS" => "#fef3c7",
        "CANCELLED" => "#fee2e2",
        "COMPLETED" => "#e0e7ff",
        _ => "#f1f5f9",
    };

    public static string TextColor(string code) => code switch
    {
        "SERIALIZED" => "#166534",
        "HIATUS" => "#92400e",
        "CANCELLED" => "#991b1b",
        "COMPLETED" => "#3730a3",
        _ => "#475569",
    };

    public static string[] AllStatusCodes { get; } =
    [
        "PROPOSAL_DRAFT",
        "UNDER_EDITORIAL_REVIEW",
        "UNDER_BOARD_REVIEW",
        "SERIALIZED",
        "HIATUS",
        "CANCELLED",
        "COMPLETED",
    ];
}
