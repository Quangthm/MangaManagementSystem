using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Domain.Interfaces;

namespace MangaManagementSystem.Application.Services;

public class ChapterSchedulingValidator
{
    private readonly IPublicationPeriodRepository _periodRepository;

    public ChapterSchedulingValidator(IPublicationPeriodRepository periodRepository)
    {
        _periodRepository = periodRepository;
    }

    public sealed class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? AllowedPeriodStart { get; set; }
        public DateTime? AllowedPeriodEnd { get; set; }
        public string? FrequencyCode { get; set; }

        public static ValidationResult Success() => new() { IsValid = true };

        public static ValidationResult Fail(string message) => new()
        {
            IsValid = false,
            ErrorMessage = message
        };

<<<<<<< HEAD
        public static ValidationResult Fail(
            string message,
            DateTime? allowedPeriodStart,
            DateTime? allowedPeriodEnd,
            string? frequencyCode) => new()
            {
                IsValid = false,
                ErrorMessage = message,
                AllowedPeriodStart = allowedPeriodStart,
                AllowedPeriodEnd = allowedPeriodEnd,
                FrequencyCode = frequencyCode
=======
        public sealed class AdvisoryResult
        {
            public DateTime? SuggestedDate { get; set; }
            public string? WarningMessage { get; set; }

            public static AdvisoryResult NoSuggestion() => new();

            public static AdvisoryResult Suggest(DateTime date, string? warning = null) => new()
            {
                SuggestedDate = date,
                WarningMessage = warning
>>>>>>> main
            };
    }

    public async Task<ValidationResult> ValidateAsync(
        string? frequencyCode,
        Guid seriesId,
        Guid chapterId,
        DateTime plannedReleaseDate,
        DateTime? previousPlannedReleaseDate,
        CancellationToken ct = default)
    {
        var date = plannedReleaseDate.Date;

        if (string.IsNullOrWhiteSpace(frequencyCode))
        {
            return ValidationResult.Success();
        }

<<<<<<< HEAD
        var frequency = frequencyCode.ToUpperInvariant();

        if (frequency == "IRREGULAR")
        {
            return ValidationResult.Success();
        }

        string periodTypeCode = frequency == "WEEKLY" ? "WEEKLY" : "MONTHLY";

        if (previousPlannedReleaseDate.HasValue)
        {
            return await ValidateAfterPreviousChapterAsync(
                periodTypeCode, frequency, previousPlannedReleaseDate.Value, date, ct);
        }

        return await ValidateFirstChapterAsync(
            periodTypeCode, frequency, date, ct);
    }

    private async Task<ValidationResult> ValidateAfterPreviousChapterAsync(
        string periodTypeCode,
        string frequency,
        DateTime previousPlannedDate,
        DateTime plannedReleaseDate,
        CancellationToken ct)
    {
        var previousPeriod = await _periodRepository.GetPeriodContainingDateAsync(
            periodTypeCode, previousPlannedDate.Date, ct);

        if (previousPeriod == null)
        {
            return ValidationResult.Fail(
                $"Could not find a {frequency} publication period containing the previous chapter's planned date.");
        }

        var nextPeriod = await _periodRepository.GetNextPeriodAsync(
            periodTypeCode, previousPeriod, ct);

        if (nextPeriod == null)
        {
            return ValidationResult.Fail(
                $"Could not find the next {frequency} publication period after the previous chapter's period. " +
                "The publication period table may need to be populated.");
        }

        if (plannedReleaseDate >= nextPeriod.PeriodStartDate && plannedReleaseDate <= nextPeriod.PeriodEndDate)
        {
            return ValidationResult.Success();
        }

        return ValidationResult.Fail(
            $"This {frequency.ToLowerInvariant()} series must schedule the next chapter inside the next " +
            $"{frequency.ToLowerInvariant()} publication period: " +
            $"{nextPeriod.PeriodStartDate:yyyy-MM-dd} to {nextPeriod.PeriodEndDate:yyyy-MM-dd}.",
            nextPeriod.PeriodStartDate,
            nextPeriod.PeriodEndDate,
            frequency);
    }

    private async Task<ValidationResult> ValidateFirstChapterAsync(
        string periodTypeCode,
        string frequency,
        DateTime plannedReleaseDate,
        CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;

        if (frequency == "WEEKLY")
        {
            var currentPeriod = await _periodRepository.GetPeriodContainingDateAsync(
                periodTypeCode, today, ct);
            var nextPeriod = currentPeriod != null
                ? await _periodRepository.GetNextPeriodAsync(periodTypeCode, currentPeriod, ct)
                : null;

            bool inCurrentPeriod = currentPeriod != null
                && plannedReleaseDate >= currentPeriod.PeriodStartDate
                && plannedReleaseDate <= currentPeriod.PeriodEndDate;

            bool inNextPeriod = nextPeriod != null
                && plannedReleaseDate >= nextPeriod.PeriodStartDate
                && plannedReleaseDate <= nextPeriod.PeriodEndDate;

            if (inCurrentPeriod || inNextPeriod)
            {
                return ValidationResult.Success();
            }
=======
        public async Task<AdvisoryResult> GetAdvisoryAsync(
            string? frequencyCode,
            DateTime selectedDate,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(frequencyCode))
                return AdvisoryResult.NoSuggestion();
>>>>>>> main

            var start = currentPeriod?.PeriodStartDate ?? today;
            var end = nextPeriod?.PeriodEndDate ?? today.AddDays(6);

<<<<<<< HEAD
            return ValidationResult.Fail(
                "This is the first planned chapter for a weekly series. " +
                "Choose a date in the current week or next week publication period.",
                start,
                end,
                frequency);
        }
        else
        {
            var currentPeriod = await _periodRepository.GetPeriodContainingDateAsync(
                periodTypeCode, today, ct);

            if (currentPeriod == null)
            {
                return ValidationResult.Fail(
                    "Could not find the current monthly publication period. " +
                    "The publication period table may need to be populated.");
            }

            if (plannedReleaseDate >= currentPeriod.PeriodStartDate
                && plannedReleaseDate <= currentPeriod.PeriodEndDate)
            {
                return ValidationResult.Success();
            }

            return ValidationResult.Fail(
                "This is the first planned chapter for a monthly series. " +
                "Choose a date in the current monthly publication period.",
                currentPeriod.PeriodStartDate,
                currentPeriod.PeriodEndDate,
                frequency);
=======
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
>>>>>>> main
        }
    }
}
