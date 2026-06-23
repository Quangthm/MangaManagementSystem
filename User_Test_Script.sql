USE MangaManagementDB;
GO

DECLARE @PasswordHash NVARCHAR(255) =
N'$2a$12$eBGlrcdEPsP8c6yDmKhnv.OojpFaPqmJ.DcYRswLWEFZAYTwGNDtq';

DECLARE @Now DATETIME2(7) = SYSUTCDATETIME();

----------------------------------------------------------------------
-- Validate required roles exist first
----------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM [auth].[Roles] WHERE role_name = N'Admin')
BEGIN
    ;THROW 59001, 'Role Admin does not exist.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM [auth].[Roles] WHERE role_name = N'Tantou Editor')
BEGIN
    ;THROW 59002, 'Role Tantou Editor does not exist.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM [auth].[Roles] WHERE role_name = N'Mangaka')
BEGIN
    ;THROW 59003, 'Role Mangaka does not exist.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM [auth].[Roles] WHERE role_name = N'Editorial Board Member')
BEGIN
    ;THROW 59004, 'Role Editorial Board Member does not exist.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM [auth].[Roles] WHERE role_name = N'Editorial Board Chief')
BEGIN
    ;THROW 59005, 'Role Editorial Board Chief does not exist.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM [auth].[Roles] WHERE role_name = N'Assistant')
BEGIN
    ;THROW 59006, 'Role Assistant does not exist.', 1;
END;

----------------------------------------------------------------------
-- Seed test users
----------------------------------------------------------------------

;WITH Numbers AS
(
    SELECT *
    FROM (VALUES
        (1),
        (2),
        (3),
        (4),
        (5)
    ) AS n(num)
),
GeneratedUsers AS
(
    -- 1 Admin only
    SELECT
        N'TestAdmin' AS username,
        N'TestAdmin@test.com' AS email,
        N'TestAdmin' AS display_name,
        N'Admin' AS role_name

    UNION ALL

    -- 5 Tantou Editors
    SELECT
        CONCAT(N'TestEditor', num),
        CONCAT(N'TestEditor', num, N'@test.com'),
        CONCAT(N'TestEditor', num),
        N'Tantou Editor'
    FROM Numbers

    UNION ALL

    -- 5 Mangaka
    SELECT
        CONCAT(N'TestMangaka', num),
        CONCAT(N'TestMangaka', num, N'@test.com'),
        CONCAT(N'TestMangaka', num),
        N'Mangaka'
    FROM Numbers

    UNION ALL

    -- 5 Editorial Board Members
    SELECT
        CONCAT(N'TestBoardMember', num),
        CONCAT(N'TestBoardMember', num, N'@test.com'),
        CONCAT(N'TestBoardMember', num),
        N'Editorial Board Member'
    FROM Numbers

    UNION ALL

    -- 5 Editorial Board Chiefs
    SELECT
        CONCAT(N'TestBoardChief', num),
        CONCAT(N'TestBoardChief', num, N'@test.com'),
        CONCAT(N'TestBoardChief', num),
        N'Editorial Board Chief'
    FROM Numbers

    UNION ALL

    -- 5 Assistants
    SELECT
        CONCAT(N'TestAssistant', num),
        CONCAT(N'TestAssistant', num, N'@test.com'),
        CONCAT(N'TestAssistant', num),
        N'Assistant'
    FROM Numbers
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
    gu.username,
    gu.email,
    gu.display_name,
    @PasswordHash,
    r.role_id,
    N'ACTIVE',
    @Now
FROM GeneratedUsers gu
INNER JOIN [auth].[Roles] r
    ON r.role_name = gu.role_name
WHERE NOT EXISTS
(
    SELECT 1
    FROM [auth].[Users] u
    WHERE u.username = gu.username
       OR u.email = gu.email
);

----------------------------------------------------------------------
-- Verification
----------------------------------------------------------------------

SELECT
    u.username,
    u.email,
    u.display_name,
    r.role_name,
    u.status_code,
    u.created_at_utc
FROM [auth].[Users] u
INNER JOIN [auth].[Roles] r
    ON r.role_id = u.role_id
WHERE u.username = N'TestAdmin'
   OR u.username LIKE N'TestEditor%'
   OR u.username LIKE N'TestMangaka%'
   OR u.username LIKE N'TestBoardMember%'
   OR u.username LIKE N'TestBoardChief%'
   OR u.username LIKE N'TestAssistant%'
ORDER BY
    CASE r.role_name
        WHEN N'Admin' THEN 1
        WHEN N'Mangaka' THEN 2
        WHEN N'Assistant' THEN 3
        WHEN N'Tantou Editor' THEN 4
        WHEN N'Editorial Board Member' THEN 5
        WHEN N'Editorial Board Chief' THEN 6
        ELSE 99
    END,
    u.username;
GO
