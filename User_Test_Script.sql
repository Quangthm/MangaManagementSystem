USE MangaManagementDB;
GO
INSERT INTO [auth].[Users] (
    [username], 
    [email],
    [display_name],
    [password_hash], 
    [role_id], 
    [status_code], 
    [created_at_utc]
)
VALUES 
(
    'TestRealAdmin',
    'realadmin@test.com',
    'admin',
    '$2a$12$eBGlrcdEPsP8c6yDmKhnv.OojpFaPqmJ.DcYRswLWEFZAYTwGNDtq',
    6,
    'ACTIVE',
    SYSUTCDATETIME()
);

INSERT INTO [auth].[Users] (
    [username], 
    [email],
    [display_name],
    [password_hash], 
    [role_id], 
    [status_code], 
    [created_at_utc]
)
VALUES 
(
    'TestEditor',
    'Editor@test.com',
    'editor',
    '$2a$12$eBGlrcdEPsP8c6yDmKhnv.OojpFaPqmJ.DcYRswLWEFZAYTwGNDtq',
    3,
    'ACTIVE',
    SYSUTCDATETIME()
);


INSERT INTO [auth].[Users] (
    [username], 
    [email],
    [display_name],
    [password_hash], 
    [role_id], 
    [status_code], 
    [created_at_utc]
)
VALUES 
(
    'TestMangaka',
    'Mangaka@test.com',
    'mangaka',
    '$2a$12$eBGlrcdEPsP8c6yDmKhnv.OojpFaPqmJ.DcYRswLWEFZAYTwGNDtq',
    1,
    'ACTIVE',
    SYSUTCDATETIME()
);

INSERT INTO [auth].[Users] (
    [username], 
    [email],
    [display_name],
    [password_hash], 
    [role_id], 
    [status_code], 
    [created_at_utc]
)
VALUES 
(
    'TestBoardMember',
    'Member@test.com',
    'member',
    '$2a$12$eBGlrcdEPsP8c6yDmKhnv.OojpFaPqmJ.DcYRswLWEFZAYTwGNDtq',
    4,
    'ACTIVE',
    SYSUTCDATETIME()
);

INSERT INTO [auth].[Users] (
    [username], 
    [email],
    [display_name],
    [password_hash], 
    [role_id], 
    [status_code], 
    [created_at_utc]
)
VALUES 
(
    'TestBoardChief',
    'Chief@test.com',
    'chief',
    '$2a$12$eBGlrcdEPsP8c6yDmKhnv.OojpFaPqmJ.DcYRswLWEFZAYTwGNDtq',
    5,
    'ACTIVE',
    SYSUTCDATETIME()
);

DECLARE @new_user_id INT;

EXEC auth.usp_User_Create
    @role_name = N'Mangaka',
    @username = N'huy123',
    @email = N'huy@example.com',
    @password_hash = N'hashed_password_here',
    @display_name = NULL,
    @avatar_file_id = NULL,
    @portfolio_file_id = NULL,
    @created_by_user_id = NULL,
    @new_user_id = @new_user_id OUTPUT;

SELECT @new_user_id AS new_user_id;

Delete from Auth.Users where user_id = 10

EXEC sp_helptext 'auth.usp_User_UpdatePortfolioFile';