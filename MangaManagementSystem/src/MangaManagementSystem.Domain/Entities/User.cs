using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Domain.Entities
{
    public class User
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Role? Role { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public Guid? AvatarFileId { get; set; }
        public FileResource? AvatarFile { get; set; }
        public Guid? PortfolioFileId { get; set; }
        public FileResource? PortfolioFile { get; set; }
        public string StatusCode { get; set; } = "PENDING_APPROVAL";
        public DateTime CreatedAtUtc { get; set; }
    }
}
