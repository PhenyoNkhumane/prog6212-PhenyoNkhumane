# RaceDay

## Project Overview

RaceDay is a race and event management system designed to support the organisation and participation of sporting events.

The system manages users, sporting disciplines, clubs, events, race categories, registrations, club memberships and race results.

## Database Design

The RaceDay database includes these entities:

- User
- Participant
- Organiser
- Event
- Category
- Discipline
- Registration
- Result
- Club
- ClubMembership

The database uses primary keys and foreign keys to maintain relationships between entities and includes constraints to support data integrity.

## User Roles

RaceDay supports exactly two account roles:

- Participant
- Organiser

A User has exactly one account subtype: Participant or Organiser. The subtype tables have one-to-one relationships with User. Event records reference Organiser, while registrations and club memberships reference Participant, so the database rejects cross-role use. Public self-registration creates Participant accounts only. An authorised administrator provisions Organiser accounts, and the organiser must change the temporary password on first login.

## Main Relationships

- An Organiser can create multiple Events.
- An Event can contain multiple Categories.
- A Category belongs to one Discipline.
- A Participant can have multiple Registrations.
- A Registration belongs to one Category.
- A Registration can have one Result.
- A Participant can belong to multiple Clubs through ClubMembership.
- A Participant can belong to only one Club per Discipline.
- A Club can have multiple ClubMembership records.

## Project Documentation

The `docs` folder contains:

- `ERD.md` — current entity relationship diagram source
- `ERD.png` — existing diagram image (legacy; see `ERD.md` for the updated account subtype design)
- `RaceDay.sql` — SQL database creation script
- `API-Endpoint-Plan.md` — planned REST API endpoints

## Database Technology

- Microsoft SQL Server
- SQL Server Management Studio (SSMS)

## API

The planned API follows REST principles and includes endpoints for:

- Authentication
- User profiles
- Events
- Categories
- Registrations
- Results
- Disciplines
- Clubs
- Club memberships

## GitHub Actions CI/CD

The project uses GitHub Actions to validate the required repository structure.

![GitHub Actions Successful Build](docs/github-actions-success.png)
