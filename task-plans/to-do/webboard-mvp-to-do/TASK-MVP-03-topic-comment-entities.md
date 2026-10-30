# TASK-MVP-03 — Entity `Topic`, `Comment` + migration

**Status:** TODO
**Owner:** —
**Depends on:** MVP-02, AUTH-01 (`User` ต้องมีก่อนเพราะ FK `user_id`)
**Spec:** `task-plans/spec/spec-webboard-mvp.md` §2 D2–D4, D9; `docs/ARCHITECTURE.md` §6
**Produces:** `DbSet<Topic> Topics`, `DbSet<Comment> Comments`, migration `AddTopicAndComment`

## Goal

ตาราง `topic` และ `comment` อยู่ใน PostgreSQL ตาม Data Model โดยมี FK, NOT NULL และ cascade ที่ DB บังคับเอง

## Exact files

- Create `src/Webboard.DataAccess/Entities/Topic.cs` — `Id`, `UserId`, `Author`, `Title`, `Body`, `CreatedAt`, `Comments`
- Create `src/Webboard.DataAccess/Entities/Comment.cs` — `Id`, `TopicId`, `Topic`, `UserId`, `Author`, `ParentCommentId` (nullable, เผื่อ Reply), `Body`, `CreatedAt`
- Create `src/Webboard.DataAccess/Configurations/TopicConfiguration.cs`, `CommentConfiguration.cs` — `Title` max 200, required ทุกฟิลด์ข้อความ, cascade topic → comment, index `topic(created_at)`, `comment(topic_id, created_at)`
- Modify `src/Webboard.DataAccess/WebboardDbContext.cs` — `DbSet` + `ApplyConfigurationsFromAssembly`
- Create `src/Webboard.DataAccess/Migrations/<timestamp>_AddTopicAndComment.cs` (สร้างด้วย `dotnet ef`)
- Create `tests/Webboard.Api.Tests/TopicCommentSchemaTests.cs`

## Steps

- [ ] เขียน test ที่ fail: insert `Comment` ที่ `TopicId` ไม่มีจริง → `DbUpdateException` (FK); ลบ `Topic` → comment หายด้วย; `Title` 201 ตัว → `DbUpdateException`
- [ ] รันให้แดง
- [ ] เพิ่ม entity + configuration แล้ว `dotnet ef migrations add AddTopicAndComment --project src/Webboard.DataAccess --startup-project src/Webboard.Api`
- [ ] อ่าน migration ที่ได้: ชื่อตาราง/คอลัมน์เป็น snake_case, มี `ON DELETE CASCADE`
- [ ] `dotnet ef database update` แล้วรัน test ให้เขียว + quality gates

## Verification

```sh
dotnet ef database update --project src/Webboard.DataAccess --startup-project src/Webboard.Api
docker compose exec db psql -U webboard -c '\d comment'   # FK topic_id → topic(id) ON DELETE CASCADE, FK user_id → app_user(id)
dotnet test                                                # Passed! - Failed: 0
```

## Commit

`feat(db): add topic and comment tables`

Stage: `src/Webboard.DataAccess/ tests/Webboard.Api.Tests/TopicCommentSchemaTests.cs`
