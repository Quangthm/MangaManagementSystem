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