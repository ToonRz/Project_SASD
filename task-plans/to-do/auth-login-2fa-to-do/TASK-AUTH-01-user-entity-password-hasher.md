# TASK-AUTH-01 — Entity `User` (`app_user`) + `PasswordHasher`

**Status:** DONE
**Owner:** narawichswu (Claude)
**Depends on:** MVP-02
**Spec:** `task-plans/spec/spec-auth-login-2fa.md` §2 D1, D2, D9; `docs/ARCHITECTURE.md` §6 (`APP_USER`)
**Produces:** `DbSet<User> Users`, migration `AddAppUser`, `PasswordHasher.Hash/Verify/DummyHash`

## Goal

มีตาราง `app_user` และ class ที่ hash/ตรวจรหัสผ่านด้วย PBKDF2-HMAC-SHA256 ตาม ADR พร้อม unit test ที่พิสูจน์ salt และการตรวจ

## Exact files

- Create `src/Webboard.DataAccess/Entities/User.cs` — คอลัมน์ตาม ARCHITECTURE §6
- Create `src/Webboard.DataAccess/Configurations/UserConfiguration.cs` — `ToTable("app_user")`, unique index `username`, `display_name` max 50, `username` max 30
- Modify `src/Webboard.DataAccess/WebboardDbContext.cs` — `DbSet<User> Users`
- Create `src/Webboard.DataAccess/Migrations/<timestamp>_AddAppUser.cs` (สร้างด้วย `dotnet ef`)
- Create `src/Webboard.Api/Security/PasswordHasher.cs`
- Create `tests/Webboard.Api.Tests/PasswordHasherTests.cs`, `UserSchemaTests.cs`

## Steps

- [x] เขียน test ที่ fail:
  - `Hash("webboard123")` สองครั้งได้ string ต่างกัน, ขึ้นต้น `PBKDF2-SHA256$600000$`, และ `Verify` ผ่านทั้งคู่
  - `Verify("wrong", hash)` = `false`; `Verify(x, DummyHash)` = `false` เสมอ
  - Known-answer: salt และ hash ที่คำนวณด้วย Python `hashlib.pbkdf2_hmac('sha256', b'webboard123', salt, 600000, 32)` ต้อง verify ผ่าน (ตรวจว่าใช้อัลกอริทึมเดียวกันจริง)
  - insert `User` สองคน username ซ้ำ → `DbUpdateException`
- [x] รันให้แดง
- [x] Implement `PasswordHasher` ตาม D1/D2 (อ่าน iterations จาก string ตอน Verify), entity + migration
- [x] รันให้เขียว + quality gates

## Verification

```sh
dotnet test --filter "FullyQualifiedName~PasswordHasherTests|FullyQualifiedName~UserSchemaTests"   # Passed! - Failed: 0
docker compose exec db psql -U webboard -c '\d app_user'                                          # unique index on username
grep -rn "SHA256.HashData\|ComputeHash" src/                                                       # ไม่มีผลลัพธ์
```

## Commit

`feat(auth): add app_user table and pbkdf2-sha256 password hasher`

Stage: `src/Webboard.DataAccess/ src/Webboard.Api/Security/PasswordHasher.cs tests/Webboard.Api.Tests/PasswordHasherTests.cs tests/Webboard.Api.Tests/UserSchemaTests.cs`
