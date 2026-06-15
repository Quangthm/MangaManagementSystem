namespace MangaManagementSystem.Infrastructure.Options
{
    public class SmtpSettings
    {
        public const string SectionName = "Smtp";

        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = "noreply@mangaflow.local";
        public string FromName { get; set; } = "MangaFlow";
        public bool UseMock { get; set; } = false;
        public bool UseSsl { get; set; }
    }
}
