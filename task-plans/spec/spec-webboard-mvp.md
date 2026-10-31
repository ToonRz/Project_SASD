# SPEC: Webboard MVP — Topic และ Comment

Status: Draft 2026-10-30 — รอทีม approve ก่อนเริ่ม task
Date: 2026-10-30
Board: [webboard-mvp-to-do/tasks.md](../to-do/webboard-mvp-to-do/tasks.md)
Requirements: FR-TOPIC-001…004, FR-COMMENT-001…003, NFR-001…005 ([SPECREQUIREMENT](../../docs/SPECREQUIREMENT.md))
Design: [ARCHITECTURE.md](../../docs/ARCHITECTURE.md), [ADR-layered-architecture](../../docs/architecture/decisions/ADR-layered-architecture.md)
Related spec: [spec-auth-login-2fa.md](spec-auth-login-2fa.md) — endpoint เขียนข้อมูลใช้ access token จาก spec นั้น

## 1. เป้าหมาย

1. มี solution .NET ที่ build และ test ได้ ตามโครงสร้าง ARCHITECTURE §4
2. Reader ดูรายการกระทู้และเปิดอ่านกระทู้พร้อมความเห็นได้; Member ที่ login แล้วตั้งกระทู้และเขียนความเห็นได้ — ผ่าน REST API ตาม ARCHITECTURE §7
3. มีหน้าเว็บ (Presentation Layer) ที่ใช้ API ข้างบนได้ครบ

ไม่ทำใน spec นี้: Reply, Tag, Room (Optional — spec แยกเมื่อจะทำ), การแก้ไข/ลบ, pagination

## 2. การตัดสินใจ

| # | เรื่อง | ตัดสิน | ที่มา |
| --- | --- | --- | --- |
| D1 | โครงสร้าง solution | `src/Webboard.Api` (Domain), `src/Webboard.DataAccess` (Data Access), `tests/Webboard.Api.Tests` — Api อ้าง DataAccess ทางเดียว | ADR-layered-architecture D2 |
| D2 | Primary key | `int` identity ทุกตาราง | ง่ายพอสำหรับขนาดนี้; URL อ่านง่าย |
| D3 | ชื่อใน DB | snake_case ผ่าน `EFCore.NamingConventions`; ตารางผู้ใช้ชื่อ `app_user` | TECHSTACK §2 |
| D4 | เวลา | `DateTime` แบบ UTC → `timestamptz`; Service ตั้ง `CreatedAt = DateTime.UtcNow` เสมอ | NFR-002 |
| D5 | Response | `record` DTO เท่านั้น (ดู §3) | ADR-layered-architecture D4 |
| D6 | Error | `AddProblemDetails()` + `[ApiController]` → 400 มี `errors` ตาม field | ARCHITECTURE §7 |
| D7 | Test | Integration test ยิง HTTP ผ่าน `WebApplicationFactory` เข้า PostgreSQL จริง (ฐาน `webboard_test`) ไม่ใช้ EF InMemory | InMemory ไม่บังคับ FK/constraint จึงซ่อน bug |
| D8 | ลำดับ | Topic ใหม่ → เก่า; Comment เก่า → ใหม่ | FR-TOPIC-002, FR-COMMENT-002 |
| D9 | ลบ Topic | `ON DELETE CASCADE` ไป comment (แม้ยังไม่มีการลบ) | ARCHITECTURE §6 |
| D10 | Dev database | `compose.yaml` รัน `postgres:17`; password มาจาก `.env` (gitignored) มี `.env.example` ให้ copy | NFR-004 |
| D11 | Test isolation | Integration test ทุก class อยู่ใน `[Collection("Database")]` (`ICollectionFixture<WebboardApiFactory>`) จึงไม่รันขนานบนฐานเดียวกัน; `WebboardApiFactory.ResetDatabaseAsync()` สั่ง `TRUNCATE … RESTART IDENTITY CASCADE` ทุกตารางใน EF model (ยกเว้น `__EFMigrationsHistory`) และ test class เรียกใน `IAsyncLifetime.InitializeAsync` | D7 — ฐาน `webboard_test` ใช้ร่วมกัน ถ้ารันขนานหรือไม่ล้างข้อมูล test จะ flaky |
| D12 | Test configuration | Secret ที่ test ต้องตั้งเองมีตัวเดียวคือ `ConnectionStrings:WebboardTest`; `WebboardApiFactory` ใช้ environment `Testing` และใส่ `Jwt:SigningKey` แบบสุ่มต่อ test run กับ Data Protection แบบ ephemeral ให้เอง; ถ้าไม่พบ `ConnectionStrings:WebboardTest` ให้ throw พร้อมข้อความที่บอกชื่อ key | NFR-004 — test ต้องรันได้ทั้งบนเครื่องที่ clone ใหม่และใน CI โดยไม่มี secret ใน repo |
| D13 | `parent_comment_id` | สร้างคอลัมน์ `comment.parent_comment_id` (nullable, FK → `comment(id)`, `ON DELETE CASCADE`) ใน migration ของ MVP เลย แต่ API ของ MVP ไม่รับค่านี้ — เป็น `null` เสมอ; spec ของ Reply ภายหลังเพิ่มแค่ฟิลด์ใน request และ validation | CLAUDE.md กฎข้อ 5, CONTEXT.md — `CommentDto` มี `ParentCommentId` อยู่แล้ว และไม่ต้อง migrate ตารางที่มีข้อมูลทีหลัง |
| D14 | Pagination | ไม่มีใน MVP — `GET /api/topics` คืน array ทั้งหมด ถ้าจะเพิ่มต้องเพิ่ม FR และ spec แยก (เปลี่ยนรูป response จึงเป็น breaking change) | ARCHITECTURE §8 จัด pagination เป็น Optional; ปิด Q2 |

