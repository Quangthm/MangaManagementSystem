using MangaManagementSystem.Application.DTOs.Admin;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Application.Mappers;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Admin.Users.Queries
{
    public sealed class GetAdminUsersQueryHandler
        : IRequestHandler<
            GetAdminUsersQuery,
            IReadOnlyList<UserDto>>
    {
        private static readonly HashSet<string>
            AllowedStatusCodes =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    "PENDING_APPROVAL",
                    "ACTIVE",
                    "DISABLED",
                    "REJECTED"
                };

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
                string.IsNullOrWhiteSpace(request.StatusCode)
                    ? null
                    : request.StatusCode.Trim().ToUpperInvariant();

            if (normalizedStatus is not null
                && !AllowedStatusCodes.Contains(
                    normalizedStatus))
            {
                throw new InvalidOperationException(
                    "The requested user status is not supported.");
            }

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
                await _userRepository
                    .GetStatusCountsAsync(
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
                GetCount(
                    counts,
                    "PENDING_APPROVAL");

            var active =
                GetCount(
                    counts,
                    "ACTIVE");

            var disabled =
                GetCount(
                    counts,
                    "DISABLED");

            var rejected =
                GetCount(
                    counts,
                    "REJECTED");

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
                await _userRepository
                    .GetByIdWithRoleAsync(
                        request.UserId,
                        cancellationToken);

            if (user?.PortfolioFileId is not Guid fileId)
            {
                return null;
            }

            var file =
                await _fileResourceService
                    .GetFileResourceByIdAsync(
                        fileId);

            return file?.DeletedAtUtc is null
                ? file
                : null;
        }
    }
}
