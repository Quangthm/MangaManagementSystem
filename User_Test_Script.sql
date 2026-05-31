-- =========================================================================
-- MỤC ĐÍCH: Tạo 3 tài khoản mặc định để test luồng Đăng nhập và Phân quyền
-- MẬT KHẨU MẶC ĐỊNH CHO CẢ 3 TÀI KHOẢN LÀ: Password123!
-- =========================================================================

-- (Tùy chọn) Xóa dữ liệu cũ nếu muốn làm sạch bảng trước khi test
-- TRUNCATE TABLE [auth].[Users];
USE MangaManagementDB
GO;


INSERT INTO auth.Users (
    Username,
    Email, 
    PasswordHash, 
    RoleId, 
    Status, 
    CreatedAt
)
VALUES 
-- 1. Tài khoản test Mangaka (RoleId = 1) -> Redirect về /dashboard/mangaka
(
    'TestMangaka',
    'mangaka@test.com', 
    '$2a$11$N9xlhu2kF.g5yEId4y1Vq.VnQ6/vP4o7S02W.wI1hV5p02aR7Q9vO', 
    1, 
    'ACTIVE', 
    GETUTCDATE()
),

-- 2. Tài khoản test Editor (RoleId = 3) -> Redirect về /dashboard/editor
(
    'TestEditor',
    'editor@test.com', 
    '$2a$11$N9xlhu2kF.g5yEId4y1Vq.VnQ6/vP4o7S02W.wI1hV5p02aR7Q9vO', 
    3, 
    'ACTIVE', 
    GETUTCDATE()
),

-- 3. Tài khoản test Admin (RoleId = 5) -> Redirect về /admin/user-approval
(
    'TestAdmin',
    'admin@test.com', 
    '$2a$11$N9xlhu2kF.g5yEId4y1Vq.VnQ6/vP4o7S02W.wI1hV5p02aR7Q9vO', 
    5, 
    'ACTIVE', 
    GETUTCDATE()
),
-- 4. Tài khoản test Assistant (RoleId = 2) -> Báo lỗi 

(
    'TestEditor',
    'editor@test.com', 
    '$2a$11$N9xlhu2kF.g5yEId4y1Vq.VnQ6/vP4o7S02W.wI1hV5p02aR7Q9vO', 
    2, 
    'PENDING_APPROVAL', 
    GETUTCDATE()
);