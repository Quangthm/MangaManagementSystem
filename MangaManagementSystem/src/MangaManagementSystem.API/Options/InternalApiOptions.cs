namespace MangaManagementSystem.API.Options
{
    public sealed class InternalApiOptions
    {
        public const string SectionName =
            "InternalApi";

        public const string HeaderName =
            "X-Internal-Api-Key";

        public string Key { get; set; } =
            string.Empty;
    }
}
