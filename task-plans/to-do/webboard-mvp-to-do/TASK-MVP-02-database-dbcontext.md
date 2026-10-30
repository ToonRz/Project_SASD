# TASK-MVP-02 — PostgreSQL ผ่าน compose, `WebboardDbContext`, test fixture

**Status:** TODO
**Owner:** —
**Depends on:** MVP-01
**Spec:** `task-plans/spec/spec-webboard-mvp.md` §2 D3, D7, D10, §3 Configuration
**Produces:** `WebboardDbContext` ที่ register ใน DI, `compose.yaml` สำหรับ dev, `WebboardApiFactory` ที่ชี้ `webboard_test` และล้างข้อมูลก่อนแต่ละ test

## Goal

นักพัฒนาทุกคนรัน PostgreSQL 17 ได้ด้วยคำสั่งเดียว API ต่อฐานข้อมูลผ่าน EF Core ด้วย connection string จาก user-secrets และ test ใช้ฐานแยก

## Exact files

- Create `compose.yaml` — service `db`: `postgres:17`, port `5432`, `POSTGRES_USER`/`POSTGRES_DB` = `webboard`, `POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}`, volume `pgdata`
- Create `.env.example` — `POSTGRES_PASSWORD=change-me`
- Create `docker/initdb/01-test-db.sql` — `CREATE DATABASE webboard_test;` (mount ที่ `/docker-entrypoint-initdb.d`)
- Create `src/Webboard.DataAccess/WebboardDbContext.cs`
- Modify `src/Webboard.DataAccess/Webboard.DataAccess.csproj` — `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`, `Microsoft.EntityFrameworkCore.Design`
- Modify `src/Webboard.Api/Program.cs` — `AddDbContext<WebboardDbContext>(o => o.UseNpgsql(cs).UseSnakeCaseNamingConvention())`
- Create `tests/Webboard.Api.Tests/WebboardApiFactory.cs`, `DatabaseTests.cs`
- Modify `README.md` — ขั้นตอน `cp .env.example .env`, `docker compose up -d db`, `dotnet user-secrets set ConnectionStrings:Webboard ...`

## Steps

- [ ] เขียน test ที่ fail: resolve `WebboardDbContext` จาก factory แล้ว `Database.CanConnectAsync()` เป็น `true`
- [ ] รันให้แดง (ยังไม่มี DbContext)
- [ ] เพิ่ม compose, DbContext, DI; factory override `ConnectionStrings:Webboard` ด้วย `ConnectionStrings:WebboardTest` และรัน `Database.MigrateAsync()` ครั้งเดียวต่อ test run
- [ ] รันให้เขียว แล้วรัน quality gates
- [ ] ตรวจว่า `git status` ไม่มี `.env`

## Verification

```sh
cp .env.example .env && docker compose up -d db
docker compose exec db psql -U webboard -lqt | cut -d'|' -f1 | grep -w webboard_test   # webboard_test
dotnet test                                                                              # Passed! - Failed: 0
git check-ignore .env                                                                    # .env
```

## Commit

`feat(data): add postgres compose service and webboard db context`

Stage: `compose.yaml .env.example docker/initdb/ src/Webboard.DataAccess/ src/Webboard.Api/Program.cs tests/Webboard.Api.Tests/ README.md`
