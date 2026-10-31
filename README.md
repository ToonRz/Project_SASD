# Project_SASD — Webboard App

เว็บบอร์ดสำหรับตั้งกระทู้และแสดงความคิดเห็น (แนว Reddit, Pantip) — โจทย์ C ของรายวิชา SASD

สถาปัตยกรรมแบบ 3 ชั้น (layered):

| Layer | Technology |
| --- | --- |
| Presentation Layer | ยังไม่กำหนด (เลือกได้อิสระ) — ดู [docs/TECHSTACK.md](docs/TECHSTACK.md) |
| Domain Layer | ASP.NET Core (REST API) |
| Data Access Layer | Entity Framework Core |
| Database | PostgreSQL |

## Requirement ขั้นต่ำ

- ตั้งกระทู้ (topic) ได้
- เขียนความเห็น (comment) ได้
- สมัครสมาชิก / login ด้วยรหัสผ่าน + 2FA (TOTP) — รหัสผ่านเก็บเป็น PBKDF2-HMAC-SHA256 ([เหตุผล](docs/architecture/decisions/ADR-auth-password-2fa.md))

ส่วนเสริม (ถ้ามีเวลา): comment ย่อยใน comment, tag ให้กระทู้, แยกห้อง (room)

## Quick start

ต้องมี .NET SDK 10 (ปักเวอร์ชันไว้ใน `global.json`)

```sh
dotnet tool restore                       # ติดตั้ง dotnet-ef (local tool)
dotnet build
dotnet test
dotnet run --project src/Webboard.Api     # Development: Swagger UI ที่ /swagger, OpenAPI ที่ /openapi/v1.json
```

### ฐานข้อมูล (PostgreSQL 17 ผ่าน Docker)

```sh
cp .env.example .env                      # แล้วแก้ POSTGRES_PASSWORD (ไฟล์ .env ไม่ถูก commit)
docker compose up -d db
dotnet user-secrets set ConnectionStrings:Webboard "Host=localhost;Port=5432;Database=webboard;Username=webboard;Password=<POSTGRES_PASSWORD>" --project src/Webboard.Api
dotnet user-secrets set ConnectionStrings:WebboardTest "Host=localhost;Port=5432;Database=webboard_test;Username=webboard;Password=<POSTGRES_PASSWORD>" --project tests/Webboard.Api.Tests
```

- ถ้าพอร์ต 5432 ถูกใช้อยู่ (เช่นมี PostgreSQL ติดตั้งในเครื่อง) ให้ตั้ง `POSTGRES_PORT=5433` ใน `.env` และใช้ `Port=5433` ใน connection string
- script ใน `docker/initdb/` รันเฉพาะตอนสร้าง volume ครั้งแรก — ถ้ามี volume `pgdata` อยู่แล้วให้รัน `docker compose exec db createdb -U webboard webboard_test` เอง
- Integration test ใช้ฐาน `webboard_test` เท่านั้น และต้องตั้ง `ConnectionStrings:WebboardTest` (หรือ env var `ConnectionStrings__WebboardTest`)

### กุญแจ JWT (สำหรับ `dotnet run` เท่านั้น — test ไม่ต้องตั้ง)

```sh
dotnet user-secrets set Jwt:SigningKey "$(openssl rand -base64 32)" --project src/Webboard.Api
```

- ถ้าไม่ตั้ง หรือสั้นกว่า 32 bytes API จะไม่ start และข้อความ error บอกชื่อ `Jwt:SigningKey`

## Documents

| คำถาม | อ่านที่ |
| --- | --- |
| ระบบต้องทำอะไร / ขอบเขต / requirement | [docs/SPECREQUIREMENT.md](docs/SPECREQUIREMENT.md) |
| โครงสร้างระบบ / layer / data model / API | [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) |
| เทคโนโลยีและเวอร์ชันที่ใช้ | [docs/TECHSTACK.md](docs/TECHSTACK.md) |
| ศัพท์ของโดเมน (Topic, Comment, Room …) | [CONTEXT.md](CONTEXT.md) |
| Branch / commit / PR | [CONTRIBUTING.md](CONTRIBUTING.md) |
| สมาชิกทีม | [CONTRIBUTORS.md](CONTRIBUTORS.md) |
| งานที่ยังเปิดอยู่ / ลำดับงาน / ใครทำ | [task-plans/to-do/](task-plans/to-do/README.md) |
| Design spec ต่อฟีเจอร์ | [task-plans/spec/](task-plans/spec/) |
| เหตุผลของการตัดสินใจเชิงสถาปัตยกรรม | [docs/architecture/decisions/](docs/architecture/decisions/) |
| Mock แบบคลิกได้ ดูว่า request วิ่งผ่าน 3 layer อย่างไร | [docs/mockups/webboard-layers.html](docs/mockups/webboard-layers.html) (เปิดในเบราว์เซอร์ได้เลย) |
