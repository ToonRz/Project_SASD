# webboard-mvp — Topic และ Comment ผ่าน 3 layer

**Source spec:** [`task-plans/spec/spec-webboard-mvp.md`](../../spec/spec-webboard-mvp.md)
**Status of spec/board:** Approved 2026-10-31 (narawichswu)

## Goal

Solution .NET ที่ build/test ได้, PostgreSQL ที่สร้างด้วย EF Core Migrations, REST API ของ Topic/Comment และหน้าเว็บที่ใช้ API นั้น ตาม Acceptance ใน spec §4

## Global constraints

- กฎ layer ตาม [ARCHITECTURE §3](../../../docs/ARCHITECTURE.md#3-กฎการพึ่งพา-dependency-rules): Controller → Service → `DbContext`; response เป็น DTO
- ชื่อผู้เขียนมาจาก claim `sub` ของ access token เท่านั้น
- Integration test ใช้ PostgreSQL จริง (`webboard_test`) ไม่ใช้ EF InMemory
- Integration test ทุก class อยู่ใน `[Collection("Database")]` และเรียก `ResetDatabaseAsync()` ก่อนแต่ละ test ([spec](../../spec/spec-webboard-mvp.md) D11); secret ที่ test ต้องตั้งมีแค่ `ConnectionStrings:WebboardTest` (D12)
- Schema เปลี่ยนผ่าน `dotnet ef migrations add` เท่านั้น
- Stage เฉพาะ path ที่ task ระบุ ห้าม `git add -A`

## Task table

| Task | Title | Depends on | Owner | Status |
| --- | --- | --- | --- | --- |
| [MVP-01](TASK-MVP-01-solution-skeleton.md) | Solution, projects, OpenAPI, ProblemDetails, test project | — | — | TODO |
| [MVP-02](TASK-MVP-02-database-dbcontext.md) | PostgreSQL ผ่าน compose, `WebboardDbContext`, test fixture | MVP-01 | — | TODO |
| [MVP-03](TASK-MVP-03-topic-comment-entities.md) | Entity `Topic`, `Comment` + migration | MVP-02, AUTH-01 | — | TODO |
| [MVP-04](TASK-MVP-04-topics-api.md) | `GET/POST /api/topics`, `GET /api/topics/{id}` | MVP-03, AUTH-03 | — | TODO |
| [MVP-05](TASK-MVP-05-comments-api.md) | `POST /api/topics/{id}/comments` | MVP-04 | — | TODO |
| [MVP-06](TASK-MVP-06-presentation.md) | หน้าเว็บ: รายการ, กระทู้, login + 2FA, ตั้งกระทู้, ความเห็น | MVP-05, AUTH-03 | — | BLOCKED: รอ TASK-presentation-tech-decision-01 |

## Dependency order

```text
MVP-01 ─► MVP-02 ─┬─────────────► MVP-03 ─► MVP-04 ─► MVP-05 ─► MVP-06
                  │                  ▲          ▲                  ▲
                  └─► AUTH-01 ───────┘          │                  │
                         └─► AUTH-02 ─► AUTH-03 ┴──────────────────┘
```

AUTH-xx อยู่ใน [auth-login-2fa-to-do](../auth-login-2fa-to-do/tasks.md)

## Gates that outrank task status

- Controller เรียก `DbContext` ตรง หรือคืน Entity เป็น response → reject
- Request ของ Topic/Comment มีฟิลด์ผู้เขียน หรือ Service อ่านผู้เขียนจาก body → reject
- Test ผ่านเพราะใช้ EF InMemory แทน PostgreSQL → reject
- มี connection string ที่มี password ใน `appsettings*.json` หรือไฟล์ที่ commit → reject
- Migration ที่ merge แล้วถูกแก้ → reject

## Receipts

_(เพิ่มเมื่อ task เสร็จ: คำสั่งจริง + output จริง)_
