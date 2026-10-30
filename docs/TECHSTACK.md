# Webboard — Technology Stack

> **ใช้อะไรรัน** — คู่กับ [SPECREQUIREMENT.md](SPECREQUIREMENT.md) (ต้องทำอะไร) และ
> [ARCHITECTURE.md](ARCHITECTURE.md) (โครงสร้างเป็นอย่างไร)
>
> แถวที่โจทย์บังคับเปลี่ยนไม่ได้ (Domain / Data Access / Database) ส่วนเวอร์ชันเป็นค่าที่เสนอ —
> เมื่อสร้าง solution แล้วให้แก้ตารางนี้ให้ตรงกับไฟล์ `.csproj` จริง

## 1. Domain Layer (`src/Webboard.Api/`)

| Concern | Choice | Notes |
| --- | --- | --- |
| Runtime | **.NET 10 (LTS)** | ใช้ SDK เดียวกันทั้งทีม — ปักเวอร์ชันด้วย `global.json` |
| Web framework | **ASP.NET Core Web API** (controllers) | โจทย์กำหนด |
| API docs | **OpenAPI** (`Microsoft.AspNetCore.OpenApi`) + Swagger UI | เปิดเฉพาะ Development |
| Validation | DataAnnotations (`[Required]`, `[MaxLength]`) | ตอบ `400` เป็น Problem Details อัตโนมัติ |
| Errors | `AddProblemDetails()` | RFC 9457 |

## 2. Data Access Layer (`src/Webboard.DataAccess/`)

| Concern | Choice | Notes |
| --- | --- | --- |
| ORM | **Entity Framework Core 10** | โจทย์กำหนด |
| Provider | **Npgsql.EntityFrameworkCore.PostgreSQL** | driver ของ PostgreSQL สำหรับ EF Core |
| Migrations | `dotnet-ef` CLI | ติดตั้งเป็น local tool (`dotnet tool restore`) ให้ทุกคนใช้เวอร์ชันเดียวกัน |
| Naming | snake_case ใน DB | ใช้ `EFCore.NamingConventions` (`UseSnakeCaseNamingConvention()`) |

## 3. Database

| Concern | Choice | Notes |
| --- | --- | --- |
| DBMS | **PostgreSQL 17** | โจทย์กำหนด |
| Local dev | Docker (`postgres:17`) หรือ PostgreSQL ที่ติดตั้งในเครื่อง | connection string อยู่ใน user-secrets ห้าม commit |

## 4. Presentation Layer

**ยังไม่กำหนด** — โจทย์ให้เลือกได้อิสระ เงื่อนไขเดียวคือคุยกับ Domain Layer ผ่าน REST API (NFR-001)
ตัดสินใจแล้วให้เขียน ADR ใน `docs/architecture/decisions/` และเติมตารางนี้

| Concern | Choice | Notes |
| --- | --- | --- |
| Framework | TBD | |
| HTTP client | TBD | |
| Dev server / CORS | TBD | ถ้า UI รันคนละ port กับ API ต้องตั้ง CORS ที่ `Program.cs` |

## 5. Dev / Test Tooling

| Tool | Purpose |
| --- | --- |
| **xUnit** | unit + integration test ของ Domain Layer |
| `Microsoft.AspNetCore.Mvc.Testing` | ยิง HTTP เข้า API จริงใน test |
| `dotnet format` | จัดรูปแบบโค้ดตาม `.editorconfig` |
| `dotnet build -warnaserror` | build ต้องไม่มี warning ก่อน merge |
