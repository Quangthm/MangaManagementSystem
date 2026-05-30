using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class UserRegistrationRequest : BaseEntity
    {
        public long RegistrationRequestId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public short RequestedRoleId { get; set; }
        public Role? RequestedRole { get; set; }
        public long? PortfolioFileId { get; set; }
        public FileResource? PortfolioFile { get; set; }
        public string Status { get; set; } = "PENDING";
        public int? ReviewedByUserId { get; set; }
        public User? ReviewedByUser { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
