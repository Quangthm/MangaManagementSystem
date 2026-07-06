namespace MangaManagementSystem.Web.Options
{
    public sealed class AuthenticationSessionOptions
    {
        public const string SectionName =
            "AuthenticationSession";

        public int CookieExpireMinutes { get; set; } =
            120;
    }
}