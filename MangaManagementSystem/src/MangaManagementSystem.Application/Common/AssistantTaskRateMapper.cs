using System.Collections.Generic;

namespace MangaManagementSystem.Application.Common
{
    internal static class AssistantTaskRateMapper
    {
        private static readonly Dictionary<string, decimal> FixedRates = new(System.StringComparer.OrdinalIgnoreCase)
        {
            ["SHADING"] = 100000m,
            ["CLEANUP"] = 80000m,
            ["BACKGROUND"] = 150000m,
            ["EFFECTS"] = 120000m,
            ["DIALOGUE"] = 90000m,
            ["TYPESETTING"] = 70000m,
            ["REVIEW"] = 100000m,
        };

        private const decimal DefaultRate = 100000m;

        internal static decimal GetRate(string taskType)
            => FixedRates.TryGetValue(taskType, out var rate) ? rate : DefaultRate;

        internal static decimal GetEstimatedAmount(string taskType, decimal? compensationAmount)
            => compensationAmount ?? GetRate(taskType);
    }
}
