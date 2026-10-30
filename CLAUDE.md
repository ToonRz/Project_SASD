# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Webboard App** (SASD course, problem C) — users create **Topics** and write **Comments**, like Reddit/Pantip.
Three layers, technology fixed by the assignment: Presentation (free choice, not yet chosen) → Domain
(ASP.NET Core REST API) → Data Access (Entity Framework Core) → PostgreSQL.

**Document map** (read the right one for the question):

| Question | Read |
| --- | --- |
| What must the system do / scope / FR IDs | `docs/SPECREQUIREMENT.md` |
| Layers, dependency rules, data model, API table | `docs/ARCHITECTURE.md` |
| Libraries and versions | `docs/TECHSTACK.md` |
| Domain terms (Topic, Comment, Reply, Tag, Room, Author, Member, MFA/Access token) | `CONTEXT.md` |
| Branching / commits / PR / secrets | `CONTRIBUTING.md` |
| Why the layers are split this way | `docs/architecture/decisions/` |

## Critical rules

1. Presentation talks to the system only through `/api/...` — never to PostgreSQL directly.
2. `Webboard.Api` references `Webboard.DataAccess`, never the reverse. Controllers call Services; only Services use `WebboardDbContext`.
3. Controllers return DTO `record`s, never EF entities.
4. Schema changes go through `dotnet ef migrations add` only. Never edit a merged migration.
5. A Reply is a `Comment` with `ParentCommentId` set — no separate `Reply` table or class.
6. Store times as UTC (`timestamptz`).
7. No secrets in the repo: connection strings with passwords live in `dotnet user-secrets` or env vars.
8. When a change alters an endpoint, entity, layer boundary or requirement, update the matching doc in `docs/` in the same change.
9. Passwords are stored only as PBKDF2-HMAC-SHA256 (`PBKDF2-SHA256$600000$salt$hash`) — never plaintext, never reversible encryption, never a single unsalted SHA-256. See `docs/architecture/decisions/ADR-auth-password-2fa.md`.
10. Login is two steps: password → MFA token (only valid on `/api/auth/2fa/*`) → TOTP code → access token. Writes use `[Authorize]` and take the Author from the token's `sub` claim, never from the request body.
11. Never log or return passwords, TOTP codes or shared keys. The TOTP shared key is stored encrypted with Data Protection.
12. Commits follow Conventional Commits with scopes `ui`, `api`, `data`, `db`, `auth`, `docs`, `ci` (see `CONTRIBUTING.md`).
