USE MangaManagementDB;
GO

INSERT INTO auth.Roles (role_name)
VALUES (N'Mangaka'),
	(N'Assistant'),
	(N'Tantou Editor'),
	(N'Editorial Board Member'),
	(N'Editorial Board Chief'),
	(N'Admin');

INSERT INTO manga.Genre
(
    genre_name,
    description
)
VALUES
(N'Action', N'Fast-paced stories with combat, conflict, or physical intensity.'),
(N'Adventure', N'Stories focused on journeys, exploration, discovery, or quests.'),
(N'Comedy', N'Stories primarily designed around humor and amusing situations.'),
(N'Drama', N'Stories focused on emotional conflict, relationships, and character development.'),
(N'Fantasy', N'Stories involving magical, mythical, supernatural, or imaginary worlds.'),
(N'Horror', N'Stories intended to create fear, suspense, dread, or unease.'),
(N'Mystery', N'Stories centered on secrets, investigations, puzzles, or hidden truths.'),
(N'Romance', N'Stories focused on romantic relationships and emotional intimacy.'),
(N'Sci-Fi', N'Stories involving futuristic science, advanced technology, space, or speculative concepts.'),
(N'Slice of Life', N'Stories focused on everyday experiences, personal routines, and ordinary life moments.'),
(N'Sports', N'Stories centered on sports, athletic competition, teamwork, and personal growth.'),
(N'Historical', N'Stories set in or strongly inspired by past historical periods.'),
(N'Psychological', N'Stories focused on mental conflict, perception, trauma, strategy, or inner emotional tension.'),
(N'Mecha', N'Stories involving piloted robots, mechanical suits, or large-scale mechanical warfare.'),
(N'Music', N'Stories centered on musicians, bands, performances, or the music industry.'),
(N'Gourmet', N'Stories centered on cooking, food culture, restaurants, or culinary competition.');
GO

INSERT INTO manga.Tag
(
    tag_name,
    description
)
VALUES
(N'Based on a Novel', N'The series is adapted from or based on a novel.'),
(N'Based on a Web Novel', N'The series is adapted from or based on a web novel.'),
(N'Based on a Game', N'The series is adapted from or inspired by a game.'),
(N'Original Work', N'The series is an original story not directly adapted from another medium.'),

(N'Isekai', N'The story involves reincarnation, summoning, transportation, or existence in another world.'),
(N'Reincarnation', N'The story involves a character being reborn into a new life or body.'),
(N'Time Travel', N'The story involves movement between different points in time.'),
(N'Regression', N'The story involves a character returning to an earlier point in their life or timeline.'),
(N'Transported to Another World', N'The story includes transportation from one world to another.'),
(N'Game-Like World', N'The story world uses game-like systems, levels, quests, or status windows.'),

(N'School Life', N'The story is significantly set in a school environment.'),
(N'Workplace', N'The story is significantly set around jobs, offices, or professional life.'),
(N'Royalty', N'The story includes kings, queens, princes, princesses, nobles, or royal succession.'),
(N'Nobility', N'The story includes noble families, aristocratic society, or noble ranking systems.'),
(N'Military', N'The story involves soldiers, armies, military organizations, or warfare structures.'),
(N'Dungeon', N'The story includes dungeons, raids, monsters, or dungeon exploration.'),
(N'Post-Apocalyptic', N'The story is set after a major disaster or collapse of civilization.'),

(N'Magic', N'Magic is an important element of the story world or plot.'),
(N'Martial Arts', N'The story features martial arts training, combat techniques, or fighting schools.'),
(N'Swordsmanship', N'The story prominently features sword fighting or sword-based combat.'),
(N'Monsters', N'The story includes monsters as important enemies, creatures, or world elements.'),
(N'Demons', N'The story includes demons or demon-like beings as important elements.'),
(N'Vampires', N'The story includes vampires or vampire-like beings.'),
(N'Ghosts', N'The story includes ghosts, spirits, hauntings, or ghost-related plot elements.'),
(N'Mythology', N'The story draws heavily from myths, legends, gods, or folklore.'),

