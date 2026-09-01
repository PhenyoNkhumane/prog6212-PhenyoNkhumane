CREATE DATABASE RaceDay;
Go
Use RaceDay;
Go
-- =============================================
-- TABLE: Discipline
-- =============================================

CREATE TABLE Discipline (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description VARCHAR(255)
);
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
-- =============================================
-- TABLE: Club
-- =============================================

CREATE TABLE Club (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(200) NOT NULL UNIQUE,
    registration_number VARCHAR(100) UNIQUE,
    contact_email VARCHAR(255),
    contact_number VARCHAR(30),
    created_at DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO