USE MangaManagementDB;
GO

DECLARE @PasswordHash NVARCHAR(255) =
N'$2a$12$eBGlrcdEPsP8c6yDmKhnv.OojpFaPqmJ.DcYRswLWEFZAYTwGNDtq';

DECLARE @Now DATETIME2(7) = SYSUTCDATETIME();

;WITH TestUsers AS
(
    SELECT *
    FROM (VALUES
        (N'TestAdmin',   N'Admin@test.com', N'admin',   N'Admin'),
        (N'TestEditor',      N'Editor@test.com',    N'editor',  N'Tantou Editor'),
        (N'TestMangaka',     N'Mangaka@test.com',   N'mangaka', N'Mangaka'),
        (N'TestBoardMember', N'Member@test.com',    N'member',  N'Editorial Board Member'),
        (N'TestBoardChief',  N'Chief@test.com',     N'chief',   N'Editorial Board Chief')
    ) AS v(username, email, display_name, role_name)
)
INSERT INTO [auth].[Users]
(
    [username],
    [email],
    [display_name],
    [password_hash],
    [role_id],
    [status_code],
    [created_at_utc]
)
SELECT
    tu.username,
    tu.email,
    tu.display_name,
    @PasswordHash,
    r.role_id,
    N'ACTIVE',
    @Now
FROM TestUsers tu
INNER JOIN [auth].[Roles] r
    ON r.role_name = tu.role_name
WHERE NOT EXISTS
(
    SELECT 1
    FROM [auth].[Users] u
    WHERE u.username = tu.username
       OR u.email = tu.email
);
GO
