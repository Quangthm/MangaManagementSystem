using MangaManagementSystem.Domain.Common;
using System.Collections.Generic;

namespace MangaManagementSystem.Domain.Entities
{
public class Role : BaseEntity
{
    public Guid RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
