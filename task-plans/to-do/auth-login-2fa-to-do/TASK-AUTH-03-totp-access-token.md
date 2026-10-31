# TASK-AUTH-03 — 2FA setup/verify, access token, JWT bearer policies, `/me`

**Status:** DONE
**Owner:** narawichswu (Claude)
**Depends on:** AUTH-02
**Spec:** `task-plans/spec/spec-auth-login-2fa.md` §2 D4–D8, §3; FR-AUTH-004, 005, 007, 008
**Produces:** `TotpService`, `TokenService.CreateAccessToken`, JWT bearer ที่ default policy ต้องการ `purpose=access`, policy `MfaPending`, `ClaimsPrincipalExtensions.GetUserId()`, test helper `GetAccessTokenAsync(client, username)`

## Goal

ผู้ใช้ตั้ง 2FA ครั้งแรกและยืนยันรหัส TOTP เพื่อรับ access token ได้ และ endpoint ทั้งระบบแยก MFA token กับ access token ออกจากกันจริง

## Exact files

- Modify `src/Webboard.Api/Webboard.Api.csproj` — `Otp.NET`, `Microsoft.AspNetCore.Authentication.JwtBearer`
- Create `src/Webboard.Api/Security/TotpService.cs` — `NewSharedKey()`, `Match(secret, code) → long? step`
- Modify `src/Webboard.Api/Security/TokenService.cs` — `CreateAccessToken(User)` (`purpose=access`, 60 นาที)
- Create `src/Webboard.Api/Security/ClaimsPrincipalExtensions.cs`
- Modify `src/Webboard.Api/Dtos/AuthDtos.cs` — `TwoFactorSetupResponse`, `VerifyTwoFactorRequest`, `AuthResponse`
- Modify `src/Webboard.Api/Services/AuthService.cs` — `BeginTwoFactorSetupAsync`, `VerifyTwoFactorAsync`
- Modify `src/Webboard.Api/Controllers/AuthController.cs` — `2fa/setup`, `2fa/verify` (`[Authorize(Policy = "MfaPending")]`), `me` (`[Authorize]`)
- Modify `src/Webboard.Api/Program.cs` — `AddAuthentication().AddJwtBearer(...)`, default policy + `MfaPending`, `AddDataProtection().PersistKeysToFileSystem(DataProtection:KeysPath)` (นอก `Testing` ถ้าไม่ได้ตั้ง key นี้ → ไม่ start และบอกชื่อ key)
- Modify `tests/Webboard.Api.Tests/WebboardApiFactory.cs` — `AddDataProtection().UseEphemeralDataProtectionProvider()` (spec-webboard-mvp D12)
- Modify `README.md` — `dotnet user-secrets set DataProtection:KeysPath <โฟลเดอร์นอก repo> --project src/Webboard.Api`
- Create `tests/Webboard.Api.Tests/AuthTwoFactorTests.cs` (`[Collection("Database")]`), `TestAuth.cs` (helper)

## Steps

- [ ] เขียน test ที่ fail (คำนวณรหัสใน test ด้วย Otp.NET จาก `sharedKey` ที่ setup คืนมา):
  - setup ด้วย MFA token → `200` มี `sharedKey`, `otpAuthUri` รูปแบบตาม spec; ค่าใน DB ≠ sharedKey (ถูก protect)
  - setup ซ้ำหลังเปิด 2FA แล้ว → `409`
  - verify รหัสถูก → `200` `accessToken` (`purpose=access`) และ `two_factor_enabled = true`
  - verify รหัสเดิมซ้ำ → `401`; รหัสผิด 5 ครั้ง → `429`
  - `GET /api/auth/me` ด้วย access token → `200`; ด้วย MFA token → `401`
  - `POST /api/auth/2fa/verify` ด้วย access token → `401`
  - MFA token หมดอายุ (ปรับเวลาด้วย `TimeProvider` ปลอม) → `401`
- [ ] รันให้แดง
- [ ] Implement ตาม D4–D8; ใช้ `TimeProvider` ทั้งใน `TotpService` และ `TokenService` เพื่อให้ test คุมเวลาได้
- [ ] สร้าง `TestAuth.GetAccessTokenAsync` ให้ MVP-04/05 ใช้
- [ ] รันให้เขียว + quality gates

## Verification

```sh
dotnet test --filter FullyQualifiedName~AuthTwoFactorTests   # Passed! - Failed: 0
dotnet test                                                   # Passed! - Failed: 0
```

แล้วลองด้วยมือ: สแกน `otpAuthUri` (แปลงเป็น QR) ด้วยแอป Authenticator จริงบนมือถือ → verify ผ่าน

## Commit

`feat(auth): add totp two-factor verification and access tokens`

Stage: `src/Webboard.Api/ README.md tests/Webboard.Api.Tests/WebboardApiFactory.cs tests/Webboard.Api.Tests/AuthTwoFactorTests.cs tests/Webboard.Api.Tests/TestAuth.cs`
