using MangaManagementSystem.Application.DTOs.Admin;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Admin.Audit.Queries
{
    internal static class AdminAuditQueryValidation
    {
        internal static void ValidatePage(
            int pageNumber,
            int pageSize)
        {
            if (pageNumber < 1)
            {
                throw new InvalidOperationException(
                    "Page number must be at least 1.");
            }

            if (pageSize is < 5 or > 100)
            {
                throw new InvalidOperationException(
                    "Page size must be between 5 and 100.");
            }
        }

        internal static string? NormalizeText(
            string? value,
            string fieldName,
            int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();

            if (normalized.Length > maximumLength)
            {
                throw new InvalidOperationException(
                    $"{fieldName} cannot exceed {maximumLength} characters.");
            }

            return normalized;
        }

        internal static void ValidateDates(
            DateTime? fromUtc,
            DateTime? toUtc)
        {
            if (fromUtc.HasValue
                && toUtc.HasValue
                && fromUtc.Value > toUtc.Value)
            {
                throw new InvalidOperationException(
                    "The from date cannot be later than the to date.");
            }
        }
    }

    public sealed class SearchAdminAuditEventsQueryHandler
        : IRequestHandler<
            SearchAdminAuditEventsQuery,
            AdminAuditPageDto>
    {
        private readonly IAuditEventRepository
            _auditEventRepository;

        public SearchAdminAuditEventsQueryHandler(
            IAuditEventRepository auditEventRepository)
        {
            _auditEventRepository = auditEventRepository;
        }

        public async Task<AdminAuditPageDto> Handle(
            SearchAdminAuditEventsQuery request,
            CancellationToken cancellationToken)
        {
            AdminAuditQueryValidation.ValidatePage(
                request.PageNumber,
                request.PageSize);

            AdminAuditQueryValidation.ValidateDates(
                request.FromUtc,
                request.ToUtc);

            var criteria =
                new AuditEventSearchCriteria(
                    AdminAuditQueryValidation.NormalizeText(
                        request.Search,
                        "Search text",
                        200),
                    AdminAuditQueryValidation.NormalizeText(
                        request.ActionCode,
                        "Action code",
                        128),
                    AdminAuditQueryValidation.NormalizeText(
                        request.EntityType,
                        "Entity type",
                        128),
                    request.FromUtc,
                    request.ToUtc,
                    request.PageNumber,
                    request.PageSize);

            var result =
                await _auditEventRepository.SearchAsync(
                    criteria,
                    cancellationToken);

            var totalPages =
                result.TotalCount == 0
                    ? 0
                    : (int)Math.Ceiling(
                        result.TotalCount
                        / (double)request.PageSize);

            var items =
                result.Items
                    .Select(
                        item =>
                            new AdminAuditEventDto(
                                item.AuditEventId,
                                item.OccurredAtUtc,
                                item.ActorUserId,
                                item.ActorUser?.Username,
                                item.ActorUser?.DisplayName,
                                item.ActorRoleName,
                                item.ActionCode,
                                item.EntityType,
                                item.EntityId,
                                item.DetailJson))
                    .ToList();

            return new AdminAuditPageDto(
                items,
                request.PageNumber,
                request.PageSize,
                result.TotalCount,
                totalPages);
        }
    }

    public sealed class GetAdminAuditFilterOptionsQueryHandler
        : IRequestHandler<
            GetAdminAuditFilterOptionsQuery,
            AdminAuditFilterOptionsDto>
    {
        private readonly IAuditEventRepository
            _auditEventRepository;

        public GetAdminAuditFilterOptionsQueryHandler(
            IAuditEventRepository auditEventRepository)
        {
            _auditEventRepository = auditEventRepository;
        }

        public async Task<AdminAuditFilterOptionsDto> Handle(
            GetAdminAuditFilterOptionsQuery request,
            CancellationToken cancellationToken)
        {
            var actionCodes =
                await _auditEventRepository
                    .GetDistinctActionCodesAsync(
                        cancellationToken);

            var entityTypes =
                await _auditEventRepository
                    .GetDistinctEntityTypesAsync(
                        cancellationToken);

            return new AdminAuditFilterOptionsDto(
                actionCodes,
                entityTypes);
        }
    }
}