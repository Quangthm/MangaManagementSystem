using MangaManagementSystem.Application.Interfaces;

namespace MangaManagementSystem.Infrastructure.Services
{
    public class BcryptPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
            => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        public bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch
            {
                // Seeded or legacy hashes may not be BCrypt format.
                return false;
            }
        }
    }
}
