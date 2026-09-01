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
-- =============================================
-- SAMPLE DATA
-- =============================================

-- DISCIPLINES
INSERT INTO Discipline (name, description)
VALUES
('Running', 'Road running and marathon events'),
('Walking', 'Competitive and recreational walking events'),
('Cycling', 'Road cycling events');
GO

-- USERS
-- 2 Organisers
INSERT INTO [User]
    (email, password_hash, first_name, last_name, role,
     id_number_or_passport, asa_permanent_license_number)
VALUES
('organiser1@raceday.co.za', 'HASH_ORGANISER_001',
 'Thabo', 'Mokoena', 'Organiser',
 '8001015009087', NULL),

('organiser2@raceday.co.za', 'HASH_ORGANISER_002',
 'Lerato', 'Molefe', 'Organiser',
 '8205056007088', NULL);

-- 2 Participants
INSERT INTO [User]
    (email, password_hash, first_name, last_name, role,
     id_number_or_passport, asa_permanent_license_number)
VALUES
('participant1@raceday.co.za', 'HASH_PARTICIPANT_001',
 'Phenyo', 'Nkhumane', 'Participant',
 '9001015009089', 'ASA123456'),

('participant2@raceday.co.za', 'HASH_PARTICIPANT_002',
 'Naledi', 'Mokoena', 'Participant',
 '9202026007090', 'ASA654321');
GO

-- CLUBS
INSERT INTO Club
    (name, registration_number, contact_email, contact_number)
VALUES
('Johannesburg Running Club', 'JRC001',
 'info@jrc.co.za', '0115551001'),

('Gauteng Cycling Club', 'GCC001',
 'info@gcc.co.za', '0115551002');
GO

-- EVENTS
INSERT INTO Event
    (organiser_id, title, description, date,
     venue_name, city, province, status)
VALUES
(1,
 'Johannesburg Road Race',
 'Annual road running event.',
 '2026-10-10',
 'Zoo Lake Sports Grounds',
 'Johannesburg',
 'Gauteng',
 'Published'),

(1,
 'Soweto Community Run',
 'Community-focused running event.',
 '2026-11-15',
 'Orlando Stadium',
 'Soweto',
 'Gauteng',
 'Published'),

(2,
 'Gauteng Cycling Challenge',
 'Competitive road cycling event.',
 '2026-12-05',
 'Midrand Sports Centre',
 'Midrand',
 'Gauteng',
 'Draft');
GO

-- CATEGORIES
INSERT INTO Category
    (event_id, discipline_id, name, distance_km,
     entry_fee_zar, max_slots, start_time)
VALUES
(1, 1, '10km Road Race', 10.00, 150.00, 1000, '07:00'),
(1, 1, '21km Half Marathon', 21.00, 250.00, 800, '06:30'),
(1, 1, '42km Marathon', 42.00, 400.00, 500, '06:00'),

(2, 1, '5km Fun Run', 5.00, 80.00, 1500, '08:00'),
(2, 2, '10km Walk', 10.00, 120.00, 500, '07:30'),

(3, 3, '50km Cycling Race', 50.00, 300.00, 500, '06:30'),
(3, 3, '100km Cycling Race', 100.00, 500.00, 300, '06:00');
GO

-- CLUB MEMBERSHIPS
INSERT INTO ClubMembership
    (participant_id, club_id, discipline_id,
     membership_number, joined_at, status)
VALUES
(3, 1, 1, 'JRC-001', '2026-01-15', 'Active'),
(4, 1, 1, 'JRC-002', '2026-02-01', 'Active'),
(3, 2, 3, 'GCC-001', '2026-03-10', 'Active');
GO

-- REGISTRATIONS
INSERT INTO Registration
    (participant_id, category_id, registration_date,
     payment_status, amount_paid, assigned_bib_number,
     requires_temp_asa_license, medical_aid_name,
     medical_aid_number)
VALUES
(3, 1, '2026-08-20', 'Paid', 150.00, 'JR001',
 0, 'Discovery Health', 'DH100001'),

(4, 2, '2026-08-21', 'Paid', 250.00, 'JR002',
 0, 'Bonitas', 'BO100002'),

(3, 4, '2026-08-25', 'Pending', 80.00, NULL,
 1, 'Discovery Health', 'DH100001');
GO

-- RESULTS
INSERT INTO Result
    (registration_id, gun_time, net_time,
     overall_position, category_position, status)
VALUES
(1, '01:02:35', '01:01:50', 25, 8, 'Finished'),

(2, '02:15:20', '02:13:45', 47, 12, 'Finished');
GO
