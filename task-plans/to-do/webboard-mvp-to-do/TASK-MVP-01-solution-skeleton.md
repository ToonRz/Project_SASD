# TASK-MVP-01 — Solution, projects, OpenAPI, ProblemDetails, test project

**Status:** DONE
**Owner:** narawichswu (Claude)
**Depends on:** —
**Spec:** `task-plans/spec/spec-webboard-mvp.md` §2 D1, D6
**Produces:** `Webboard.sln` ที่ build ได้, `Program.cs` ที่ task อื่นเพิ่ม service/endpoint เข้าไป, test project ที่มี `WebApplicationFactory<Program>`

## Goal

Clone repo แล้ว `dotnet build` กับ `dotnet test` ผ่าน มี Swagger UI ตอน Development และ error ทุกตัวเป็น Problem Details

## Exact files

- Create `global.json` — ปัก .NET SDK 10
- Create `.gitignore` — จาก `dotnet new gitignore` (มี `bin/`, `obj/`, `.env`)
- Create `.config/dotnet-tools.json` — `dotnet-ef` (local tool)
- Create `Webboard.sln`
- Create `src/Webboard.Api/Webboard.Api.csproj`, `Program.cs`, `appsettings.json`, `appsettings.Development.json`
- Create `src/Webboard.DataAccess/Webboard.DataAccess.csproj` (class library ว่าง) — Api อ้างโปรเจกต์นี้
- Create `tests/Webboard.Api.Tests/Webboard.Api.Tests.csproj` (xUnit + `Microsoft.AspNetCore.Mvc.Testing`), `OpenApiTests.cs`
- Modify `README.md` — เติม Quick start (`dotnet tool restore`, `dotnet build`, `dotnet test`, `dotnet run --project src/Webboard.Api`)

## Steps

- [x] สร้าง solution และสามโปรเจกต์ด้วย `dotnet new`; ตั้ง `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- [x] เขียน test ที่ fail: `GET /openapi/v1.json` → `200` และ `GET /api/does-not-exist` → `404` แบบ `application/problem+json`
- [x] รัน `dotnet test` ให้แดง
- [x] `Program.cs`: `AddControllers()`, `AddProblemDetails()`, `AddOpenApi()`, `UseExceptionHandler()`, `UseStatusCodePages()`, `MapOpenApi()` + Swagger UI เฉพาะ Development; เพิ่ม `public partial class Program;` ให้ test เห็น
- [x] รัน `dotnet test` ให้เขียว แล้วรัน quality gates

## Verification

```sh
dotnet tool restore
dotnet build -warnaserror        # Build succeeded. 0 Warning(s)
dotnet test                      # Passed! - Failed: 0
dotnet format --verify-no-changes
```

## Commit

`build: add solution skeleton with api, data access and test projects`

Stage: `global.json .gitignore .config/dotnet-tools.json Webboard.sln src/ tests/ README.md`
