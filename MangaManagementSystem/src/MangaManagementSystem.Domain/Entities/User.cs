using MangaManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Domain.Entities
{
    public class User : BaseEntity
    {
        public int UserId { get; set; }
        public short RoleId { get; set; }
        public Role? Role { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public long? AvatarFileId { get; set; }
        public FileResource? AvatarFile { get; set; }
        public long? PortfolioFileId { get; set; }
        public FileResource? PortfolioFile { get; set; }
        public string Status { get; set; } = "PENDING_APPROVAL";
        public DateTime CreatedAt { get; set; }
        public ICollection<UserRegistrationRequest> RegistrationRequests { get; set; } = new List<UserRegistrationRequest>();
    }
}
