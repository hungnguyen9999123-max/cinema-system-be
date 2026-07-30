/*
    Demo seed data for local development
    Date: 2026-07-15

    What this script adds:
    - Extra schema safety for FNB_ITEMS.image_public_id
    - Demo movies with poster/banner URLs
    - Demo cinema, rooms, seat types, seats
    - Active audience types and pricing rules
    - Future showtimes for booking/payment testing
    - F&B items and promotions

    This script is safe to run multiple times. It uses fixed demo IDs/names and
    updates demo records instead of creating duplicates.

    sqlcmd example:
    sqlcmd -S "(local)" -d cinema_db -U sa -P 12345 -C -i scripts\Database\20260715_demo_seed_data.sql
*/

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF COL_LENGTH('dbo.FNB_ITEMS', 'image_public_id') IS NULL
BEGIN
    ALTER TABLE dbo.FNB_ITEMS ADD image_public_id nvarchar(255) NULL;
END
GO

DECLARE @Now datetime2 = SYSUTCDATETIME();
DECLARE @Today date = CAST(GETDATE() AS date);
DECLARE @CreatedBy uniqueidentifier;

SELECT TOP (1) @CreatedBy = id
FROM dbo.USERS
WHERE UPPER(status) = 'ACTIVE'
ORDER BY
    CASE
        WHEN UPPER(role) IN ('ADMIN', 'MANAGER', 'STAFF') THEN 0
        ELSE 1
    END,
    created_at;

IF @CreatedBy IS NULL
BEGIN
    THROW 51000, 'Seed failed: no active user found for created_by.', 1;
END

DECLARE @CinemaId uniqueidentifier = 'A1000000-0000-0000-0000-000000000001';
DECLARE @RoomStandardId uniqueidentifier = 'A1000000-0000-0000-0000-000000000101';
DECLARE @RoomVipId uniqueidentifier = 'A1000000-0000-0000-0000-000000000102';
DECLARE @SeatStandardId uniqueidentifier = COALESCE(
    (SELECT TOP (1) id FROM dbo.SEAT_TYPES WHERE UPPER(name) = 'STANDARD'),
    CONVERT(uniqueidentifier, 'A1000000-0000-0000-0000-000000000201')
);
DECLARE @SeatVipId uniqueidentifier = COALESCE(
    (SELECT TOP (1) id FROM dbo.SEAT_TYPES WHERE UPPER(name) = 'VIP'),
    CONVERT(uniqueidentifier, 'A1000000-0000-0000-0000-000000000202')
);
DECLARE @AudienceAdultId uniqueidentifier = COALESCE(
    (SELECT TOP (1) id FROM dbo.AUDIENCE_TYPES WHERE UPPER(code) = 'ADULT'),
    CONVERT(uniqueidentifier, 'A1000000-0000-0000-0000-000000000301')
);
DECLARE @AudienceStudentId uniqueidentifier = COALESCE(
    (SELECT TOP (1) id FROM dbo.AUDIENCE_TYPES WHERE UPPER(code) = 'STUDENT'),
    CONVERT(uniqueidentifier, 'A1000000-0000-0000-0000-000000000302')
);
DECLARE @AudienceChildId uniqueidentifier = COALESCE(
    (SELECT TOP (1) id FROM dbo.AUDIENCE_TYPES WHERE UPPER(code) = 'CHILD'),
    CONVERT(uniqueidentifier, 'A1000000-0000-0000-0000-000000000303')
);
DECLARE @Movie1Id uniqueidentifier = 'A1000000-0000-0000-0000-000000000401';
DECLARE @Movie2Id uniqueidentifier = 'A1000000-0000-0000-0000-000000000402';
DECLARE @Movie3Id uniqueidentifier = 'A1000000-0000-0000-0000-000000000403';

IF NOT EXISTS (SELECT 1 FROM dbo.SEAT_TYPES WHERE id = @SeatStandardId)
BEGIN
    INSERT INTO dbo.SEAT_TYPES (id, name, seat_multiplier, description, status)
    VALUES (@SeatStandardId, N'Standard', 1.00, N'Ghế tiêu chuẩn', N'ACTIVE');
