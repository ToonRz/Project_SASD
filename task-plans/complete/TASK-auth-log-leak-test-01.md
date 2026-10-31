# TASK-auth-log-leak-test-01 — ตรวจว่า log ไม่มีรหัส TOTP, shared key หรือ token

**Status:** DONE
**Owner:** narawichswu (Claude)
**Spec:** `task-plans/spec/spec-auth-login-2fa.md` §4 Acceptance (บรรทัด log), NFR-006
**Produces:** test `Logs_NeverContainTotpCodesSharedKeysOrTokens` + การปิดช่องที่รหัสผ่านรั่วใน log ระดับ Trace

## Goal

พิสูจน์ว่าระหว่าง flow register → login → 2FA setup → verify → `/me` → สร้างกระทู้ ไม่มีรหัสผ่าน, MFA token, access token, shared key (และ secret ใน `otpAuthUri`) หรือรหัส TOTP ที่ส่งไปอยู่ใน log แม้เปิด log ระดับ Trace

## Exact files

- `tests/Webboard.Api.Tests/AuthTwoFactorTests.cs` (เพิ่ม test)
- `src/Webboard.Api/Dtos/AuthDtos.cs` (redact secret ใน `ToString` ของ DTO ที่มี secret)
- `task-plans/spec/spec-auth-login-2fa.md` (ติ๊ก acceptance)

## Steps

- [x] เขียน test: เปิด Trace ให้ `Webboard`, `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore` (EF sensitive-data logging คงปิด)
- [x] รัน test ให้แดง — พบรั่วจริง: `ControllerActionInvoker` (Trace) log argument ของ action ด้วย `record.ToString()` → `Password = ...` ของ `RegisterRequest`/`LoginRequest`
- [x] แก้ที่ `src/`: DTO ที่มี secret เป็น `sealed record` พร้อม `PrintMembers` ที่ปิดค่า (`***`)
- [x] รันให้เขียว + quality gates

## Verification

```
dotnet test --filter Logs_NeverContainTotpCodesSharedKeysOrTokens
dotnet build -warnaserror; dotnet test; dotnet format --verify-no-changes
```

## Commit

`test(auth): check logs never contain totp codes, shared keys or tokens` — stage เฉพาะไฟล์ใน Exact files + ไฟล์ TASK นี้

## Receipt

False positives: token/key/รหัสผ่านเทียบแบบ substring; รหัส TOTP 6 หลักเทียบเป็น whole token เท่านั้น (`(?<![A-Za-z0-9])code(?![A-Za-z0-9])`) กันเลขบังเอิญใน id/timestamp

```
$ dotnet test --filter Logs_NeverContainTotp  (ก่อนแก้) -> Failed: Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker: Executing action method ...AuthController.Login ... (LoginRequest { Username = ton, Password = <plaintext> })
$ dotnet build -warnaserror            -> 0 Warning(s), 0 Error(s)
$ dotnet test                          -> Passed! Failed: 0, Passed: 62, Total: 62
$ dotnet format --verify-no-changes    -> exit 0
```
