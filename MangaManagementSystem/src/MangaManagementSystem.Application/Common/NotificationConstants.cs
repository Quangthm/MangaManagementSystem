namespace MangaManagementSystem.Application.Common
{
    public static class NotificationTypeCodes
    {
        public const string ProposalReview =
            "PROPOSAL_REVIEW";

        public const string ProposalDecision =
            "PROPOSAL_DECISION";

        public const string TaskAssignment =
            "TASK_ASSIGNMENT";

        public const string TaskReview =
            "TASK_REVIEW";

        public const string BoardPoll =
            "BOARD_POLL";

        public const string BoardDecision =
            "BOARD_DECISION";

        public const string ChapterReview =
            "CHAPTER_REVIEW";

        public const string ChapterDecision =
            "CHAPTER_DECISION";

        public const string PublicationSchedule =
            "PUBLICATION_SCHEDULE";

        public const string SystemMessage =
            "SYSTEM_MESSAGE";
    }

    public static class NotificationRelatedEntityTypes
    {
        public const string SeriesProposal =
            "SeriesProposal";

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
