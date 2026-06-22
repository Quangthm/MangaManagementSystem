using System.Collections.Generic;

namespace MangaManagementSystem.Application.Common.Constants
{
    public static class ChapterPageAnnotationIssueTypes
    {
        public const string BackgroundInconsistency = "BACKGROUND_INCONSISTENCY";
        public const string ShadingError = "SHADING_ERROR";
        public const string EffectsError = "EFFECTS_ERROR";
        public const string CleanupRequired = "CLEANUP_REQUIRED";
        public const string DialogueError = "DIALOGUE_ERROR";
        public const string TypesettingError = "TYPESETTING_ERROR";
        public const string TranslationError = "TRANSLATION_ERROR";
        public const string PanelOrderError = "PANEL_ORDER_ERROR";
        public const string CharacterAnatomyError = "CHARACTER_ANATOMY_ERROR";
        public const string ContinuityError = "CONTINUITY_ERROR";
        public const string Other = "OTHER";

        public static readonly IReadOnlyList<string> All = new[]
        {
            BackgroundInconsistency,
            ShadingError,
            EffectsError,
            CleanupRequired,
            DialogueError,
            TypesettingError,
            TranslationError,
            PanelOrderError,
            CharacterAnatomyError,
            ContinuityError,
            Other
        };
    }
}
