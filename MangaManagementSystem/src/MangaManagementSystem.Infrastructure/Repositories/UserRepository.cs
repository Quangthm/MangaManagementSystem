using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        private readonly ApplicationDbContext _context;
        public UserRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
        {
            return await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == usernameOrEmail || u.Username == usernameOrEmail);
        }

        public async Task<IReadOnlyList<User>> GetByStatusAsync(string status)
        {
            return await _context.Users
                .Where(u => u.StatusCode == status)
                .OrderBy(u => u.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<int> CreateUserViaProcAsync(
            string roleName,
            string username,
            string email,
            string passwordHash,
            string? displayName = null,
            long? avatarFileId = null,
            long? portfolioFileId = null,
            int? createdByUserId = null)
        {
            // Use parameterized raw SQL to call stored procedure and return new_user_id
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "auth.usp_User_Create";
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@role_name", roleName));
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@username", username));
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@email", email));
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@password_hash", passwordHash));
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@display_name", (object?)displayName ?? System.DBNull.Value));
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@avatar_file_id", (object?)avatarFileId ?? System.DBNull.Value));
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@portfolio_file_id", (object?)portfolioFileId ?? System.DBNull.Value));

            var outParam = new Microsoft.Data.SqlClient.SqlParameter("@new_user_id", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
            cmd.Parameters.Add(outParam);

            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            await cmd.ExecuteNonQueryAsync();

            var newUserId = outParam.Value == System.DBNull.Value ? 0 : (int)outParam.Value;
            return newUserId;
        }
    }
}
