using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Domain.Interfaces;

namespace MangaManagementSystem.Application.Services
{
    public class ChapterSchedulingValidator
    {
        private readonly IPublicationPeriodRepository _periodRepository;

        public ChapterSchedulingValidator(IPublicationPeriodRepository periodRepository)
        {
            _periodRepository = periodRepository;
        }

        public sealed class AdvisoryResult
        {
            public DateTime? SuggestedDate { get; set; }
            public string? WarningMessage { get; set; }

            public static AdvisoryResult NoSuggestion() => new();

            public static AdvisoryResult Suggest(DateTime date, string? warning = null) => new()
            {
                SuggestedDate = date,
                WarningMessage = warning
            };
        }

        public async Task<AdvisoryResult> GetAdvisoryAsync(
            string? frequencyCode,
            DateTime selectedDate,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(frequencyCode))
                return AdvisoryResult.NoSuggestion();

            var frequency = frequencyCode.ToUpperInvariant();

            if (frequency == "IRREGULAR")
                return AdvisoryResult.NoSuggestion();

            var today = DateTime.UtcNow.Date;

            if (frequency == "WEEKLY")
            {
                var suggested = GetNextWeekSameDay(today, selectedDate);
                string? warning = null;
                if (selectedDate.Date != suggested.Date)
                    warning = $"The selected date does not follow the WEEKLY pattern. " +
                              $"Suggested next date: {suggested:yyyy-MM-dd}.";

                return AdvisoryResult.Suggest(suggested, warning);
            }

            if (frequency == "MONTHLY")
            {
                var suggested = GetNextMonthSameDay(today, selectedDate);
                string? warning = null;
                if (selectedDate.Date != suggested.Date)
                    warning = $"The selected date does not match the MONTHLY suggested date: " +
                              $"{suggested:yyyy-MM-dd}.";

                return AdvisoryResult.Suggest(suggested, warning);
            }

            return AdvisoryResult.NoSuggestion();
        }

        private static DateTime GetNextWeekSameDay(DateTime today, DateTime selectedDate)
        {
            int diff = ((int)selectedDate.DayOfWeek - (int)today.DayOfWeek + 7) % 7;
            if (diff == 0) diff = 7;
            return today.AddDays(diff);
        }

        private static DateTime GetNextMonthSameDay(DateTime today, DateTime selectedDate)
        {
            int nextMonth = today.Month == 12 ? 1 : today.Month + 1;
            int nextYear = today.Month == 12 ? today.Year + 1 : today.Year;
            int targetDay = selectedDate.Day;
            int daysInMonth = DateTime.DaysInMonth(nextYear, nextMonth);
            int actualDay = Math.Min(targetDay, daysInMonth);
            return new DateTime(nextYear, nextMonth, actualDay);
        }
    }
}
