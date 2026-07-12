namespace MangaManagementSystem.Application.Common.Constants;

/// <summary>
/// Error numbers raised by usp_ChapterPageTask_SubmitForReview (58101-58106).
/// </summary>
public static class SubmitForReviewErrors
{
    public const int TaskLocked = 58101;
    public const int TaskNotFound = 58102;
    public const int NotInAssignedStatus = 58103;
    public const int NotAssignedToActor = 58104;
    public const int PageVersionNotFound = 58105;
    public const int PageVersionMismatch = 58106;
}

/// <summary>
/// Error numbers raised by cancel/approve/return-for-rework/reassign SPs.
/// Prefix 582 = Cancel, 583 = Approve, 584 = ReturnForRework, 585 = Reassign.
/// </summary>
public static class MangakaTaskErrors
{
    public const int LockAcquisitionFailed = 58201;
    public const int TaskNotFound = 58202;
    public const int CancelWrongStatus = 58203;

    public const int ApproveLockFailed = 58301;
    public const int ApproveTaskNotFound = 58302;
    public const int ApproveWrongStatus = 58303;

    public const int ReturnLockFailed = 58401;
    public const int ReturnTaskNotFound = 58402;
    public const int ReturnWrongStatus = 58403;
    public const int ReturnNotContributor = 58406;

    public const int ReassignLockFailed = 58501;
    public const int ReassignTaskNotFound = 58502;
    public const int ReassignCompletedOrCancelled = 58503;
    public const int ReassignSameUser = 58504;
    public const int ReassignReasonRequired = 58505;
    public const int ReassignNotContributor = 58508;
}

/// <summary>
/// General SQL error codes.
/// </summary>
public static class SqlErrors
{
    public const int UniqueConstraintViolation = 2627;
    public const int DuplicateKey = 2601;
}
