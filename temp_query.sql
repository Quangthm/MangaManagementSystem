USE MangaManagementDB;
DECLARE @__actorUserId_0 uniqueidentifier = '3EB316CA-C81B-448C-A0C2-735D125436CD';

SELECT [s].[series_id], [s].[status_code], [s].[title]
FROM [manga].[Series] AS [s]
WHERE EXISTS (
    SELECT 1
    FROM [manga].[SeriesContributor] AS [s0]
    INNER JOIN [auth].[Users] AS [u] ON [s0].[user_id] = [u].[user_id]
    INNER JOIN [auth].[Roles] AS [r] ON [u].[role_id] = [r].[role_id]
    WHERE [s0].[series_id] = [s].[series_id] AND [s0].[user_id] = @__actorUserId_0 AND [s0].[end_date] IS NULL AND [u].[status_code] = N'ACTIVE' AND [r].[role_name] = N'Mangaka')
