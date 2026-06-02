USE MangaManagementDB;
GO
INSERT INTO [auth].[Users] (
    [username], 
    [email], 
    [password_hash], 
    [role_id], 
    [status_code], 
    [created_at_utc]
)
VALUES 
(
    'TestRealAdmin',
    'realadmin@test.com',
    '$2a$12$eBGlrcdEPsP8c6yDmKhnv.OojpFaPqmJ.DcYRswLWEFZAYTwGNDtq',
    6,
    'ACTIVE',
    SYSUTCDATETIME()
);

DECLARE @new_user_id INT;

EXEC auth.usp_User_Create
    @role_name = N'Mangaka',
    @username = N'tuan123',
    @email = N'tuan@example.com',
    @password_hash = N'hashed_password_here',
    @display_name = NULL,
    @avatar_file_id = NULL,
    @portfolio_file_id = NULL,
    @created_by_user_id = NULL,
    @new_user_id = @new_user_id OUTPUT;

SELECT @new_user_id AS new_user_id;
