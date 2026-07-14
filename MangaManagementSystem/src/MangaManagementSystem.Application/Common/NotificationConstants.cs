namespace MangaManagementSystem.Application.Common
{
    public static class NotificationTypeCodes
    {
        public const string TaskAssignment =
            "TASK_ASSIGNMENT";

        public const string BoardPoll =
            "BOARD_POLL";

        public const string PublicationFrequencyRequest =
            "PUBLICATION_FREQUENCY_REQUEST";
        public const string SystemMessage =
            "SYSTEM_MESSAGE";
    }

    public static class NotificationRelatedEntityTypes
    {
        public const string ChapterPageTask =
            "ChapterPageTask";

        public const string SeriesBoardPoll =
            "SeriesBoardPoll";

        public const string Series =
            "Series";

        public const string Chapter =
            "Chapter";
    }
}
