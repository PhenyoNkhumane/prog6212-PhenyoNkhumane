# RaceDay Role-Based Design

## Account roles

RaceDay has two account roles: `Participant` and `Organiser`. Each account has exactly one role. `User` stores shared identity and login data, while the matching `Participant` or `Organiser` subtype row represents the account's role in the data model.

Public self-registration allows the caller to choose either permitted role. The API validates the role against the fixed allow-list and atomically creates the `User` and matching subtype. The role cannot be changed through profile updates. Registration uses a signed JWT; the client sends it as a bearer token on protected requests.

Passwords must be at least 12 characters and include upper-case, lower-case, numeric, and non-alphanumeric characters. Login attempts are rate-limited. A JWT for an account marked `MustChangePassword` can only call the password-change or logout endpoint.

## Permissions

| Capability | Participant | Organiser |
|---|---|---|
| Register an account | Yes, self-registration | Yes, self-registration |
| View public events, categories, clubs and results | Yes | Yes |
| Enter a category and manage own registration | Yes | No |
| View own registrations, club memberships and results | Yes | No |
| Create and manage own events and categories | No | Yes |
| View registrations and record results for own events | No | Yes |
| Add disciplines or clubs | No | Yes |
| Manage another organiser's event | No | No |

Every protected request must validate the JWT and check its role and, for event operations, ownership of the relevant event. Knowing an identifier does not grant access to another user's records. Password changes do not mutate an issued token; the user signs in again to receive an updated token.

## Database enforcement

`Event.organiser_id` references `Organiser.user_id`. `Registration.participant_id` and `ClubMembership.participant_id` reference `Participant.user_id`. These foreign keys prevent an account without the appropriate subtype from acting in that capacity. Subtype triggers validate that the subtype matches the role and reject a user appearing in both subtype tables. SQL also guards role changes against an existing subtype mismatch.
