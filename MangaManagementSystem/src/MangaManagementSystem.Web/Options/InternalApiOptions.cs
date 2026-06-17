namespace MangaManagementSystem.Web.Options
{
    public sealed class InternalApiOptions
    {
        public const string SectionName =
            "InternalApi";

        public const string HeaderName =
            "X-Internal-Api-Key";

        public const string ActorUserIdHeaderName =
            "X-Actor-User-Id";

        public const string ActorRoleHeaderName =
            "X-Actor-Role";

        public string Key { get; set; } =
            string.Empty;
    }
}