END
ELSE
BEGIN
    UPDATE dbo.SEAT_TYPES
    SET name = N'Standard',
        seat_multiplier = 1.00,
        description = N'Ghế tiêu chuẩn',
        status = N'ACTIVE'
    WHERE id = @SeatStandardId;
END

IF NOT EXISTS (SELECT 1 FROM dbo.SEAT_TYPES WHERE id = @SeatVipId)
BEGIN
    INSERT INTO dbo.SEAT_TYPES (id, name, seat_multiplier, description, status)
    VALUES (@SeatVipId, N'VIP', 1.35, N'Ghế VIP rộng và êm hơn', N'ACTIVE');
END
ELSE
BEGIN
    UPDATE dbo.SEAT_TYPES
    SET name = N'VIP',
        seat_multiplier = 1.35,
        description = N'Ghế VIP rộng và êm hơn',
        status = N'ACTIVE'
    WHERE id = @SeatVipId;
END

IF NOT EXISTS (SELECT 1 FROM dbo.AUDIENCE_TYPES WHERE id = @AudienceAdultId)
BEGIN
    INSERT INTO dbo.AUDIENCE_TYPES (id, code, display_name, audience_multiplier, description, is_active)
    VALUES (@AudienceAdultId, N'ADULT', N'Người lớn', 1.00, N'Vé người lớn', 1);
END
ELSE
BEGIN
    UPDATE dbo.AUDIENCE_TYPES
    SET code = N'ADULT',
        display_name = N'Người lớn',
        audience_multiplier = 1.00,
        description = N'Vé người lớn',
        is_active = 1
    WHERE id = @AudienceAdultId;
END

IF NOT EXISTS (SELECT 1 FROM dbo.AUDIENCE_TYPES WHERE id = @AudienceStudentId)
BEGIN
    INSERT INTO dbo.AUDIENCE_TYPES (id, code, display_name, audience_multiplier, description, is_active)
    VALUES (@AudienceStudentId, N'STUDENT', N'Học sinh sinh viên', 0.85, N'Vé học sinh sinh viên', 1);
END
ELSE
BEGIN
    UPDATE dbo.AUDIENCE_TYPES
    SET code = N'STUDENT',
        display_name = N'Học sinh sinh viên',
        audience_multiplier = 0.85,
        description = N'Vé học sinh sinh viên',
        is_active = 1
    WHERE id = @AudienceStudentId;
END

IF NOT EXISTS (SELECT 1 FROM dbo.AUDIENCE_TYPES WHERE id = @AudienceChildId)
BEGIN
    INSERT INTO dbo.AUDIENCE_TYPES (id, code, display_name, audience_multiplier, description, is_active)
    VALUES (@AudienceChildId, N'CHILD', N'Trẻ em', 0.70, N'Vé trẻ em', 1);
END
ELSE
BEGIN
    UPDATE dbo.AUDIENCE_TYPES
    SET code = N'CHILD',
        display_name = N'Trẻ em',
        audience_multiplier = 0.70,
        description = N'Vé trẻ em',
        is_active = 1
    WHERE id = @AudienceChildId;
END

IF NOT EXISTS (SELECT 1 FROM dbo.CINEMAS WHERE id = @CinemaId)
BEGIN
    INSERT INTO dbo.CINEMAS (id, name, address, city, phone, status, created_at, updated_at)
    VALUES (
        @CinemaId,
        N'Demo Cinema Center',
        N'123 Nguyen Hue, Quan 1',
        N'Ho Chi Minh',
        N'0909000001',
        N'ACTIVE',
        @Now,
        @Now
    );
END
ELSE
BEGIN
    UPDATE dbo.CINEMAS
    SET name = N'Demo Cinema Center',
        address = N'123 Nguyen Hue, Quan 1',
        city = N'Ho Chi Minh',
        phone = N'0909000001',
        status = N'ACTIVE',
        updated_at = @Now
    WHERE id = @CinemaId;
END

IF NOT EXISTS (SELECT 1 FROM dbo.ROOMS WHERE id = @RoomStandardId)
BEGIN
    INSERT INTO dbo.ROOMS (id, cinema_id, name, room_type, total_capacity, status, created_at)
    VALUES (@RoomStandardId, @CinemaId, N'Demo Room A - Standard', N'STANDARD', 40, N'ACTIVE', @Now);
