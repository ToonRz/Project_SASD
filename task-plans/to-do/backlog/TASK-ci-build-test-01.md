# TASK-ci-build-test-01 — GitHub Actions: build, test, format ทุก PR

**Status:** TODO
**Owner:** —
**Depends on:** MVP-02 (ต้องมี solution และ test ที่ใช้ PostgreSQL แล้ว)
**Spec:** `CONTRIBUTING.md` (Pull Requests checklist); `task-plans/to-do/README.md` (Project hooks → Quality gates)
**Produces:** workflow `ci` ที่ใช้เป็น required status check ของ `main` และ `develop`

## Goal

ทุก PR เข้า `develop`/`main` ถูกตรวจ build, test และ format อัตโนมัติ บน PostgreSQL จริง ก่อน merge

## Exact files

- Create `.github/workflows/ci.yml` — trigger `pull_request` + `push` ที่ `main`, `develop`; job เดียว:
  - `services.postgres: postgres:17` (health check `pg_isready`) และสร้าง `webboard_test`
  - `actions/setup-dotnet` อ่านเวอร์ชันจาก `global.json`
  - `dotnet tool restore`, `dotnet build -warnaserror`, `dotnet format --verify-no-changes`, `dotnet test`
  - `ConnectionStrings__WebboardTest` และ `Jwt__SigningKey` ตั้งเป็นค่าทดสอบใน `env` ของ job (ไม่ใช่ secret จริง)
- Modify `CONTRIBUTING.md` — ระบุว่า PR ต้องผ่าน check `ci`

## Steps

- [ ] เขียน workflow
- [ ] เปิด PR ทดสอบที่ตั้งใจให้ test fail หนึ่งตัว → check `ci` ต้องแดง
- [ ] แก้ให้ผ่าน → check `ci` เขียว

## Verification

```sh
gh run list --workflow ci.yml --limit 2   # หนึ่ง run failure (PR ทดสอบ) และหนึ่ง run success
```

## Commit

`ci: build, test and format check on pull requests`

Stage: `.github/workflows/ci.yml CONTRIBUTING.md`
