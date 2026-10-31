# TASK-MVP-02 — PostgreSQL ผ่าน compose, `WebboardDbContext`, test fixture

**Status:** TODO
**Owner:** —
**Depends on:** MVP-01
**Spec:** `task-plans/spec/spec-webboard-mvp.md` §2 D3, D7, D10, D11, D12, §3 Configuration
**Produces:** `WebboardDbContext` ที่ register ใน DI, `compose.yaml` สำหรับ dev, `WebboardApiFactory` ที่ชี้ `webboard_test` พร้อม `ResetDatabaseAsync()`, collection `"Database"` ที่ test class ถัดไปใช้

## Goal

นักพัฒนาทุกคนรัน PostgreSQL 17 ได้ด้วยคำสั่งเดียว API ต่อฐานข้อมูลผ่าน EF Core ด้วย connection string จาก user-secrets และ test ใช้ฐานแยก

## Exact files

- Create `compose.yaml` — service `db`: `postgres:17`, port `5432`, `POSTGRES_USER`/`POSTGRES_DB` = `webboard`, `POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}`, volume `pgdata`
- Create `.env.example` — `POSTGRES_PASSWORD=change-me`
- Create `docker/initdb/01-test-db.sql` — `CREATE DATABASE webboard_test;` (mount ที่ `/docker-entrypoint-initdb.d`)
- Create `src/Webboard.DataAccess/WebboardDbContext.cs`
- Modify `src/Webboard.DataAccess/Webboard.DataAccess.csproj` — `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`, `Microsoft.EntityFrameworkCore.Design`
- Modify `src/Webboard.Api/Program.cs` — `AddDbContext<WebboardDbContext>(o => o.UseNpgsql(cs).UseSnakeCaseNamingConvention())`
- Modify `tests/Webboard.Api.Tests/Webboard.Api.Tests.csproj` — เพิ่ม `<UserSecretsId>` (สร้างด้วย `dotnet user-secrets init --project tests/Webboard.Api.Tests`)
- Create `tests/Webboard.Api.Tests/WebboardApiFactory.cs` — environment `Testing`, อ่าน `ConnectionStrings:WebboardTest` จาก user-secrets/env var, `ResetDatabaseAsync()` ตาม D11
- Create `tests/Webboard.Api.Tests/DatabaseCollection.cs` — `[CollectionDefinition("Database")] : ICollectionFixture<WebboardApiFactory>`
- Create `tests/Webboard.Api.Tests/DatabaseTests.cs`
- Modify `README.md` — ขั้นตอน `cp .env.example .env`, `docker compose up -d db`, `dotnet user-secrets set ConnectionStrings:Webboard ... --project src/Webboard.Api`, `dotnet user-secrets set ConnectionStrings:WebboardTest ... --project tests/Webboard.Api.Tests` และหมายเหตุ: script ใน `docker/initdb/` รันเฉพาะตอนสร้าง volume ครั้งแรก — ถ้ามี volume `pgdata` อยู่แล้วให้รัน `docker compose exec db createdb -U webboard webboard_test` เอง

## Steps

- [ ] เขียน test ที่ fail:
  - resolve `WebboardDbContext` จาก factory แล้ว `Database.CanConnectAsync()` เป็น `true`
  - ไม่ได้ตั้ง `ConnectionStrings:WebboardTest` → factory throw และข้อความมีชื่อ key (ไม่มีค่า connection string)
- [ ] รันให้แดง (ยังไม่มี DbContext)
- [ ] เพิ่ม compose, DbContext, DI; factory override `ConnectionStrings:Webboard` ด้วย `ConnectionStrings:WebboardTest` และรัน `Database.MigrateAsync()` ครั้งเดียวต่อ test run
- [ ] `ResetDatabaseAsync()`: ดึงชื่อตารางจาก `context.Model.GetEntityTypes()` แล้ว `TRUNCATE … RESTART IDENTITY CASCADE` ในคำสั่งเดียว; model ยังไม่มีตาราง → ไม่ทำอะไร (AUTH-01/MVP-03 จะมี test ที่ใช้จริง)
- [ ] `DatabaseTests` อยู่ใน `[Collection("Database")]` เป็นตัวอย่างให้ test class ถัดไป
- [ ] รันให้เขียว แล้วรัน quality gates
- [ ] ตรวจว่า `git status` ไม่มี `.env`

## Verification

```sh
cp .env.example .env && docker compose up -d db
docker compose exec db psql -U webboard -lqt | cut -d'|' -f1 | grep -w webboard_test   # webboard_test
dotnet user-secrets list --project tests/Webboard.Api.Tests                              # ConnectionStrings:WebboardTest = …
dotnet test                                                                              # Passed! - Failed: 0
git check-ignore .env                                                                    # .env
```

## Commit

`feat(data): add postgres compose service and webboard db context`

Stage: `compose.yaml .env.example docker/initdb/ src/Webboard.DataAccess/ src/Webboard.Api/Program.cs tests/Webboard.Api.Tests/ README.md`
