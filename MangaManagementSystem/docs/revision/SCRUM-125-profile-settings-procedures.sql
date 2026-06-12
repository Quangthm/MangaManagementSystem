USE MangaManagementDB;
GO

CREATE OR ALTER PROCEDURE auth.usp_User_UpdateDisplayName
    @user_id UNIQUEIDENTIFIER,
    @display_name NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF @user_id IS NULL
    BEGIN
        THROW 50001, 'user_id is required.', 1;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM auth.Users
        WHERE user_id = @user_id
    )
    BEGIN
        THROW 50002, 'User was not found.', 1;
    END;

    IF @display_name IS NULL OR LTRIM(RTRIM(@display_name)) = ''
    BEGIN
        THROW 50003, 'Display name cannot be empty.', 1;
    END;

    UPDATE auth.Users
    SET display_name = LTRIM(RTRIM(@display_name))
    WHERE user_id = @user_id;
END;
GO

CREATE OR ALTER PROCEDURE auth.usp_User_UpdateAvatarFile
    @user_id UNIQUEIDENTIFIER,
    @avatar_file_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF @user_id IS NULL
    BEGIN
        THROW 50011, 'user_id is required.', 1;
    END;

    IF @avatar_file_id IS NULL
    BEGIN
        THROW 50012, 'avatar_file_id is required.', 1;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM auth.Users
        WHERE user_id = @user_id
    )
    BEGIN
        THROW 50013, 'User was not found.', 1;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM manga.FileResource
        WHERE file_resource_id = @avatar_file_id
          AND deleted_at_utc IS NULL
    )
    BEGIN
        THROW 50014, 'Avatar file resource was not found or is unavailable.', 1;
    END;

    UPDATE auth.Users
    SET avatar_file_id = @avatar_file_id
    WHERE user_id = @user_id;
END;
GO

CREATE OR ALTER PROCEDURE auth.usp_User_UpdatePortfolioFile
    @user_id UNIQUEIDENTIFIER,
    @portfolio_file_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF @user_id IS NULL
    BEGIN
        THROW 50021, 'user_id is required.', 1;
    END;

    IF @portfolio_file_id IS NULL
    BEGIN
        THROW 50022, 'portfolio_file_id is required.', 1;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM auth.Users
        WHERE user_id = @user_id
    )
    BEGIN
        THROW 50023, 'User was not found.', 1;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM manga.FileResource
        WHERE file_resource_id = @portfolio_file_id
          AND deleted_at_utc IS NULL
    )
    BEGIN
        THROW 50024, 'Portfolio file resource was not found or is unavailable.', 1;
    END;

    UPDATE auth.Users
    SET portfolio_file_id = @portfolio_file_id
    WHERE user_id = @user_id;
END;
GO

CREATE OR ALTER PROCEDURE auth.usp_User_ResetPassword
    @user_id UNIQUEIDENTIFIER,
    @password_hash NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    IF @user_id IS NULL
    BEGIN
        THROW 50031, 'user_id is required.', 1;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM auth.Users
        WHERE user_id = @user_id
    )
    BEGIN
        THROW 50032, 'User was not found.', 1;
    END;

    IF @password_hash IS NULL OR LTRIM(RTRIM(@password_hash)) = ''
    BEGIN
        THROW 50033, 'password_hash cannot be empty.', 1;
    END;

    UPDATE auth.Users
    SET password_hash = @password_hash
    WHERE user_id = @user_id;
END;
GO