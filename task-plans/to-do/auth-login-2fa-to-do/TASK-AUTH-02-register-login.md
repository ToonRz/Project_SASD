# TASK-AUTH-02 — `register`, `login` (MFA token), lockout

**Status:** TODO
**Owner:** —
**Depends on:** AUTH-01
**Spec:** `task-plans/spec/spec-auth-login-2fa.md` §2 D3, D6, D8, D9, §3; FR-AUTH-001, 003, 006
**Produces:** `AuthService.RegisterAsync/LoginAsync`, `TokenService.CreateMfaToken`, `AuthController` (`register`, `login`)

## Goal

ผู้ใช้สมัครได้ และ login ขั้นที่ 1 ด้วยรหัสผ่านแล้วได้ MFA token; การเดารหัสถูกจำกัดด้วย lockout และไม่บอกว่า username มีจริงหรือไม่

## Exact files

- Create `src/Webboard.Api/Dtos/AuthDtos.cs` — `RegisterRequest`, `UserDto`, `LoginRequest`, `LoginResponse`
- Create `src/Webboard.Api/Services/AuthService.cs`
- Create `src/Webboard.Api/Security/TokenService.cs` — `CreateMfaToken(User)` (HS256, `purpose=mfa`, 5 นาที)
- Create `src/Webboard.Api/Controllers/AuthController.cs`
- Modify `src/Webboard.Api/Program.cs` — register services, bind `Jwt` options
- Modify `src/Webboard.Api/appsettings.json` — `Jwt:Issuer`, `Jwt:Audience` (ไม่มี `SigningKey`)
- Modify `README.md` — `dotnet user-secrets set Jwt:SigningKey "$(openssl rand -base64 32)"`
- Create `tests/Webboard.Api.Tests/AuthRegisterLoginTests.cs`

## Steps

- [ ] เขียน test ที่ fail:
  - register ถูกต้อง → `201` `twoFactorEnabled: false`; username `TON` แล้ว `ton` → `409`
  - register password 7 ตัว → `400` มี `errors.Password`; username `a b` → `400`
  - login ถูก → `200` มี `mfaToken` (decode แล้ว `purpose=mfa`, `exp` ≈ 5 นาที) และ `twoFactorSetupRequired: true`
  - login รหัสผิด กับ username ไม่มีจริง → ทั้งคู่ `401` body `detail` เดียวกัน
  - รหัสผิด 5 ครั้งติด → ครั้งที่ 5 `429` มี `Retry-After`; รหัสถูกระหว่างล็อก → `429`
  - log ที่ test จับได้ (`ILogger` sink) ไม่มีสตริงรหัสผ่านที่ส่ง
- [ ] รันให้แดง
- [ ] Implement ตาม D3 (เรียก `Verify` กับ `DummyHash` เมื่อไม่พบ user), D8, D9
- [ ] รันให้เขียว + quality gates

## Verification

```sh
dotnet test --filter FullyQualifiedName~AuthRegisterLoginTests   # Passed! - Failed: 0
git grep -n "SigningKey" -- '*.json'                              # ไม่มีผลลัพธ์
```

## Commit

`feat(auth): add register and password login with lockout`

Stage: `src/Webboard.Api/ README.md tests/Webboard.Api.Tests/AuthRegisterLoginTests.cs`
