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
-- =============================================
-- TABLE: Event
-- =============================================

CREATE TABLE Event (
    id INT IDENTITY(1,1) PRIMARY KEY,
    organiser_id INT NOT NULL,
    title VARCHAR(200) NOT NULL,
    description VARCHAR(1000),
    date DATE NOT NULL,
    venue_name VARCHAR(200) NOT NULL,
    city VARCHAR(100) NOT NULL,
    province VARCHAR(100) NOT NULL,
    status VARCHAR(20) NOT NULL,

    CONSTRAINT FK_Event_Organiser
        FOREIGN KEY (organiser_id)
        REFERENCES [User](id),

    CONSTRAINT CK_Event_Status
        CHECK (status IN ('Draft', 'Published', 'Cancelled'))
);
GO
-- =============================================
-- TABLE: Category
-- =============================================

CREATE TABLE Category (
    id INT IDENTITY(1,1) PRIMARY KEY,
    event_id INT NOT NULL,
    discipline_id INT NOT NULL,
    name VARCHAR(150) NOT NULL,
    distance_km DECIMAL(6,2),
    entry_fee_zar DECIMAL(10,2) NOT NULL,
    max_slots INT NOT NULL,
    start_time TIME NOT NULL,

    CONSTRAINT FK_Category_Event
        FOREIGN KEY (event_id)
        REFERENCES Event(id),

    CONSTRAINT FK_Category_Discipline
        FOREIGN KEY (discipline_id)
        REFERENCES Discipline(id),

    CONSTRAINT CK_Category_Distance
        CHECK (distance_km > 0),

    CONSTRAINT CK_Category_EntryFee
        CHECK (entry_fee_zar >= 0),

    CONSTRAINT CK_Category_MaxSlots
        CHECK (max_slots > 0)
);
GO
