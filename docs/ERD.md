# RaceDay Entity Relationship Diagram

`User` stores shared login and identity details. Every account has one role and one matching subtype row. Public registration creates Participants only; Organisers are provisioned by an authorised administrator. Events reference Organiser, and Registrations and ClubMemberships reference Participant.

```mermaid
erDiagram
    USER ||--o| PARTICIPANT : "has subtype"
    USER ||--o| ORGANISER : "has subtype"
    ORGANISER ||--o{ EVENT : creates
    EVENT ||--o{ CATEGORY : contains
    DISCIPLINE ||--o{ CATEGORY : classifies
    PARTICIPANT ||--o{ REGISTRATION : enters
    CATEGORY ||--o{ REGISTRATION : receives
    REGISTRATION ||--o| RESULT : records
    PARTICIPANT ||--o{ CLUB_MEMBERSHIP : holds
    CLUB ||--o{ CLUB_MEMBERSHIP : has
    DISCIPLINE ||--o{ CLUB_MEMBERSHIP : classifies

    USER {
        int id PK
        string email UK
        string password_hash
        string first_name
        string last_name
        string role
        boolean must_change_password
    }
    PARTICIPANT {
        int user_id PK,FK
    }
    ORGANISER {
        int user_id PK,FK
        string provisioned_by
        datetime provisioned_at
    }
    EVENT {
        int id PK
        int organiser_id FK
        string title
        date date
    }
    CATEGORY {
        int id PK
        int event_id FK
        int discipline_id FK
        string name
    }
    DISCIPLINE {
        int id PK
        string name UK
    }
    REGISTRATION {
        int id PK
        int participant_id FK
        int category_id FK
    }
    RESULT {
        int id PK
        int registration_id FK,UK
    }
    CLUB {
        int id PK
        string name UK
    }
    CLUB_MEMBERSHIP {
        int id PK
        int participant_id FK
        int club_id FK
        int discipline_id FK
    }
```

The database foreign keys bind event ownership to `Organiser` and participant activity to `Participant`. Constraints and subtype checks prevent a single account from occupying both roles.
