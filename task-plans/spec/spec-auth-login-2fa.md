# SPEC: Login ด้วยรหัสผ่าน + 2FA (TOTP)

Status: Draft 2026-10-30 — รอทีม approve ก่อนเริ่ม task
Date: 2026-10-30
Board: [auth-login-2fa-to-do/tasks.md](../to-do/auth-login-2fa-to-do/tasks.md)
Requirements: FR-AUTH-001…008, NFR-004, NFR-006, NFR-007 ([SPECREQUIREMENT](../../docs/SPECREQUIREMENT.md) §4.6)
Design: [ARCHITECTURE.md §9](../../docs/ARCHITECTURE.md#9-authentication-login--2fa), [ADR-auth-password-2fa](../../docs/architecture/decisions/ADR-auth-password-2fa.md)
Instruction: requirement "login ใช้ 2FA และ password ต้อง encrypt เป็น sha256" (2026-10-30) — ตีความเป็น PBKDF2-HMAC-SHA256 ตาม ADR

## 1. เป้าหมาย

1. สมัครสมาชิกได้ และรหัสผ่านเก็บเป็น PBKDF2-HMAC-SHA256 เท่านั้น
2. Login สองขั้น: รหัสผ่าน → MFA token → รหัส TOTP → access token; 2FA บังคับทุกคน
3. Endpoint ที่เขียนข้อมูลรับเฉพาะ access token

ไม่ทำ: ลืม/เปลี่ยนรหัสผ่าน, recovery code, refresh token, social login, ยืนยันอีเมล

Anti-goal: มีทางใดก็ตามที่ได้ access token โดยไม่ผ่านรหัส TOTP

## 2. การตัดสินใจ

| # | เรื่อง | ตัดสิน | ที่มา |
| --- | --- | --- | --- |
| D1 | Password hash | `Rfc2898DeriveBytes.Pbkdf2(password, salt, 600_000, HashAlgorithmName.SHA256, 32)`, salt 16 bytes จาก `RandomNumberGenerator`, เทียบด้วย `CryptographicOperations.FixedTimeEquals` | ADR D1, OWASP Password Storage |
| D2 | รูปแบบที่เก็บ | `PBKDF2-SHA256$<iterations>$<salt b64>$<hash b64>` — อ่าน iterations จาก string ตอนตรวจ | ADR D1 |
| D3 | Username ไม่มีจริง | ยังตรวจกับ `PasswordHasher.DummyHash` เพื่อให้เวลาตอบเท่ากัน แล้วตอบ `401` ข้อความเดียวกับรหัสผิด | ADR D4 |
| D4 | TOTP | Otp.NET: SHA-1, 6 หลัก, 30 วินาที, `VerificationWindow(previous: 1, future: 1)`; ปฏิเสธ step ≤ `totp_last_step` | RFC 6238, ADR D2 |
| D5 | Shared key at rest | `IDataProtector` purpose `"Webboard.TotpSecret"`; key ring เก็บนอก repo | ADR D2 |
| D6 | Token | JWT HS256, claims `sub` (user id), `name` (display name), `purpose` (`mfa`/`access`); MFA 5 นาที, access 60 นาที; `ClockSkew = 30s` | ADR D3 |
| D7 | แยก token | Default authorization policy ต้องมี `purpose=access`; endpoint `/api/auth/2fa/*` ใช้ policy `MfaPending` ที่ต้องมี `purpose=mfa` | FR-AUTH-007 |
| D8 | Lockout | ผิดติดกัน 5 ครั้ง (รหัสผ่านหรือ TOTP นับรวม) → `lockout_until = now + 15 นาที`, รีเซ็ตตัวนับ; ระหว่างล็อกตอบ `429` + `Retry-After` | FR-AUTH-006 |
| D9 | Username | trim + `ToLowerInvariant()` ก่อนเก็บและก่อนค้น; unique index บน `username` | FR-AUTH-001 |
| D10 | ไม่ใช้ ASP.NET Core Identity | เขียน `PasswordHasher`, `TotpService`, `TokenService` เอง (3 class เล็ก) | ADR D1 Consequences |

## 3. Contracts

```csharp
public record RegisterRequest(
    [Required, RegularExpression("^[A-Za-z0-9_]{3,30}$")] string Username,
    [Required, MaxLength(50)] string DisplayName,
    [Required, StringLength(128, MinimumLength = 8)] string Password);
public record UserDto(int Id, string Username, string DisplayName, bool TwoFactorEnabled);

public record LoginRequest([Required] string Username, [Required] string Password);
public record LoginResponse(string MfaToken, bool TwoFactorSetupRequired);

public record TwoFactorSetupResponse(string SharedKey, string OtpAuthUri);
public record VerifyTwoFactorRequest([Required, RegularExpression("^[0-9]{6}$")] string Code);
public record AuthResponse(string AccessToken, int ExpiresIn, UserDto User);
```

| Endpoint | Token | สำเร็จ | ผิดพลาด |
| --- | --- | --- | --- |
| `POST /api/auth/register` | — | `201 UserDto` | `400`, `409` |
| `POST /api/auth/login` | — | `200 LoginResponse` | `400`, `401`, `429` |
| `POST /api/auth/2fa/setup` | MFA | `200 TwoFactorSetupResponse` | `401`, `409` (เปิดแล้ว) |
| `POST /api/auth/2fa/verify` | MFA | `200 AuthResponse` | `400`, `401`, `429` |
| `GET /api/auth/me` | Access | `200 UserDto` | `401` |

`OtpAuthUri` = `otpauth://totp/Webboard:<username>?secret=<base32>&issuer=Webboard&algorithm=SHA1&digits=6&period=30`

JSON เป็น camelCase: response ของ `2fa/setup` คือ `{ "sharedKey": …, "otpAuthUri": … }`

Configuration (user-secrets ตอน dev, env var ตอน deploy — ห้ามอยู่ใน `appsettings*.json`):

| Key | ค่า |
| --- | --- |
| `Jwt:SigningKey` | สุ่มอย่างน้อย 32 bytes (base64) |
| `Jwt:Issuer` / `Jwt:Audience` | `webboard` / `webboard-api` (อยู่ใน `appsettings.json` ได้) |
| `DataProtection:KeysPath` | โฟลเดอร์นอก repo ที่เก็บ key ring |

ตอนรัน test ไม่ต้องตั้ง `Jwt:SigningKey` และ `DataProtection:KeysPath` — `WebboardApiFactory` ใส่ signing key แบบสุ่มและใช้ Data Protection แบบ ephemeral ให้ ([spec-webboard-mvp](spec-webboard-mvp.md) D12)

## 4. Acceptance

- [ ] ในฐานข้อมูลไม่มีคอลัมน์ใดเก็บรหัสผ่านหรือ shared key แบบอ่านได้ (`SELECT password_hash, totp_secret_protected FROM app_user`)
- [ ] Unit test: hash เดียวกันสองครั้งได้ string ต่างกัน (salt ต่าง) แต่ verify ผ่านทั้งคู่; รหัสผิด verify ไม่ผ่าน
- [ ] สมัคร → login → ได้ `twoFactorSetupRequired: true` → setup → verify ด้วยรหัสจาก Otp.NET → ได้ access token → `GET /api/auth/me` = `200`
- [ ] ใช้รหัส TOTP เดิมซ้ำใน step เดียวกัน → `401`
- [ ] ใช้ MFA token เรียก `POST /api/topics` → `401`; ใช้ access token เรียก `/api/auth/2fa/verify` → `401`
- [ ] รหัสผ่านผิด 5 ครั้งติด → ครั้งที่ 5 ได้ `429` และ login ด้วยรหัสถูกระหว่างล็อกก็ได้ `429`
- [ ] Username ไม่มีจริงกับรหัสผิด ได้ status และ body เหมือนกัน
- [ ] ไม่มีรหัสผ่าน, รหัส TOTP หรือ token ใน log ของ API ระหว่างรัน test ทั้งหมด

## 5. Open questions

- Q1 — ผู้ใช้ทำมือถือหาย: ไม่มี recovery code และยังไม่มีบทบาท admin ที่จะ reset 2FA ให้ — ยอมรับให้ลบ `totp_secret_protected` ด้วย SQL ตอน demo หรือต้องเพิ่ม FR?
- Q2 — ต้องจำกัดจำนวนครั้งต่อ IP เพิ่มจาก lockout ต่อบัญชีหรือไม่ (กันการไล่ล็อกหลายบัญชี)