END
ELSE
BEGIN
    UPDATE dbo.ROOMS
    SET cinema_id = @CinemaId,
        name = N'Demo Room A - Standard',
        room_type = N'STANDARD',
        total_capacity = 40,
        status = N'ACTIVE'
    WHERE id = @RoomStandardId;
END

IF NOT EXISTS (SELECT 1 FROM dbo.ROOMS WHERE id = @RoomVipId)
BEGIN
    INSERT INTO dbo.ROOMS (id, cinema_id, name, room_type, total_capacity, status, created_at)
    VALUES (@RoomVipId, @CinemaId, N'Demo Room B - VIP', N'VIP', 24, N'ACTIVE', @Now);
END
ELSE
BEGIN
    UPDATE dbo.ROOMS
    SET cinema_id = @CinemaId,
        name = N'Demo Room B - VIP',
        room_type = N'VIP',
        total_capacity = 24,
        status = N'ACTIVE'
    WHERE id = @RoomVipId;
END

DECLARE @Rows table (row_letter nchar(1));
INSERT INTO @Rows (row_letter) VALUES (N'A'), (N'B'), (N'C'), (N'D'), (N'E');

DECLARE @Cols table (col_number tinyint);
INSERT INTO @Cols (col_number) VALUES (1), (2), (3), (4), (5), (6), (7), (8);

INSERT INTO dbo.SEATS (id, room_id, seat_type_id, seat_label, row_letter, col_number, status)
SELECT
    NEWID(),
    @RoomStandardId,
    CASE WHEN r.row_letter IN (N'D', N'E') THEN @SeatVipId ELSE @SeatStandardId END,
    CONCAT(RTRIM(r.row_letter), c.col_number),
    r.row_letter,
    c.col_number,
    N'ACTIVE'
FROM @Rows AS r
CROSS JOIN @Cols AS c
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.SEATS AS s
    WHERE s.room_id = @RoomStandardId
      AND s.seat_label = CONCAT(RTRIM(r.row_letter), c.col_number)
);

DELETE FROM @Rows;
DELETE FROM @Cols;
INSERT INTO @Rows (row_letter) VALUES (N'A'), (N'B'), (N'C'), (N'D');
INSERT INTO @Cols (col_number) VALUES (1), (2), (3), (4), (5), (6);

INSERT INTO dbo.SEATS (id, room_id, seat_type_id, seat_label, row_letter, col_number, status)
SELECT
    NEWID(),
    @RoomVipId,
    @SeatVipId,
    CONCAT(RTRIM(r.row_letter), c.col_number),
    r.row_letter,
    c.col_number,
    N'ACTIVE'
FROM @Rows AS r
CROSS JOIN @Cols AS c
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.SEATS AS s
    WHERE s.room_id = @RoomVipId
      AND s.seat_label = CONCAT(RTRIM(r.row_letter), c.col_number)
);

DECLARE @Movies table (
    id uniqueidentifier,
    title nvarchar(255),
    genre nvarchar(100),
    language nvarchar(50),
    duration_min int,
    release_date date,
    synopsis nvarchar(max),
    age_rating nvarchar(10),
    poster_url nvarchar(500),
    banner_url nvarchar(500),
    trailer_url nvarchar(500)
);

INSERT INTO @Movies VALUES
(
    @Movie1Id,
    N'Saigon Midnight Chase',
    N'Action',
    N'Vietnamese',
    118,
    DATEADD(day, -14, @Today),
    N'Mot nhom ban tre bi cuon vao cuoc truy duoi gay can giua trung tam Sai Gon ve dem.',
    N'T13',
    N'https://picsum.photos/seed/saigon-midnight-poster/420/630',
    N'https://picsum.photos/seed/saigon-midnight-banner/1280/520',
    N'https://www.youtube.com/watch?v=dQw4w9WgXcQ'
),
(
    @Movie2Id,
    N'Orbit 2099',
    N'Sci-Fi',
    N'English',
    132,
    DATEADD(day, -7, @Today),
    N'Phi hanh doan cuoi cung cua Trai Dat phai dua ra lua chon kho khan de cuu mot tram khong gian sap roi quy dao.',
    N'T16',
    N'https://picsum.photos/seed/orbit-2099-poster/420/630',
    N'https://picsum.photos/seed/orbit-2099-banner/1280/520',
    N'https://www.youtube.com/watch?v=dQw4w9WgXcQ'
),
(
    @Movie3Id,
    N'The Little Lantern',
    N'Family',
    N'Vietnamese',
    96,
    DATEADD(day, -3, @Today),
    N'Cau chuyen am ap ve mot em be va chiec den long co kha nang dan loi nhung dieu uoc that long.',
    N'P',
    N'https://picsum.photos/seed/little-lantern-poster/420/630',
    N'https://picsum.photos/seed/little-lantern-banner/1280/520',
    N'https://www.youtube.com/watch?v=dQw4w9WgXcQ'
);

