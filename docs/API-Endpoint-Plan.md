# API Endpoint Plan

**Project:** Race/Event Management System
**Part:** 1 — Endpoint Planning (based on approved ERD: User, Club, Event, Category, Discipline, Registration, Result, ClubMembership)

Roles referenced below come from the role field on User, which is restricted to Participant and Organiser using a database CHECK constraint. Each account has exactly one role. "Owner" means the Organiser whose organiser_id is associated with the related Event.

## 1. Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Register a new user as either a Participant or an Organiser | Public | `{ email, password, first_name, last_name, role, id_number_or_passport, asa_permanent_license_number? }` | `201 Created` — `{ user: {...}, token }` |
| POST | `/api/auth/login` | Authenticate an existing user and issue a JWT | Public | `{ email, password }` | `200 OK` — `{ user: {...}, token }` |

## 2. User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/users/me` | View the logged-in user's own profile | Participant / Organiser | — | `200 OK` — user object |
| PUT | `/api/users/me` | Update the logged-in user's own profile | Participant / Organiser | `{ first_name?, last_name?, email?, id_number_or_passport?, asa_permanent_license_number? }` | `200 OK` — updated user object |

## 3. Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events` | List all events, with optional filters such as status, city or date | Public | — | `200 OK` — array of events |
| GET | `/api/events/{eventId}` | Get details of a single event | Public | — | `200 OK` — event object |
| POST | `/api/events` | Create a new event | Organiser | `{ title, description, date, venue_name, city, province, status }` | `201 Created` — event object |
| PUT | `/api/events/{eventId}` | Update an existing event | Organiser (owner) | `{ title?, description?, date?, venue_name?, city?, province?, status? }` | `200 OK` — updated event object |
| DELETE | `/api/events/{eventId}` | Delete an event | Organiser (owner) | — | `204 No Content` |

## 4. Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | List all categories for a given event | Public | — | `200 OK` — array of categories |
| GET | `/api/categories/{categoryId}` | Get details of a single category | Public | — | `200 OK` — category object |
| POST | `/api/events/{eventId}/categories` | Create a new category for an event, specifying its discipline, distance, entry fee, capacity and start time | Organiser (owner) | `{ discipline_id, name, distance_km, entry_fee_zar, max_slots, start_time }` | `201 Created` — category object |
| PUT | `/api/categories/{categoryId}` | Update a category | Organiser (owner) | `{ name?, distance_km?, entry_fee_zar?, max_slots?, start_time? }` | `200 OK` — updated category object |
| DELETE | `/api/categories/{categoryId}` | Delete a category | Organiser (owner) | — | `204 No Content` |

## 5. Event Enrolments (Registrations)

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/events/{eventId}/registrations` | Enter (enrol in) an event by selecting a category | Participant | `{ category_id, requires_temp_asa_license?, medical_aid_name?, medical_aid_number? }` | `201 Created` — registration object |
| GET | `/api/registrations/me` | View the logged-in Participant's own enrolments | Participant | — | `200 OK` — array of registrations |
| GET | `/api/events/{eventId}/registrations` | View all enrolments for an organiser's event | Organiser (owner) | — | `200 OK` — array of registrations |
| GET | `/api/registrations/{registrationId}` | View a single registration's detail | Participant (owner) / Organiser (owner event) | — | `200 OK` — registration object |
| PUT | `/api/registrations/{registrationId}` | Update a registration | Organiser (owner); Participant (owner) limited to their own medical-aid details | Organiser: `{ payment_status?, amount_paid?, assigned_bib_number?, status? }` / Participant: `{ medical_aid_name?, medical_aid_number? }` | `200 OK` — updated registration object |
| DELETE | `/api/registrations/{registrationId}` | Cancel a registration or withdraw from an event | Participant (owner) / Organiser (owner event) | — | `204 No Content` |

## 6. Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/events/{eventId}/results` | Record the official race result for a participant's registration, including times, positions and race status | Organiser (owner) | `{ registration_id, gun_time, net_time, overall_position, category_position, status }` | `201 Created` — result object |
| PUT | `/api/results/{resultId}` | Correct/update a captured result | Organiser (owner) | `{ gun_time?, net_time?, overall_position?, category_position?, status? }` | `200 OK` — updated result object |
| GET | `/api/events/{eventId}/results` | View all results for an event | Public | — | `200 OK` — array of results |
| GET | `/api/results/me` | View the logged-in Participant's own results | Participant | — | `200 OK` — array of results |
| GET | `/api/results/{resultId}` | View a single result | Participant (owner) / Organiser (owner event) | — | `200 OK` — result object |

## 7. Additional supporting endpoints (identified as necessary from the ERD)

These support entities referenced by the ERD (`Discipline`, `Club`, `ClubMembership`) that Categories and Registrations depend on, but weren't explicitly listed in the functional requirements.

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/disciplines` | List all disciplines (e.g. Run, Walk, Cycle) | Public | — | `200 OK` — array of disciplines |
| POST | `/api/disciplines` | Create a new discipline | Organiser | `{ name, description }` | `201 Created` — discipline object |
| GET | `/api/clubs` | List all clubs | Public | — | `200 OK` — array of clubs |
| POST | `/api/clubs` | Register a new club | Organiser | `{ name, registration_number, contact_email, contact_number }` | `201 Created` — club object |
| GET | `/api/clubs/{clubId}` | Get details of a single club | Public | — | `200 OK` — club object |
| POST | `/api/clubs/{clubId}/memberships` | Join a club under a discipline | Participant | `{ discipline_id, membership_number, joined_at }` | `201 Created` — membership object |
| GET | `/api/users/me/memberships` | View the logged-in Participant's own club memberships | Participant | — | `200 OK` — array of memberships |

## Notes on error responses

All endpoints additionally return, where applicable: `400 Bad Request` (validation failure), `401 Unauthorized` (missing/invalid token), `403 Forbidden` (wrong role or not the resource owner), and `404 Not Found` (resource does not exist).
