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
| [AUTH-01](TASK-AUTH-01-user-entity-password-hasher.md) | Entity `User` (`app_user`) + `PasswordHasher` | MVP-02 | — | TODO |
| [AUTH-02](TASK-AUTH-02-register-login.md) | `register`, `login` (MFA token), lockout | AUTH-01 | — | TODO |
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
