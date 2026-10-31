# Webboard — Technology Stack

> **ใช้อะไรรัน** — คู่กับ [SPECREQUIREMENT.md](SPECREQUIREMENT.md) (ต้องทำอะไร) และ
> [ARCHITECTURE.md](ARCHITECTURE.md) (โครงสร้างเป็นอย่างไร)
>
> แถวที่โจทย์บังคับเปลี่ยนไม่ได้ (Domain / Data Access / Database) ส่วนเวอร์ชันเป็นค่าที่เสนอ —
> เมื่อสร้าง solution แล้วให้แก้ตารางนี้ให้ตรงกับไฟล์ `.csproj` จริง

## 1. Domain Layer (`src/Webboard.Api/`)

| Concern | Choice | Notes |
| --- | --- | --- |
| Runtime | **.NET 10 (LTS)** | SDK 10.0.403 (`rollForward: latestFeature`) — ปักเวอร์ชันด้วย `global.json` |
| Web framework | **ASP.NET Core Web API** (controllers) | โจทย์กำหนด |
| API docs | **OpenAPI** (`Microsoft.AspNetCore.OpenApi` 10.0.14) + Swagger UI (`Swashbuckle.AspNetCore.SwaggerUI` 10.3.1) | เปิดเฉพาะ Development |
| Validation | DataAnnotations (`[Required]`, `[MaxLength]`) | ตอบ `400` เป็น Problem Details อัตโนมัติ |
| Errors | `AddProblemDetails()` | RFC 9457 |
| Auth — password hash | **PBKDF2-HMAC-SHA256** ผ่าน `Rfc2898DeriveBytes.Pbkdf2` (BCL) | 600,000 รอบ, salt 16 bytes, hash 32 bytes, เทียบด้วย `CryptographicOperations.FixedTimeEquals` — ดู [ADR](architecture/decisions/ADR-auth-password-2fa.md) |
| Auth — 2FA | **TOTP** (RFC 6238) ผ่าน **Otp.NET** | SHA-1, 6 หลัก, 30 วินาที, ยอมคลาด ±1 ช่วง |
| Auth — tokens | **JWT Bearer** (`Microsoft.AspNetCore.Authentication.JwtBearer`) | HS256; access 60 นาที, MFA 5 นาที; signing key อยู่ใน user-secrets |
| Secret at rest | **ASP.NET Core Data Protection** | เข้ารหัส shared key ของ 2FA ก่อนลง DB |

## 2. Data Access Layer (`src/Webboard.DataAccess/`)

| Concern | Choice | Notes |
| --- | --- | --- |
| ORM | **Entity Framework Core 10** (10.0.14) | โจทย์กำหนด |
| Provider | **Npgsql.EntityFrameworkCore.PostgreSQL** 10.0.3 | driver ของ PostgreSQL สำหรับ EF Core |
| Migrations | `dotnet-ef` CLI 10.0.14 | ติดตั้งเป็น local tool (`dotnet tool restore`) ให้ทุกคนใช้เวอร์ชันเดียวกัน |
| Naming | snake_case ใน DB | ใช้ `EFCore.NamingConventions` 10.0.1 (`UseSnakeCaseNamingConvention()`) |

## 3. Database

| Concern | Choice | Notes |
| --- | --- | --- |
| DBMS | **PostgreSQL 17** | โจทย์กำหนด |
| Local dev | Docker (`postgres:17`) ผ่าน `compose.yaml` หรือ PostgreSQL ที่ติดตั้งในเครื่อง | password อยู่ใน `.env` (gitignored); connection string อยู่ใน user-secrets ห้าม commit |

## 4. Presentation Layer

**ยังไม่กำหนด** — โจทย์ให้เลือกได้อิสระ เงื่อนไขเดียวคือคุยกับ Domain Layer ผ่าน REST API (NFR-001)
ตัดสินใจแล้วให้เขียน ADR ใน `docs/architecture/decisions/` และเติมตารางนี้

| Concern | Choice | Notes |
| --- | --- | --- |
| Framework | TBD | |
| HTTP client | TBD | |
| Dev server / CORS | TBD | ถ้า UI รันคนละ port กับ API ต้องตั้ง CORS ที่ `Program.cs` (อนุญาต header `Authorization`) |
| QR code | TBD | แปลง `otpAuthUri` จาก `/api/auth/2fa/setup` เป็น QR ให้แอป Authenticator สแกน |

## 5. Dev / Test Tooling

| Tool | Purpose |
| --- | --- |
| **xUnit** 2.9.3 | unit + integration test ของ Domain Layer |
| `Microsoft.AspNetCore.Mvc.Testing` 10.0.14 | ยิง HTTP เข้า API จริงใน test |
| `dotnet format` | จัดรูปแบบโค้ดตาม `.editorconfig` |
| `dotnet build -warnaserror` | build ต้องไม่มี warning ก่อน merge |
