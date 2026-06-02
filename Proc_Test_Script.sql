USE MangaManagementDB;
GO
DECLARE @new_user_id INT;

EXEC auth.usp_User_Create
    @role_name = N'Admin',
    @username = N'tuan123',
    @email = N'tuan@example.com',
    @password_hash = N'hashed_password_here',
    @display_name = NULL,
    @avatar_file_id = NULL,
    @portfolio_file_id = NULL,
    @created_by_user_id = NULL,
    @new_user_id = @new_user_id OUTPUT;

SELECT @new_user_id AS new_user_id;

EXEC auth.usp_User_Create
    @role_name = N'Tantou Editor',
    @username = N'editor01',
    @email = N'editor01@example.com',
    @password_hash = N'hashed_password_here_123456789',
    @display_name = N'Editor One',
    @avatar_file_id = NULL,
    @portfolio_file_id = NULL,
    @created_by_user_id = 1,
    @new_user_id = @new_user_id OUTPUT;

SELECT @new_user_id AS new_user_id;

EXEC auth.usp_Admin_ChangeUserStatus
    @admin_user_id = 1,
    @target_user_id = 5,
    @new_status_code = N'ACTIVE',
    @reason = N'User registration approved.';

EXEC auth.usp_User_ResetPassword
    @target_user_id = 5,
    @new_password_hash = N'new_hashed_password_here_123456789',
    @actor_user_id = 5,
    @reset_mode = N'SELF_CHANGE',
    @reset_reason = NULL;

EXEC auth.usp_User_UpdateDisplayName
    @user_id = 5,
    @display_name = N'Tuan Như',
    @actor_user_id = 5;