(N'Male Protagonist', N'The main protagonist is male.'),
(N'Female Protagonist', N'The main protagonist is female.'),
(N'Smart Protagonist', N'The protagonist is known for intelligence, planning, or strategy.'),
(N'Overpowered Protagonist', N'The protagonist is significantly stronger or more capable than most characters.'),
(N'Weak to Strong', N'The protagonist starts weak and grows stronger over time.'),
(N'Hard-Working Protagonist', N'The protagonist is characterized by effort, persistence, and growth.'),
(N'Determined Protagonist', N'The protagonist strongly pursues a goal despite obstacles.'),
(N'Kind Protagonist', N'The protagonist is notably kind, empathetic, or compassionate.'),
(N'Antihero Protagonist', N'The protagonist has morally gray traits or methods.'),
(N'Misunderstood Protagonist', N'The protagonist is frequently misunderstood or misjudged by others.'),
(N'Hidden Identity', N'The story includes a character hiding their true identity, status, or background.'),
(N'Masked Character/s', N'The series includes important characters who wear masks or hide their identity.'),

(N'Revenge', N'Revenge is a major motivation or recurring story theme.'),
(N'Survival', N'The story includes survival-focused conflict, danger, or harsh conditions.'),
(N'Tournament', N'The story includes tournaments, competitions, or ranked contests.'),
(N'Training', N'The story includes training arcs, skill development, or improvement through practice.'),
(N'Found Family', N'The story includes characters forming a family-like bond outside blood relations.'),
(N'Coming of Age', N'The story follows personal growth, maturity, or transition into adulthood.'),
(N'Redemption', N'The story includes a character seeking forgiveness, change, or moral recovery.'),
(N'Betrayal', N'The story includes betrayal as an important plot event or recurring conflict.'),

(N'Love Triangle', N'The story includes romantic tension involving three central characters.'),
(N'Slow Burn Romance', N'The romantic relationship develops gradually over time.'),
(N'Enemies to Lovers', N'The story includes characters moving from conflict or rivalry into romance.'),
(N'Childhood Friends', N'The story includes important relationships between characters who knew each other since childhood.'),

(N'Dark Tone', N'The story has a serious, grim, violent, or emotionally heavy tone.'),
(N'Lighthearted Tone', N'The story has a relaxed, gentle, or cheerful tone.'),
(N'Comedic Undertone', N'The story contains noticeable humorous elements without being primarily comedy.'),
(N'Tragic Past', N'The story includes a character with a painful or traumatic backstory.'),
(N'Moral Ambiguity', N'The story includes difficult choices, gray morality, or unclear right and wrong.');
GO
INSERT INTO manga.PublicationPeriod (
    period_name,
    period_type_code,
    period_start_date,
    period_end_date
)
VALUES
-- Yearly period
(N'2026_YEAR', N'YEARLY', '2026-01-01', '2026-12-31'),

-- Monthly periods
(N'2026_JUNE', N'MONTHLY', '2026-06-01', '2026-06-30'),
(N'2026_JULY', N'MONTHLY', '2026-07-01', '2026-07-31'),
(N'2026_AUGUST', N'MONTHLY', '2026-08-01', '2026-08-31'),

-- June weekly periods
(N'2026_JUNE_WEEK1', N'WEEKLY', '2026-06-01', '2026-06-07'),
(N'2026_JUNE_WEEK2', N'WEEKLY', '2026-06-08', '2026-06-14'),
(N'2026_JUNE_WEEK3', N'WEEKLY', '2026-06-15', '2026-06-21'),
(N'2026_JUNE_WEEK4', N'WEEKLY', '2026-06-22', '2026-06-28'),

-- July weekly periods
-- 2026-06-29 to 2026-07-05 has 5 days in July, so it belongs to July.
(N'2026_JULY_WEEK1', N'WEEKLY', '2026-06-29', '2026-07-05'),
(N'2026_JULY_WEEK2', N'WEEKLY', '2026-07-06', '2026-07-12'),
(N'2026_JULY_WEEK3', N'WEEKLY', '2026-07-13', '2026-07-19'),
(N'2026_JULY_WEEK4', N'WEEKLY', '2026-07-20', '2026-07-26'),
-- 2026-07-27 to 2026-08-02 has 5 days in July, so it belongs to July.
(N'2026_JULY_WEEK5', N'WEEKLY', '2026-07-27', '2026-08-02'),

-- August weekly periods
(N'2026_AUGUST_WEEK1', N'WEEKLY', '2026-08-03', '2026-08-09'),
(N'2026_AUGUST_WEEK2', N'WEEKLY', '2026-08-10', '2026-08-16'),
(N'2026_AUGUST_WEEK3', N'WEEKLY', '2026-08-17', '2026-08-23'),
(N'2026_AUGUST_WEEK4', N'WEEKLY', '2026-08-24', '2026-08-30');