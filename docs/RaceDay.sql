CREATE DATABASE RaceDay;

GO
USE RaceDay;

GO
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
  id INT IDENTITY(1, 1) PRIMARY KEY,
  email VARCHAR(255) NOT NULL UNIQUE,
  password_hash VARCHAR(255) NOT NULL,
  first_name VARCHAR(100) NOT NULL,
  last_name VARCHAR(100) NOT NULL,
  ROLE VARCHAR(20) NOT NULL,
  id_number_or_passport VARCHAR(50),
  asa_permanent_license_number VARCHAR(50),
  created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
  CONSTRAINT CK_User_Role CHECK (
    ROLE IN ('Organiser', 'Participant')
  )
);

GO
-- =============================================
-- TABLE: Club
-- =============================================
CREATE TABLE Club (
  id INT IDENTITY(1, 1) PRIMARY KEY,
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
  id INT IDENTITY(1, 1) PRIMARY KEY,
  organiser_id INT NOT NULL,
  title VARCHAR(200) NOT NULL,
  description VARCHAR(1000),
  DATE DATE NOT NULL,
  venue_name VARCHAR(200) NOT NULL,
  city VARCHAR(100) NOT NULL,
  province VARCHAR(100) NOT NULL,
  status VARCHAR(20) NOT NULL,
  CONSTRAINT FK_Event_Organiser FOREIGN KEY (organiser_id) REFERENCES [User] (id),
  CONSTRAINT CK_Event_Status CHECK (status IN ('Draft', 'Published', 'Cancelled'))
);

GO
-- =============================================
-- TABLE: Category
-- =============================================
CREATE TABLE Category (
  id INT IDENTITY(1, 1) PRIMARY KEY,
  event_id INT NOT NULL,
  discipline_id INT NOT NULL,
  name VARCHAR(150) NOT NULL,
  distance_km DECIMAL(6, 2),
  entry_fee_zar DECIMAL(10, 2) NOT NULL,
  max_slots INT NOT NULL,
  start_time TIME NOT NULL,
  CONSTRAINT FK_Category_Event FOREIGN KEY (event_id) REFERENCES Event (id),
  CONSTRAINT FK_Category_Discipline FOREIGN KEY (discipline_id) REFERENCES Discipline (id),
  CONSTRAINT CK_Category_Distance CHECK (distance_km > 0),
  CONSTRAINT CK_Category_EntryFee CHECK (entry_fee_zar >= 0),
  CONSTRAINT CK_Category_MaxSlots CHECK (max_slots > 0)
);

GO
-- =============================================
-- TABLE: Registration
-- =============================================
CREATE TABLE Registration (
  id INT IDENTITY(1, 1) PRIMARY KEY,
  participant_id INT NOT NULL,
  category_id INT NOT NULL,
  registration_date DATETIME2 NOT NULL DEFAULT GETDATE(),
  payment_status VARCHAR(20) NOT NULL,
  amount_paid DECIMAL(10, 2) NOT NULL,
  assigned_bib_number VARCHAR(20),
  requires_temp_asa_license BIT NOT NULL DEFAULT 0,
  medical_aid_name VARCHAR(150),
  medical_aid_number VARCHAR(50),
  status VARCHAR(20) NOT NULL DEFAULT 'Active',
  CONSTRAINT FK_Registration_Participant FOREIGN KEY (participant_id) REFERENCES [User] (id),
  CONSTRAINT FK_Registration_Category FOREIGN KEY (category_id) REFERENCES Category (id),
  CONSTRAINT CK_Registration_PaymentStatus CHECK (payment_status IN ('Pending', 'Paid', 'Refunded')),
  CONSTRAINT CK_Registration_AmountPaid CHECK (amount_paid >= 0),
  CONSTRAINT CK_Registration_Status CHECK (status IN ('Active', 'Cancelled')),
  CONSTRAINT UQ_Registration_Participant_Category UNIQUE (participant_id, category_id)
);

GO
-- =============================================
-- TABLE: ClubMembership
-- =============================================
CREATE TABLE ClubMembership (
  id INT IDENTITY(1, 1) PRIMARY KEY,
  participant_id INT NOT NULL,
  club_id INT NOT NULL,
  discipline_id INT NOT NULL,
  membership_number VARCHAR(100),
  joined_at DATE NOT NULL DEFAULT GETDATE(),
  status VARCHAR(20) NOT NULL DEFAULT 'Active',
  CONSTRAINT FK_ClubMembership_Participant FOREIGN KEY (participant_id) REFERENCES [User] (id),
  CONSTRAINT FK_ClubMembership_Club FOREIGN KEY (club_id) REFERENCES Club (id),
  CONSTRAINT FK_ClubMembership_Discipline FOREIGN KEY (discipline_id) REFERENCES Discipline (id),
  CONSTRAINT CK_ClubMembership_Status CHECK (status IN ('Active', 'Inactive')),
  CONSTRAINT UQ_ClubMembership_Participant_Discipline UNIQUE (participant_id, discipline_id)
);

GO
-- =============================================
-- TABLE: Result
-- =============================================

CREATE TABLE Result (
    id INT IDENTITY(1,1) PRIMARY KEY,
    registration_id INT NOT NULL,
    gun_time TIME,
    net_time TIME,
    overall_position INT,
    category_position INT,
    status VARCHAR(20) NOT NULL,

    CONSTRAINT FK_Result_Registration
        FOREIGN KEY (registration_id)
        REFERENCES Registration(id),

    CONSTRAINT CK_Result_Status
        CHECK (status IN ('Finished', 'DNF', 'DNS', 'DQ')),

    CONSTRAINT CK_Result_OverallPosition
        CHECK (overall_position IS NULL OR overall_position > 0),

    CONSTRAINT CK_Result_CategoryPosition
        CHECK (category_position IS NULL OR category_position > 0),

    CONSTRAINT UQ_Result_Registration
        UNIQUE (registration_id)
);
GO
