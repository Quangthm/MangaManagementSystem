USE MangaManagementDB;

IF OBJECT_ID('manga.Genre', 'U') IS NULL
BEGIN
    CREATE TABLE manga.Genre
    (
        genre_id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT df_genre_id DEFAULT NEWID(),
        genre_name NVARCHAR(100) NOT NULL,
        description NVARCHAR(500) NULL,
        CONSTRAINT pk_genre PRIMARY KEY (genre_id),
        CONSTRAINT uq_genre_name UNIQUE (genre_name)
    );
END

IF OBJECT_ID('manga.SeriesGenre', 'U') IS NULL
BEGIN
    CREATE TABLE manga.SeriesGenre
    (
        series_id UNIQUEIDENTIFIER NOT NULL,
        genre_id UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT pk_series_genre PRIMARY KEY (series_id, genre_id)
    );
END

IF OBJECT_ID('manga.Tag', 'U') IS NULL
BEGIN
    CREATE TABLE manga.Tag
    (
        tag_id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT df_tag_id DEFAULT NEWID(),
        tag_name NVARCHAR(100) NOT NULL,
        description NVARCHAR(500) NULL,
        CONSTRAINT pk_tag PRIMARY KEY (tag_id),
        CONSTRAINT uq_tag_name UNIQUE (tag_name)
    );
END

IF OBJECT_ID('manga.SeriesTag', 'U') IS NULL
BEGIN
    CREATE TABLE manga.SeriesTag
    (
        series_id UNIQUEIDENTIFIER NOT NULL,
        tag_id UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT pk_series_tag PRIMARY KEY (series_id, tag_id)
    );
END
