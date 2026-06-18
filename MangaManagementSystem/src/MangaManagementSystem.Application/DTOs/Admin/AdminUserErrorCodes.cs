namespace MangaManagementSystem.Application.DTOs.Admin
{
    public static class AdminUserErrorCodes
    {
        public const string InvalidStatus = "admin_user_invalid_status";
        public const string InvalidTransition = "admin_user_invalid_transition";
        public const string UserNotFound = "admin_user_not_found";
        public const string PortfolioNotFound = "admin_user_portfolio_not_found";
        public const string AccessDenied = "admin_access_denied";
        public const string RequestFailed = "admin_request_failed";
    }
}