MERGE dbo.MOVIES AS target
USING @Movies AS source
ON target.id = source.id
WHEN MATCHED THEN
    UPDATE SET
        created_by = @CreatedBy,
        title = source.title,
        genre = source.genre,
        language = source.language,
        duration_min = source.duration_min,
        release_date = source.release_date,
        synopsis = source.synopsis,
        age_rating = source.age_rating,
        poster_url = source.poster_url,
        banner_url = source.banner_url,
        trailer_url = source.trailer_url,
        status = N'NOW_SHOWING',
        updated_at = @Now
WHEN NOT MATCHED THEN
    INSERT (
        id, created_by, title, genre, language, duration_min, release_date,
        synopsis, age_rating, poster_url, trailer_url, status, created_at,
        updated_at, banner_url
    )
    VALUES (
        source.id, @CreatedBy, source.title, source.genre, source.language,
        source.duration_min, source.release_date, source.synopsis,
        source.age_rating, source.poster_url, source.trailer_url,
        N'NOW_SHOWING', @Now, @Now, source.banner_url
    );

DECLARE @Pricing table (
    id uniqueidentifier,
    room_type_id int,
    time_slot_id int,
    base_price decimal(12, 2),
    time_multiplier decimal(5, 2)
);

INSERT INTO @Pricing VALUES
('A1000000-0000-0000-0000-000000000501', 1, 1, 75000, 1.00),
('A1000000-0000-0000-0000-000000000502', 1, 2, 75000, 1.20),
('A1000000-0000-0000-0000-000000000503', 2, 1, 95000, 1.00),
('A1000000-0000-0000-0000-000000000504', 2, 2, 95000, 1.20);

MERGE dbo.PRICING_RULES AS target
USING @Pricing AS source
ON target.id = source.id
WHEN MATCHED THEN
    UPDATE SET
        cinema_id = @CinemaId,
        room_type_id = source.room_type_id,
        time_slot_id = source.time_slot_id,
        base_price = source.base_price,
        time_multiplier = source.time_multiplier,
        effective_from = DATEADD(day, -30, @Today),
        effective_to = DATEADD(year, 3, @Today),
        is_active = 1
WHEN NOT MATCHED THEN
    INSERT (
        id, cinema_id, room_type_id, time_slot_id, base_price,
        time_multiplier, effective_from, effective_to, is_active, created_at
    )
    VALUES (
        source.id, @CinemaId, source.room_type_id, source.time_slot_id,
        source.base_price, source.time_multiplier, DATEADD(day, -30, @Today),
        DATEADD(year, 3, @Today), 1, @Now
    );

DECLARE @Showtimes table (
    id uniqueidentifier,
    movie_id uniqueidentifier,
    room_id uniqueidentifier,
    start_time datetime2,
    time_slot nvarchar(20),
    language_type nvarchar(20)
);

