/* Run once against an existing database before deploying the updated application. */
BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.Class', 'class_capacity') IS NULL
    BEGIN
        ALTER TABLE dbo.[Class] ADD class_capacity int NULL;

        -- Existing classes inherit room capacity; classes without a room start at 20.
        UPDATE c
        SET class_capacity = COALESCE(r.max_capacity, 20)
        FROM dbo.[Class] AS c
        LEFT JOIN dbo.[Room] AS r ON r.room_id = c.room_id;

        ALTER TABLE dbo.[Class] ALTER COLUMN class_capacity int NOT NULL;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints
        WHERE name = 'DF_Classes_ClassCapacity'
          AND parent_object_id = OBJECT_ID('dbo.Class'))
    BEGIN
        ALTER TABLE dbo.[Class]
            ADD CONSTRAINT DF_Classes_ClassCapacity DEFAULT (20) FOR class_capacity;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = 'CK_Classes_Capacity'
          AND parent_object_id = OBJECT_ID('dbo.Class'))
    BEGIN
        ALTER TABLE dbo.[Class]
            ADD CONSTRAINT CK_Classes_Capacity CHECK (class_capacity > 0);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
