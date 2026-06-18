namespace MangaManagementSystem.Application.DTOs.Auth
{
    public static class AuthErrorCodes
    {
        public const string InvalidCredentials = "invalid_credentials";
        public const string AccountPending = "account_pending";
        public const string AccountRejected = "account_rejected";
        public const string AccountDisabled = "account_disabled";
        public const string AccountNotFound = "account_not_found";
        public const string AccountConfigurationInvalid = "account_configuration_invalid";
        public const string GoogleEmailMissing = "google_email_missing";
        public const string GoogleOAuthFailed = "google_oauth_failed";
        public const string GoogleSignupFailed = "google_signup_failed";
        public const string InvalidRole = "invalid_role";
        public const string RecaptchaFailed = "recaptcha_failed";
        public const string InvalidOtp = "invalid_otp";
        public const string EmailRequired = "email_required";
        public const string EmailAlreadyExists = "email_already_exists";
        public const string UsernameTaken = "username_taken";
        public const string RegistrationStartFailed = "registration_start_failed";
        public const string RegistrationCompleteFailed = "registration_complete_failed";
        public const string ValidationFailed = "validation_failed";
        public const string UnauthorizedInternalRequest = "unauthorized_internal_request";
        public const string InvalidRequest = "invalid_request";
        public const string RequestFailed = "request_failed";
    }
}