INSERT INTO @Showtimes VALUES
('A1000000-0000-0000-0000-000000000601', @Movie1Id, @RoomStandardId, DATEADD(hour, 10, CAST(DATEADD(day, 1, @Today) AS datetime2)), N'MORNING', N'SUBTITLED'),
('A1000000-0000-0000-0000-000000000602', @Movie2Id, @RoomStandardId, DATEADD(hour, 14, CAST(DATEADD(day, 1, @Today) AS datetime2)), N'AFTERNOON', N'SUBTITLED'),
('A1000000-0000-0000-0000-000000000603', @Movie3Id, @RoomVipId, DATEADD(hour, 19, CAST(DATEADD(day, 1, @Today) AS datetime2)), N'PEAK', N'DUBBED'),
('A1000000-0000-0000-0000-000000000604', @Movie1Id, @RoomVipId, DATEADD(hour, 21, CAST(DATEADD(day, 2, @Today) AS datetime2)), N'PEAK', N'SUBTITLED'),
('A1000000-0000-0000-0000-000000000605', @Movie2Id, @RoomStandardId, DATEADD(hour, 18, CAST(DATEADD(day, 3, @Today) AS datetime2)), N'EVENING', N'SUBTITLED'),
('A1000000-0000-0000-0000-000000000606', @Movie3Id, @RoomStandardId, DATEADD(hour, 9, CAST(DATEADD(day, 4, @Today) AS datetime2)), N'MORNING', N'DUBBED');

MERGE dbo.SHOWTIMES AS target
USING (
    SELECT
        s.id,
        s.movie_id,
        s.room_id,
        r.cinema_id,
        @CreatedBy AS created_by,
        s.start_time,
        DATEADD(minute, m.duration_min, s.start_time) AS end_time,
        s.time_slot,
        s.language_type
    FROM @Showtimes AS s
    INNER JOIN dbo.MOVIES AS m ON m.id = s.movie_id
    INNER JOIN dbo.ROOMS AS r ON r.id = s.room_id
) AS source
ON target.id = source.id
WHEN MATCHED THEN
    UPDATE SET
        movie_id = source.movie_id,
        room_id = source.room_id,
        cinema_id = source.cinema_id,
        created_by = source.created_by,
        start_time = source.start_time,
        end_time = source.end_time,
        time_slot = source.time_slot,
        language_type = source.language_type,
        status = N'SCHEDULED'
WHEN NOT MATCHED THEN
    INSERT (
        id, movie_id, room_id, cinema_id, created_by, start_time, end_time,
        time_slot, language_type, status, created_at
    )
    VALUES (
        source.id, source.movie_id, source.room_id, source.cinema_id,
        source.created_by, source.start_time, source.end_time,
        source.time_slot, source.language_type, N'SCHEDULED', @Now
    );

DECLARE @Fnb table (
    id uniqueidentifier,
    name nvarchar(100),
    category nvarchar(50),
    description nvarchar(max),
    price decimal(10, 2),
    image_url nvarchar(500)
);

INSERT INTO @Fnb VALUES
('A1000000-0000-0000-0000-000000000701', N'Combo Classic', N'COMBO', N'Bap rang bo lon va 2 nuoc ngot.', 89000, N'https://picsum.photos/seed/combo-classic/500/360'),
('A1000000-0000-0000-0000-000000000702', N'Combo Couple', N'COMBO', N'Bap caramel, 2 nuoc ngot va snack nho.', 129000, N'https://picsum.photos/seed/combo-couple/500/360'),
('A1000000-0000-0000-0000-000000000703', N'Bap rang bo', N'FOOD', N'Bap rang bo thom nong.', 59000, N'https://picsum.photos/seed/butter-popcorn/500/360'),
('A1000000-0000-0000-0000-000000000704', N'Bap caramel', N'FOOD', N'Bap rang caramel gion ngot.', 69000, N'https://picsum.photos/seed/caramel-popcorn/500/360'),
('A1000000-0000-0000-0000-000000000705', N'Nachos pho mai', N'FOOD', N'Nachos kem sot pho mai.', 75000, N'https://picsum.photos/seed/nachos-cheese/500/360'),
('A1000000-0000-0000-0000-000000000706', N'Coca-Cola', N'DRINK', N'Nuoc ngot lon.', 35000, N'https://picsum.photos/seed/coca-cola/500/360'),
('A1000000-0000-0000-0000-000000000707', N'Tra dao cam sa', N'DRINK', N'Tra dao mat lanh.', 45000, N'https://picsum.photos/seed/peach-tea/500/360'),
('A1000000-0000-0000-0000-000000000708', N'Nuoc suoi', N'DRINK', N'Nuoc suoi dong chai.', 25000, N'https://picsum.photos/seed/mineral-water/500/360');

