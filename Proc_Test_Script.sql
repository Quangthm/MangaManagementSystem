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

DECLARE @new_user_id INT;
DECLARE @portfolio_file_resource_id BIGINT;

EXEC auth.usp_User_CreateWithOptionalPortfolio
    @role_name = N'Mangaka',
    @username = N'tuan123',
    @email = N'tuan@example.com',
    @password_hash = N'hashed_password_here_123456789',
    @display_name = N'Tuan Như',
    @avatar_file_id = NULL,

    @portfolio_original_file_name = NULL,
    @portfolio_cloudinary_public_id = NULL,
    @portfolio_cloudinary_secure_url = NULL,
    @portfolio_content_type = NULL,
    @portfolio_file_size_bytes = NULL,
    @portfolio_sha256_hash = NULL,

    @created_by_user_id = NULL,
    @new_user_id = @new_user_id OUTPUT,
    @portfolio_file_resource_id = @portfolio_file_resource_id OUTPUT;

SELECT
    @new_user_id AS new_user_id,
    @portfolio_file_resource_id AS portfolio_file_resource_id;

DECLARE @new_user_id INT;
DECLARE @portfolio_file_resource_id BIGINT;

EXEC auth.usp_User_CreateWithOptionalPortfolio
    @role_name = N'Mangaka',
    @username = N'tuan456',
    @email = N'tuan456@example.com',
    @password_hash = N'hashed_password_here_123456789',
    @display_name = N'Tuan Như',
    @avatar_file_id = NULL,

    @portfolio_original_file_name = N'portfolio.pdf',
    @portfolio_cloudinary_public_id = N'manga/portfolios/portfolio_001',
    @portfolio_cloudinary_secure_url = N'https://res.cloudinary.com/example/portfolio.pdf',
    @portfolio_content_type = N'application/pdf',
    @portfolio_file_size_bytes = 204800,
    @portfolio_sha256_hash = N'0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF',

    @created_by_user_id = NULL,
    @new_user_id = @new_user_id OUTPUT,
    @portfolio_file_resource_id = @portfolio_file_resource_id OUTPUT;

SELECT
    @new_user_id AS new_user_id,
    @portfolio_file_resource_id AS portfolio_file_resource_id;
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

EXEC manga.usp_FileResource_SoftDelete
    @file_resource_id = 10,
    @deleted_by_user_id = 1,
    @delete_reason = N'Admin removed unused uploaded file.';

DECLARE @series_proposal_id BIGINT;

EXEC manga.usp_SeriesProposal_Submit
    @series_id = 1,
    @submitted_by_user_id = 5,
    @proposal_title = N'My Manga Proposal',
    @synopsis_snapshot = N'A short synopsis of the submitted proposal.',
    @genre_snapshot = N'Action, Fantasy',
    @original_file_name = N'proposal.pdf',
    @cloudinary_public_id = N'manga/proposals/proposal_001',
    @cloudinary_secure_url = N'https://res.cloudinary.com/example/proposal.pdf',
    @content_type = N'application/pdf',
    @file_size_bytes = 204800,
    @sha256_hash = NULL,
    @series_proposal_id = @series_proposal_id OUTPUT;

SELECT @series_proposal_id AS series_proposal_id;