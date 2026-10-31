# TASK-MVP-03 — Entity `Topic`, `Comment` + migration

**Status:** DONE
**Owner:** narawichswu (Claude)
**Depends on:** MVP-02, AUTH-01 (`User` ต้องมีก่อนเพราะ FK `user_id`)
**Spec:** `task-plans/spec/spec-webboard-mvp.md` §2 D2–D4, D9, D11, D13; `docs/ARCHITECTURE.md` §6
**Produces:** `DbSet<Topic> Topics`, `DbSet<Comment> Comments`, migration `AddTopicAndComment`

## Goal

ตาราง `topic` และ `comment` อยู่ใน PostgreSQL ตาม Data Model โดยมี FK, NOT NULL และ cascade ที่ DB บังคับเอง

## Exact files

- Create `src/Webboard.DataAccess/Entities/Topic.cs` — `Id`, `UserId`, `Author`, `Title`, `Body`, `CreatedAt`, `Comments`
- Create `src/Webboard.DataAccess/Entities/Comment.cs` — `Id`, `TopicId`, `Topic`, `UserId`, `Author`, `ParentCommentId` (nullable), `ParentComment`, `Body`, `CreatedAt` — `ParentCommentId` มีตั้งแต่ตอนนี้ตาม D13 แต่ MVP เป็น `null` เสมอ
- Create `src/Webboard.DataAccess/Configurations/TopicConfiguration.cs`, `CommentConfiguration.cs` — `Title` max 200, required ทุกฟิลด์ข้อความ, cascade topic → comment, self-FK `parent_comment_id` → `comment(id)` cascade, index `topic(created_at)`, `comment(topic_id, created_at)`
- Modify `src/Webboard.DataAccess/WebboardDbContext.cs` — `DbSet` + `ApplyConfigurationsFromAssembly`
- Create `src/Webboard.DataAccess/Migrations/<timestamp>_AddTopicAndComment.cs` (สร้างด้วย `dotnet ef`)
- Create `tests/Webboard.Api.Tests/TopicCommentSchemaTests.cs` — `[Collection("Database")]`

## Steps

- [ ] เขียน test ที่ fail: insert `Comment` ที่ `TopicId` ไม่มีจริง → `DbUpdateException` (FK); `ParentCommentId` ไม่มีจริง → `DbUpdateException`; ลบ `Topic` → comment หายด้วย; `Title` 201 ตัว → `DbUpdateException`
- [ ] รันให้แดง
- [ ] เพิ่ม entity + configuration แล้ว `dotnet ef migrations add AddTopicAndComment --project src/Webboard.DataAccess --startup-project src/Webboard.Api`
- [ ] อ่าน migration ที่ได้: ชื่อตาราง/คอลัมน์เป็น snake_case, มี `ON DELETE CASCADE`
- [ ] `dotnet ef database update` แล้วรัน test ให้เขียว + quality gates

## Verification

```sh
dotnet ef database update --project src/Webboard.DataAccess --startup-project src/Webboard.Api
docker compose exec db psql -U webboard -c '\d comment'   # FK topic_id → topic(id) ON DELETE CASCADE, FK user_id → app_user(id), FK parent_comment_id → comment(id) ON DELETE CASCADE
dotnet test                                                # Passed! - Failed: 0
```

## Commit

`feat(db): add topic and comment tables`

Stage: `src/Webboard.DataAccess/ tests/Webboard.Api.Tests/TopicCommentSchemaTests.cs`