MERGE dbo.FNB_ITEMS AS target
USING @Fnb AS source
ON target.id = source.id
WHEN MATCHED THEN
    UPDATE SET
        created_by = @CreatedBy,
        name = source.name,
        category = source.category,
        description = source.description,
        price = source.price,
        image_url = source.image_url,
        image_public_id = NULL,
        status = N'ACTIVE',
        updated_at = @Now
WHEN NOT MATCHED THEN
    INSERT (
        id, created_by, name, category, description, price, image_url,
        image_public_id, status, created_at, updated_at
    )
    VALUES (
        source.id, @CreatedBy, source.name, source.category, source.description,
        source.price, source.image_url, NULL, N'ACTIVE', @Now, @Now
    );

DECLARE @Promotions table (
    id uniqueidentifier,
    promo_code nvarchar(20),
    name nvarchar(150),
    discount_type nvarchar(20),
    discount_value decimal(10, 2),
    min_order_amt decimal(12, 2),
    usage_limit int
);

INSERT INTO @Promotions VALUES
('A1000000-0000-0000-0000-000000000801', N'DEMO10', N'Giam 10% ve demo', N'PERCENTAGE', 10, 50000, 500),
('A1000000-0000-0000-0000-000000000802', N'FAMILY20', N'Giam 20% cho phim gia dinh', N'PERCENTAGE', 20, 150000, 300),
('A1000000-0000-0000-0000-000000000803', N'VNPAY50K', N'Giam 50K khi thanh toan VNPAY', N'FIXED_AMOUNT', 50000, 120000, 400);

MERGE dbo.PROMOTIONS AS target
USING @Promotions AS source
ON target.id = source.id
WHEN MATCHED THEN
    UPDATE SET
        created_by = @CreatedBy,
        promo_code = source.promo_code,
        name = source.name,
        discount_type = source.discount_type,
        discount_value = source.discount_value,
        min_order_amt = source.min_order_amt,
        valid_from = DATEADD(day, -1, @Today),
        valid_to = DATEADD(day, 60, @Today),
        usage_limit = source.usage_limit,
        is_active = 1
WHEN NOT MATCHED THEN
    INSERT (
        id, created_by, promo_code, name, discount_type, discount_value,
        min_order_amt, valid_from, valid_to, usage_limit, is_active, created_at
    )
    VALUES (
        source.id, @CreatedBy, source.promo_code, source.name,
        source.discount_type, source.discount_value, source.min_order_amt,
        DATEADD(day, -1, @Today), DATEADD(day, 60, @Today),
        source.usage_limit, 1, @Now
    );

PRINT 'Demo seed summary';
SELECT 'MOVIES' AS table_name, COUNT(*) AS demo_count FROM dbo.MOVIES WHERE id IN (@Movie1Id, @Movie2Id, @Movie3Id)
UNION ALL SELECT 'CINEMAS', COUNT(*) FROM dbo.CINEMAS WHERE id = @CinemaId
UNION ALL SELECT 'ROOMS', COUNT(*) FROM dbo.ROOMS WHERE id IN (@RoomStandardId, @RoomVipId)
UNION ALL SELECT 'SEATS', COUNT(*) FROM dbo.SEATS WHERE room_id IN (@RoomStandardId, @RoomVipId)
UNION ALL SELECT 'SHOWTIMES', COUNT(*) FROM dbo.SHOWTIMES WHERE id IN (
    'A1000000-0000-0000-0000-000000000601',
    'A1000000-0000-0000-0000-000000000602',
    'A1000000-0000-0000-0000-000000000603',
    'A1000000-0000-0000-0000-000000000604',
    'A1000000-0000-0000-0000-000000000605',
    'A1000000-0000-0000-0000-000000000606'
)
UNION ALL SELECT 'FNB_ITEMS', COUNT(*) FROM dbo.FNB_ITEMS WHERE id BETWEEN 'A1000000-0000-0000-0000-000000000701' AND 'A1000000-0000-0000-0000-000000000708'
UNION ALL SELECT 'PROMOTIONS', COUNT(*) FROM dbo.PROMOTIONS WHERE id BETWEEN 'A1000000-0000-0000-0000-000000000801' AND 'A1000000-0000-0000-0000-000000000803';
GO
