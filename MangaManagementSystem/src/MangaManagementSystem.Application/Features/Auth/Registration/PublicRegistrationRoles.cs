namespace MangaManagementSystem.Application.Features.Auth.Registration
{
    public static class PublicRegistrationRoles
    {
        public const string Mangaka = "Mangaka";
        public const string Assistant = "Assistant";
        public const string TantouEditor = "Tantou Editor";
        public const string EditorialBoardMember = "Editorial Board Member";
        public const string EditorialBoardChief = "Editorial Board Chief";

        private static readonly IReadOnlyDictionary<string, string>
            CanonicalRoleNames =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    [Mangaka] = Mangaka,
                    [Assistant] = Assistant,
                    [TantouEditor] = TantouEditor,
                    [EditorialBoardMember] = EditorialBoardMember,
                    [EditorialBoardChief] = EditorialBoardChief
                };

        public static IReadOnlyCollection<string> All =>
            CanonicalRoleNames.Values.ToArray();

        public static bool TryNormalize(
            string? roleName,
            out string normalizedRoleName)
        {
            normalizedRoleName = string.Empty;

            if (string.IsNullOrWhiteSpace(roleName))
            {
                return false;
            }

            if (!CanonicalRoleNames.TryGetValue(
                    roleName.Trim(),
                    out var canonicalRoleName))
            {
                return false;
            }

            normalizedRoleName = canonicalRoleName;
            return true;
        }

        public static string NormalizeOrThrow(string? roleName)
        {
            if (TryNormalize(roleName, out var normalizedRoleName))
            {
                return normalizedRoleName;
            }

            throw new InvalidOperationException(
                "The selected role is not available for public registration.");
        }
    }
}
