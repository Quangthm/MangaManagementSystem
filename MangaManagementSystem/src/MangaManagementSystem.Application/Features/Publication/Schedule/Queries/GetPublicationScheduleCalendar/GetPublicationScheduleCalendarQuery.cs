using System;
using MangaManagementSystem.Application.DTOs.Publication;
using MediatR;

namespace MangaManagementSystem.Application.Features.Publication.Schedule.Queries.GetPublicationScheduleCalendar;

public sealed record GetPublicationScheduleCalendarQuery(
    DateTime? AnchorDate,
    Guid? SeriesId,
    string? FrequencyCode) : IRequest<PublicationScheduleCalendarDto>;
