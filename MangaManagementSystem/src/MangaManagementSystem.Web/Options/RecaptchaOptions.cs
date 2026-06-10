namespace MangaManagementSystem.Web.Options
{
    public class RecaptchaOptions
    {
        public const string SectionName = "Recaptcha";
        public string SiteKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
    }
}
