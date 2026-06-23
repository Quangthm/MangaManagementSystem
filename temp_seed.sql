USE MangaManagementDB;

INSERT INTO manga.Genre (genre_name, description)
SELECT name, descr FROM (
    VALUES
    (N'Action', N'Fast-paced stories with combat, conflict, or physical intensity.'),
    (N'Adventure', N'Stories focused on journeys, exploration, discovery, or quests.'),
    (N'Comedy', N'Stories primarily designed around humor and amusing situations.'),
    (N'Drama', N'Stories focused on emotional conflict, relationships, and character development.'),
    (N'Fantasy', N'Stories involving magical, mythical, supernatural, or imaginary worlds.'),
    (N'Romance', N'Stories centered around romantic relationships and love.'),
    (N'Sci-Fi', N'Science fiction stories involving futuristic technology, space exploration, etc.'),
    (N'Slice of Life', N'Stories depicting everyday life experiences and situations.')
) AS temp(name, descr)
WHERE NOT EXISTS (SELECT 1 FROM manga.Genre WHERE genre_name = temp.name);

INSERT INTO manga.Tag (tag_name, description)
SELECT name, descr FROM (
    VALUES
    (N'Magic', N'Involves magical powers or systems.'),
    (N'Mecha', N'Involves giant robots or mechs.'),
    (N'School Life', N'Set in a school environment.'),
    (N'Supernatural', N'Involves supernatural elements like ghosts, spirits, etc.'),
    (N'Isekai', N'Protagonist is transported to another world.'),
    (N'Martial Arts', N'Focuses on martial arts combat.'),
    (N'Psychological', N'Focuses on psychological elements and mind games.')
) AS temp(name, descr)
WHERE NOT EXISTS (SELECT 1 FROM manga.Tag WHERE tag_name = temp.name);
