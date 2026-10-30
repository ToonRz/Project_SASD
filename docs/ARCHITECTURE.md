# Webboard — Architecture

เอกสารนี้อธิบายว่าระบบแบ่งเป็นส่วนไหน แต่ละส่วนรับผิดชอบอะไร และข้อมูลไหลผ่านอย่างไร
อ่านคู่กับ [SPECREQUIREMENT.md](SPECREQUIREMENT.md) (ต้องทำอะไร) และ [TECHSTACK.md](TECHSTACK.md) (ใช้อะไรทำ)
เหตุผลที่เลือกโครงสร้างนี้อยู่ใน [ADR-layered-architecture.md](architecture/decisions/ADR-layered-architecture.md)

## Contents

1. [ภาพรวม](#1-ภาพรวม)
2. [Layer และหน้าที่](#2-layer-และหน้าที่)
3. [กฎการพึ่งพา (Dependency Rules)](#3-กฎการพึ่งพา-dependency-rules)
4. [โครงสร้าง Repository](#4-โครงสร้าง-repository)
5. [Request Flow](#5-request-flow)
6. [Data Model](#6-data-model)
7. [REST API](#7-rest-api)
8. [Feature Pillars (MVP / Optional)](#8-feature-pillars-mvp--optional)

---

## 1. ภาพรวม

```
┌─────────────────────────────┐
│  Presentation Layer         │   UI (เลือกเทคโนโลยีได้อิสระ)
└──────────────┬──────────────┘
               │  HTTP + JSON (REST)
┌──────────────▼──────────────┐
│  Domain Layer               │   ASP.NET Core Web API
│  Controllers → Services     │   validation + business rules
└──────────────┬──────────────┘
               │  method call (C#)
┌──────────────▼──────────────┐
│  Data Access Layer          │   Entity Framework Core
│  DbContext / Entities       │   mapping + migrations
└──────────────┬──────────────┘
               │  SQL (Npgsql)
┌──────────────▼──────────────┐
│  Database: PostgreSQL       │
└─────────────────────────────┘
```

## 2. Layer และหน้าที่

| Layer | ทำ | ไม่ทำ |
| --- | --- | --- |
| **Presentation** | แสดงผล, รับ input, เรียก REST API, escape ข้อความก่อนแสดง | ไม่ต่อฐานข้อมูล, ไม่เก็บ business rule ที่ต้องเชื่อถือได้ (validation ฝั่ง UI เป็นแค่ UX) |
| **Domain** (ASP.NET) | กำหนด endpoint, ตรวจ input, บังคับ business rule (เช่น Reply ต้องอยู่ Topic เดียวกับแม่), แปลง Entity ↔ DTO, คืน HTTP status ที่ถูกต้อง | ไม่เขียน SQL เอง, ไม่ส่ง Entity ของ EF ออกไปเป็น response ตรงๆ |
| **Data Access** (EF Core) | `DbContext`, Entity class, การ map ตาราง (Fluent API), Migrations | ไม่มี business rule, ไม่รู้จัก HTTP |
| **Database** | เก็บข้อมูล, บังคับ constraint (FK, NOT NULL, unique) | — |

## 3. กฎการพึ่งพา (Dependency Rules)

ทิศทางเดียวจากบนลงล่าง — layer ล่างไม่รู้จัก layer บน

1. `Presentation → Domain` ผ่าน HTTP เท่านั้น (ไม่ share code)
2. `Webboard.Api` (Domain) อ้างอิง `Webboard.DataAccess` ได้; **ห้ามกลับทาง**
3. Controller เรียก Service; Service เรียก `DbContext` — Controller ไม่แตะ `DbContext` ตรง
4. Response ทุกตัวเป็น DTO (`record`) ไม่ใช่ Entity — กัน navigation property วนลูป และกันข้อมูลที่ไม่ควรออก
5. Constraint สำคัญต้องอยู่ทั้งใน Domain (ตอบ `400` ที่อ่านรู้เรื่อง) และใน Database (กันข้อมูลเสียถ้าโค้ดพลาด)

## 4. โครงสร้าง Repository

> โครงสร้างที่วางแผนไว้ — ปรับเมื่อสร้าง solution จริงและอัปเดตเอกสารนี้ใน PR เดียวกัน

```
Project_SASD/
  Webboard.sln
  src/
    Webboard.Api/            Domain Layer — ASP.NET Core Web API
      Controllers/           TopicsController, CommentsController
      Services/              TopicService, CommentService (business rules)
      Dtos/                  request/response records
      Program.cs             DI, routing, error handling
    Webboard.DataAccess/     Data Access Layer — EF Core
      Entities/              Topic, Comment (+ Tag, Room เมื่อทำ optional)
      Configurations/        IEntityTypeConfiguration<T> (Fluent API)
      Migrations/            สร้างด้วย dotnet ef เท่านั้น ห้ามแก้มือ
      WebboardDbContext.cs
    webboard-web/            Presentation Layer (เทคโนโลยีตามที่ทีมเลือก)
  tests/
    Webboard.Api.Tests/      unit + integration tests ของ Domain Layer
  docs/
```

## 5. Request Flow

ตัวอย่าง: เขียน Comment (FR-COMMENT-001)

1. **Presentation** ส่ง `POST /api/topics/42/comments` body `{ "authorName": "...", "body": "..." }`
2. **Controller** รับ request — model validation (`[Required]`, `[MaxLength]`) ไม่ผ่าน → `400` (FR-TOPIC-004 รูปแบบเดียวกัน)
3. **Service** เช็กว่า Topic 42 มีอยู่ — ไม่มี → `404` (FR-COMMENT-003)
4. **Service** สร้าง `Comment` entity, ตั้ง `CreatedAt = DateTime.UtcNow`, เรียก `SaveChangesAsync()`
5. **Data Access** (EF Core) แปลงเป็น `INSERT` ไป PostgreSQL
6. **Controller** แปลง entity เป็น `CommentDto` แล้วตอบ `201 Created` พร้อม header `Location`

## 6. Data Model

```mermaid
erDiagram
    ROOM  ||--o{ TOPIC   : "contains (optional)"
    TOPIC ||--o{ COMMENT : has
    COMMENT |o--o{ COMMENT : "replies to (optional)"
    TOPIC }o--o{ TAG     : "tagged with (optional)"

    TOPIC {
        int id PK
        string title "≤ 200, not null"
        string body "not null"
        string author_name "≤ 50, not null"
        timestamptz created_at "UTC"
        int room_id FK "nullable — optional"
    }
    COMMENT {
        int id PK
        int topic_id FK "not null"
        int parent_comment_id FK "nullable — optional (Reply)"
        string body "not null"
        string author_name "≤ 50, not null"
        timestamptz created_at "UTC"
    }
    TAG {
        int id PK
        string name "unique, lowercase"
    }
    ROOM {
        int id PK
        string name "unique"
    }
```

- **MVP** ใช้แค่ `TOPIC` และ `COMMENT` (ไม่มี `parent_comment_id`, `room_id`)
- **Reply** ไม่ใช่ตารางแยก — คือ `COMMENT` ที่มี `parent_comment_id` (ดู [CONTEXT.md](../CONTEXT.md))
- `TOPIC ↔ TAG` เป็น many-to-many ผ่านตารางเชื่อม `topic_tag` (EF Core สร้างให้อัตโนมัติ)
- ลบ Topic → ลบ Comment ตาม (`ON DELETE CASCADE`) — แม้ MVP จะยังไม่มีการลบ ก็ตั้งไว้ให้ข้อมูลไม่ค้าง

## 7. REST API

Base path: `/api` · รูปแบบ JSON แบบ camelCase · Swagger UI ที่ `/swagger` ตอน dev

| Method | Path | FR | ผลลัพธ์ |
| --- | --- | --- | --- |
| `GET` | `/api/topics` | FR-TOPIC-002 | `200` รายการ Topic (ใหม่ → เก่า) |
| `POST` | `/api/topics` | FR-TOPIC-001 | `201` Topic ที่สร้าง · `400` ถ้า input ผิด |
| `GET` | `/api/topics/{id}` | FR-TOPIC-003 | `200` Topic + Comments · `404` |
| `POST` | `/api/topics/{id}/comments` | FR-COMMENT-001 | `201` Comment · `400` · `404` |
| `POST` | `/api/topics/{id}/comments` + `parentCommentId` | FR-REPLY-001 | *(optional)* `201` · `400` ถ้าแม่อยู่คนละ Topic |
| `GET` | `/api/topics?tag={name}` | FR-TAG-002 | *(optional)* |
| `GET` | `/api/rooms` | FR-ROOM-001 | *(optional)* |
| `GET` | `/api/rooms/{id}/topics` | FR-ROOM-003 | *(optional)* |

**Error contract:** ทุก error ตอบเป็น [RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457) (`application/problem+json`) ซึ่ง ASP.NET Core สร้างให้ผ่าน `AddProblemDetails()` — validation error มี `errors` แยกตาม field

## 8. Feature Pillars (MVP / Optional)

แต่ละ pillar แบ่ง 2 ระดับ:
- **MVP** — ต้องมีจึงจะผ่าน requirement ขั้นต่ำ
- **Optional** — ทำเพิ่มเมื่อ MVP เสร็จและผ่านการทดสอบแล้ว

### 8.1 Topic
- **MVP** — สร้าง, ดูรายการ, ดูรายละเอียด
- **Optional** — แบ่งหน้า (pagination), ค้นหาด้วยคำใน Title, กรองตาม Tag/Room

### 8.2 Comment
- **MVP** — เขียน Comment ใน Topic, แสดงเรียงตามเวลา
- **Optional** — Reply ซ้อนกัน (แนะนำจำกัดความลึกที่ UI เช่น 3 ชั้น เพื่ออ่านง่าย)

### 8.3 Organization (Tag / Room)
- **Optional** — Tag หลายอันต่อ Topic, Room หนึ่งอันต่อ Topic

### 8.4 อื่นๆ (ยังไม่อยู่ในขอบเขต — ดู SPECREQUIREMENT §2.3)
- บัญชีผู้ใช้ / login, แก้ไข-ลบโพสต์, โหวต/like, real-time update
