using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Publication.Schedule.Queries.GetPublicationScheduleCalendar
{
    public sealed class GetPublicationScheduleCalendarQueryHandler
        : IRequestHandler<GetPublicationScheduleCalendarQuery, PublicationScheduleCalendarDto>
    {
        private readonly IPublicationScheduleRepository _repository;

        public GetPublicationScheduleCalendarQueryHandler(IPublicationScheduleRepository repository)
        {
            _repository = repository;
        }

        public async Task<PublicationScheduleCalendarDto> Handle(
            GetPublicationScheduleCalendarQuery request, CancellationToken cancellationToken)
        {
            var anchor = request.AnchorDate ?? DateTime.UtcNow.Date;
            var culture = CultureInfo.InvariantCulture;
            var dayOfWeek = (int)anchor.DayOfWeek;
            dayOfWeek = dayOfWeek == 0 ? 7 : dayOfWeek;
            var weekStart = anchor.AddDays(1 - dayOfWeek);
            var weekEnd = weekStart.AddDays(6);

            var chapters = await _repository.GetScheduleChaptersAsync(
                weekStart, weekEnd,
                request.SeriesId,
                request.FrequencyCode,
                cancellationToken);

            var today = DateTime.UtcNow.Date;
            var days = new List<PublicationScheduleDayDto>();
            for (int i = 0; i < 7; i++)
            {
                var date = weekStart.AddDays(i);
                var items = chapters
                    .Where(c => GetEffectiveDate(c.PlannedReleaseDate, c.ReleasedAtUtc) == date)
                    .OrderBy(c => c.SeriesTitle)
                    .ThenBy(c => c.ChapterNumberLabel)
                    .Select(c => new PublicationScheduleItemDto(
                        c.SeriesId,
                        c.SeriesTitle,
                        c.SeriesSlug,
                        c.SeriesCoverUrl,
                        c.ChapterId,
                        c.ChapterNumberLabel,
                        c.StatusCode,
                        GetStatusBadgeLabel(c.StatusCode),
                        c.PlannedReleaseDate,
                        c.ReleasedAtUtc,
                        c.PublicationFrequencyCode))
                    .ToList();

                days.Add(new PublicationScheduleDayDto(
                    date,
                    date.ToString("ddd, MMM d", culture),
                    date == today,
                    items.AsReadOnly()));
            }

            return new PublicationScheduleCalendarDto(weekStart, weekEnd, days.AsReadOnly());
        }

        private static DateTime GetEffectiveDate(DateTime? planned, DateTime? released)
        {
            if (released.HasValue)
                return released.Value.Date;
            if (planned.HasValue)
                return planned.Value.Date;
            return DateTime.MinValue;
        }

        private static string GetStatusBadgeLabel(string statusCode) => statusCode switch
        {
            "RELEASED" => "Released",
            "SCHEDULED" => "Planned",
            "ON_HOLD" => "On Hold",
            "APPROVED" => "Planned",
            _ => statusCode.Replace("_", " ")
        };
    }
}
