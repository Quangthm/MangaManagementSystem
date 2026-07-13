using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.API.Contracts;

public sealed class SetSeriesHiatusRequest
{
    [Required]
    [StringLength(500)]
    public string Reason { get; init; } = string.Empty;
}
