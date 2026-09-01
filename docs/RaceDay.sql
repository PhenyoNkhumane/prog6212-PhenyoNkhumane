CREATE DATABASE RaceDay;

GO
USE RaceDay;

GO
-- =============================================
-- TABLE: User
-- =============================================

CREATE TABLE [User] (
    id INT IDENTITY(1,1) PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    role VARCHAR(20) NOT NULL,
    id_number_or_passport VARCHAR(50),
    asa_permanent_license_number VARCHAR(50),
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT CK_User_Role
        CHECK (role IN ('Organiser', 'Participant'))
);
GO