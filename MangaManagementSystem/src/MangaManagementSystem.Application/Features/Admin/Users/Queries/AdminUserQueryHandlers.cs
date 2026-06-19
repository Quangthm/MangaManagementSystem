using MangaManagementSystem.Application.DTOs.Admin;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Application.Mappers;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Admin.Users.Queries
{
    internal static class AdminUserQueryValidation
    {
        internal static readonly HashSet<string> AllowedStatusCodes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "PENDING_APPROVAL",
                "ACTIVE",
                "DISABLED",
                "REJECTED"
            };

        internal static readonly HashSet<string> AllowedRoleNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Admin",
                "Mangaka",
                "Assistant",
                "Tantou Editor",
                "Editorial Board Member",
                "Editorial Board Chief"
            };

        internal static string? NormalizeStatus(string? statusCode)
        {
            if (string.IsNullOrWhiteSpace(statusCode))
            {
                return null;
            }

            var normalized = statusCode.Trim().ToUpperInvariant();

            if (!AllowedStatusCodes.Contains(normalized))
            {
                throw new InvalidOperationException(
                    "The requested user status is not supported.");
            }

            return normalized;
        }

        internal static string? NormalizeRole(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return null;
            }

            var normalized = roleName.Trim();

            if (!AllowedRoleNames.Contains(normalized))
            {
                throw new InvalidOperationException(
                    "The requested user role is not supported.");
            }

            return AllowedRoleNames.Single(
                role => string.Equals(
                    role,
                    normalized,
                    StringComparison.OrdinalIgnoreCase));
        }

        internal static void ValidatePage(int pageNumber, int pageSize)
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

        internal static AdminUserListItemDto ToListItem(User user)
        {
            return new AdminUserListItemDto(
                user.UserId,
                user.Username,
                user.Email,
                user.DisplayName,
                user.Role?.RoleName,
                user.StatusCode,
                user.AvatarFileId,
                user.PortfolioFileId,
                user.CreatedAtUtc);
        }

        internal static AdminUserDetailDto ToDetail(User user)
        {
            return new AdminUserDetailDto(
                user.UserId,
                user.Username,
                user.Email,
                user.DisplayName,
                user.Role?.RoleName,
                user.StatusCode,
                user.AvatarFileId,
                user.PortfolioFileId,
                user.CreatedAtUtc);
        }
    }

    public sealed class GetAdminUsersQueryHandler
        : IRequestHandler<
            GetAdminUsersQuery,
            IReadOnlyList<UserDto>>
    {
        private readonly IUserRepository _userRepository;

        public GetAdminUsersQueryHandler(
            IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IReadOnlyList<UserDto>> Handle(
            GetAdminUsersQuery request,
            CancellationToken cancellationToken)
        {
            var normalizedStatus =
                AdminUserQueryValidation.NormalizeStatus(
                    request.StatusCode);

            var users =
                normalizedStatus is null
                    ? await _userRepository
                        .GetAllWithRoleAsync(
                            cancellationToken)
                    : await _userRepository
                        .GetByStatusAsync(
                            normalizedStatus);

            return users
                .Select(user => user.ToDto())
                .OrderByDescending(user => user.CreatedAtUtc)
                .ToList();
        }
    }

    public sealed class SearchAdminUsersQueryHandler
        : IRequestHandler<
            SearchAdminUsersQuery,
            AdminUserPageDto>
    {
        private readonly IUserRepository _userRepository;

        public SearchAdminUsersQueryHandler(
            IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<AdminUserPageDto> Handle(
            SearchAdminUsersQuery request,
            CancellationToken cancellationToken)
        {
            AdminUserQueryValidation.ValidatePage(
                request.PageNumber,
                request.PageSize);

            var search =
                string.IsNullOrWhiteSpace(request.Search)
                    ? null
                    : request.Search.Trim();

            if (search?.Length > 200)
            {
                throw new InvalidOperationException(
                    "Search text cannot exceed 200 characters.");
            }

            var criteria =
                new UserSearchCriteria(
                    search,
                    AdminUserQueryValidation.NormalizeStatus(
                        request.StatusCode),
                    AdminUserQueryValidation.NormalizeRole(
                        request.RoleName),
                    request.PageNumber,
                    request.PageSize);

            var result =
                await _userRepository.SearchAdminUsersAsync(
                    criteria,
                    cancellationToken);

            var totalPages =
                result.TotalCount == 0
                    ? 0
                    : (int)Math.Ceiling(
                        result.TotalCount
                        / (double)request.PageSize);

            return new AdminUserPageDto(
                result.Items
                    .Select(AdminUserQueryValidation.ToListItem)
                    .ToList(),
                request.PageNumber,
                request.PageSize,
                result.TotalCount,
                totalPages);
        }
    }

    public sealed class GetAdminUserDetailQueryHandler
        : IRequestHandler<
            GetAdminUserDetailQuery,
            AdminUserDetailDto?>
    {
        private readonly IUserRepository _userRepository;

        public GetAdminUserDetailQueryHandler(
            IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<AdminUserDetailDto?> Handle(
            GetAdminUserDetailQuery request,
            CancellationToken cancellationToken)
        {
            if (request.UserId == Guid.Empty)
            {
                return null;
            }

            var user =
                await _userRepository.GetByIdWithRoleAsync(
                    request.UserId,
                    cancellationToken);

            return user is null
                ? null
                : AdminUserQueryValidation.ToDetail(user);
        }
    }

    public sealed class GetAdminUserAuditQueryHandler
        : IRequestHandler<
            GetAdminUserAuditQuery,
            AdminUserAuditPageDto>
    {
        private readonly IAuditEventRepository
            _auditEventRepository;

        public GetAdminUserAuditQueryHandler(
            IAuditEventRepository auditEventRepository)
        {
            _auditEventRepository = auditEventRepository;
        }

        public async Task<AdminUserAuditPageDto> Handle(
            GetAdminUserAuditQuery request,
            CancellationToken cancellationToken)
        {
            AdminUserQueryValidation.ValidatePage(
                request.PageNumber,
                request.PageSize);

            var result =
                await _auditEventRepository.GetForUserAsync(
                    request.UserId,
                    request.PageNumber,
                    request.PageSize,
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
                            new AdminUserAuditEventDto(
                                item.AuditEventId,
                                item.OccurredAtUtc,
                                item.ActorUserId,
                                item.ActorRoleName,
                                item.ActionCode,
                                item.EntityType,
                                item.EntityId,
                                item.DetailJson))
                    .ToList();

            return new AdminUserAuditPageDto(
                items,
                request.PageNumber,
                request.PageSize,
                result.TotalCount,
                totalPages);
        }
    }

    public sealed class GetAdminUserStatusCountsQueryHandler
        : IRequestHandler<
            GetAdminUserStatusCountsQuery,
            AdminUserStatusCountsDto>
    {
        private readonly IUserRepository _userRepository;

        public GetAdminUserStatusCountsQueryHandler(
            IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<AdminUserStatusCountsDto> Handle(
            GetAdminUserStatusCountsQuery request,
            CancellationToken cancellationToken)
        {
            var counts =
                await _userRepository.GetStatusCountsAsync(
                    cancellationToken);

            static int GetCount(
                IReadOnlyDictionary<string, int> source,
                string statusCode)
            {
                return source.TryGetValue(
                    statusCode,
                    out var count)
                    ? count
                    : 0;
            }

            var pending =
                GetCount(counts, "PENDING_APPROVAL");
            var active =
                GetCount(counts, "ACTIVE");
            var disabled =
                GetCount(counts, "DISABLED");
            var rejected =
                GetCount(counts, "REJECTED");

            return new AdminUserStatusCountsDto(
                pending,
                active,
                disabled,
                rejected,
                counts.Values.Sum());
        }
    }

    public sealed class GetAdminUserPortfolioQueryHandler
        : IRequestHandler<
            GetAdminUserPortfolioQuery,
            FileResourceDto?>
    {
        private readonly IUserRepository _userRepository;
        private readonly IFileResourceService
            _fileResourceService;

        public GetAdminUserPortfolioQueryHandler(
            IUserRepository userRepository,
            IFileResourceService fileResourceService)
        {
            _userRepository = userRepository;
            _fileResourceService = fileResourceService;
        }

        public async Task<FileResourceDto?> Handle(
            GetAdminUserPortfolioQuery request,
            CancellationToken cancellationToken)
        {
            var user =
                await _userRepository.GetByIdWithRoleAsync(
                    request.UserId,
                    cancellationToken);

            if (user?.PortfolioFileId is not Guid fileId)
            {
                return null;
            }

            var file =
                await _fileResourceService
                    .GetFileResourceByIdAsync(fileId);

            return file?.DeletedAtUtc is null
                ? file
                : null;
        }
    }
}
