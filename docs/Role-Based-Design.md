# RaceDay Role-Based Design

## Account roles

RaceDay has two account roles: `Participant` and `Organiser`. Each account has exactly one role. `User` stores shared identity and login data, while the matching `Participant` or `Organiser` subtype row represents the account's role in the data model.

Public self-registration creates a `Participant` account only. The request cannot choose a role. An authorised administrator provisions an `Organiser` account through the controlled account-provisioning process and supplies a one-time credential or invitation. The organiser must set a new password at first login. Organisers cannot promote themselves or convert a participant account.

The administrator who provisions accounts is an administrative operator, not a third RaceDay account role. The two permitted application roles remain Participant and Organiser. Provisioning must be limited to authorised staff and must not expose temporary passwords in logs or public responses.

## Permissions

| Capability | Participant | Organiser |
|---|---|---|
| Register an account | Yes, self-registration | No, provisioned by authorised staff |
| View public events, categories, clubs and results | Yes | Yes |
| Enter a category and manage own registration | Yes | No |
| View own registrations, club memberships and results | Yes | No |
| Create and manage own events and categories | No | Yes |
| View registrations and record results for own events | No | Yes |
| Add disciplines or clubs | No | Yes |
| Manage another organiser's event | No | No |

Every protected request must authenticate the account and check both its role and, for event operations, ownership of the relevant event. Knowing an identifier does not grant access to another user's records.

## Database enforcement

`Event.organiser_id` references `Organiser.user_id`. `Registration.participant_id` and `ClubMembership.participant_id` reference `Participant.user_id`. These foreign keys prevent an account without the appropriate subtype from acting in that capacity. Subtype triggers validate that the subtype matches the role and reject a user appearing in both subtype tables. The API must also create the `User` row and its matching subtype in one transaction.