## 3. Contracts

```csharp
// Webboard.Api/Dtos
public record TopicSummaryDto(int Id, string Title, string AuthorName, DateTime CreatedAt, int CommentCount);
public record TopicDetailDto(int Id, string Title, string Body, string AuthorName, DateTime CreatedAt, IReadOnlyList<CommentDto> Comments);
public record CommentDto(int Id, int TopicId, int? ParentCommentId, string AuthorName, string Body, DateTime CreatedAt);

public record CreateTopicRequest([Required, MaxLength(200)] string Title, [Required] string Body);
public record CreateCommentRequest([Required] string Body);
```

| Endpoint | Token | สำเร็จ | ผิดพลาด |
| --- | --- | --- | --- |
| `GET /api/topics` | — | `200 TopicSummaryDto[]` | — |
| `GET /api/topics/{id}` | — | `200 TopicDetailDto` | `404` |
| `POST /api/topics` | Access | `201 TopicSummaryDto` + `Location` | `400`, `401` |
| `POST /api/topics/{id}/comments` | Access | `201 CommentDto` + `Location` | `400`, `401`, `404` |

`AuthorName` มาจาก `app_user.display_name` ของ `UserId` ที่อ่านจาก claim `sub` — request ไม่มีฟิลด์ผู้เขียน

Configuration:

| Key | ที่อยู่ | ตัวอย่าง |
| --- | --- | --- |
| `ConnectionStrings:Webboard` | user-secrets (dev), env var (deploy) | `Host=localhost;Port=5432;Database=webboard;Username=webboard;Password=…` |
| `ConnectionStrings:WebboardTest` | user-secrets ของ test project (dev), env var `ConnectionStrings__WebboardTest` (CI) | `…;Database=webboard_test;…` |

`Jwt:SigningKey` และ `DataProtection:KeysPath` ไม่ต้องตั้งตอนรัน test — factory ใส่ค่าให้ (D12)

## 4. Acceptance

- [ ] `dotnet build -warnaserror` และ `dotnet test` ผ่านบนเครื่องที่ clone ใหม่ หลังทำตาม README Quick start
- [ ] `dotnet ef database update` สร้างตาราง `app_user`, `topic`, `comment` ที่มี FK และ index ตาม ARCHITECTURE §6
- [ ] `GET /api/topics` ไม่ต้องมี token และเรียงใหม่ → เก่า พร้อม `commentCount` ถูกต้อง
- [ ] `POST /api/topics` ไม่มี token → `401`; มี access token แต่ Title ว่าง → `400` มี `errors.Title`; ถูกต้อง → `201` และ `authorName` = ผู้ใช้ใน token
- [ ] `POST /api/topics/999/comments` พร้อม token → `404`
- [ ] หน้าเว็บ: ดูรายการ, เปิดกระทู้, login (รวม 2FA), ตั้งกระทู้, เขียนความเห็น ได้ครบโดยไม่เรียกอะไรนอกจาก `/api/...`

## 5. Open questions

- Q1 — Presentation Layer ใช้เทคโนโลยีอะไร → [TASK-presentation-tech-decision-01](../to-do/backlog/TASK-presentation-tech-decision-01.md) (MVP-06 รอข้อนี้)
- ~~Q2 — ต้องมี pagination ของรายการกระทู้หรือไม่~~ → ตัดสินแล้วเป็น D14 (ไม่มีใน MVP)
