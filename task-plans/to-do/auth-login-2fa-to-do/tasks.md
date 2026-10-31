# auth-login-2fa — Login ด้วยรหัสผ่าน + 2FA (TOTP)

**Source spec:** [`task-plans/spec/spec-auth-login-2fa.md`](../../spec/spec-auth-login-2fa.md)
**Status of spec/board:** Approved 2026-10-31 (narawichswu)

## Goal

สมัคร, login สองขั้น และ access token สำหรับ endpoint ที่เขียนข้อมูล ตาม Acceptance ใน spec §4

## Global constraints

- รหัสผ่านเก็บเป็น PBKDF2-HMAC-SHA256 600,000 รอบเท่านั้น ([ADR](../../../docs/architecture/decisions/ADR-auth-password-2fa.md))
- ไม่มีเส้นทางใดได้ access token โดยไม่ผ่านรหัส TOTP
- `Jwt:SigningKey` และ Data Protection key ring อยู่นอก repo; test ไม่ใช้สองค่านี้จาก user-secrets — `WebboardApiFactory` ใส่ค่าสุ่ม/ephemeral ให้ ([spec-webboard-mvp](../../spec/spec-webboard-mvp.md) D12)
- Integration test ทุก class อยู่ใน `[Collection("Database")]` และเรียก `ResetDatabaseAsync()` ก่อนแต่ละ test (spec-webboard-mvp D11)
- ห้าม log รหัสผ่าน, รหัส TOTP, shared key หรือ token (NFR-006) — รวมถึงใน test output และ exception message
- Stage เฉพาะ path ที่ task ระบุ ห้าม `git add -A`

## Task table

| Task | Title | Depends on | Owner | Status |
| --- | --- | --- | --- | --- |
| [AUTH-01](TASK-AUTH-01-user-entity-password-hasher.md) | Entity `User` (`app_user`) + `PasswordHasher` | MVP-02 | narawichswu (Claude) | DONE |
| [AUTH-02](TASK-AUTH-02-register-login.md) | `register`, `login` (MFA token), lockout | AUTH-01 | narawichswu (Claude) | DONE |
| [AUTH-03](TASK-AUTH-03-totp-access-token.md) | 2FA setup/verify, access token, JWT bearer policies, `/me` | AUTH-02 | — | TODO |

## Dependency order

```text
MVP-02 ─► AUTH-01 ─► AUTH-02 ─► AUTH-03 ─► (MVP-04, MVP-06)
```

## Gates that outrank task status

- พบรหัสผ่าน plaintext, SHA-256 แบบไม่มี salt หรือจำนวนรอบ < 600,000 ที่ใดก็ตาม → reject
- MFA token เรียก endpoint อื่นนอกจาก `/api/auth/2fa/*` สำเร็จ → reject
- รหัส TOTP เดิมใช้ซ้ำใน step เดียวกันสำเร็จ → reject
- Username ไม่มีจริงกับรหัสผิดตอบต่างกัน (status, body หรือข้าม PBKDF2) → reject
- `Jwt:SigningKey` อยู่ในไฟล์ที่ commit → reject

## Receipts

_(เพิ่มเมื่อ task เสร็จ: คำสั่งจริง + output จริง)_

### AUTH-01

```text
$ dotnet test --filter "FullyQualifiedName~PasswordHasherTests|FullyQualifiedName~UserSchemaTests"
(red first: CS0234 Webboard.Api.Security / Webboard.DataAccess.Entities not found)
Passed!  - Failed:     0, Passed:    11, Skipped:     0, Total:    11

$ dotnet build -warnaserror        -> 0 Warning(s) 0 Error(s)
$ dotnet test                      -> Passed!  - Failed: 0, Passed: 15, Total: 15
$ dotnet format --verify-no-changes -> exit 0 (after `dotnet format` normalised the generated migration's EOL/BOM)
$ dotnet ef database update ...    -> Applying migration '20261031070039_AddAppUser'. Done.
$ docker compose exec -T db psql -U webboard -c '\d app_user'
 username | character varying(30) | not null ; display_name varchar(50) ; password_hash text ; ...
 "pk_app_user" PRIMARY KEY, btree (id)
 "ix_app_user_username" UNIQUE, btree (username)
$ grep -rn "SHA256.HashData\|ComputeHash" src/ --include=*.cs   -> (no output)
```

Deviation: added `Microsoft.EntityFrameworkCore.Design` (PrivateAssets=all) to `Webboard.Api.csproj` because `dotnet ef --startup-project src/Webboard.Api` requires it.


### AUTH-02

```text
$ dotnet test --filter FullyQualifiedName~AuthRegisterLoginTests
(red first: Failed: 10, Passed: 1, Total: 11 - endpoints did not exist)
Passed!  - Failed:     0, Passed:    11, Skipped:     0, Total:    11

$ dotnet build -warnaserror         -> 0 Warning(s) 0 Error(s)
$ dotnet test                       -> Passed!  - Failed: 0, Passed: 26, Total: 26
$ dotnet format --verify-no-changes -> exit 0
$ git grep -n "SigningKey" -- '*.json'  -> (no output)
```

Deviations: added package `Microsoft.IdentityModel.JsonWebTokens` (token creation + test decoding). `OpenApiTests` now supplies a throwaway `Jwt:SigningKey` because startup validation (ValidateOnStart) fails without one. The signing key is validated in every environment, not only outside `Testing`.
