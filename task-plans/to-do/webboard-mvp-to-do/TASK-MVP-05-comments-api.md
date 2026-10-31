# TASK-MVP-05 — `POST /api/topics/{id}/comments`

**Status:** TODO
**Owner:** —
**Depends on:** MVP-04
**Spec:** `task-plans/spec/spec-webboard-mvp.md` §3; FR-COMMENT-001…003
**Produces:** `CommentService`, `CommentsController`; `GET /api/topics/{id}` คืน comments เรียงเก่า → ใหม่

## Goal

Member ที่มี access token เขียนความเห็นในกระทู้ที่มีอยู่ได้ และความเห็นแสดงในรายละเอียดกระทู้ตามลำดับเวลา

## Exact files

- Create `src/Webboard.Api/Services/CommentService.cs` — `CreateAsync(userId, topicId, request)`
- Create `src/Webboard.Api/Controllers/CommentsController.cs` — route `api/topics/{topicId:int}/comments`, `[Authorize]`
- Create `src/Webboard.Api/Dtos/CreateCommentRequest.cs`
- Modify `src/Webboard.Api/Program.cs` — register `CommentService`
- Create `tests/Webboard.Api.Tests/CommentsApiTests.cs` — `[Collection("Database")]`

## Steps

- [ ] เขียน test ที่ fail:
  - ไม่มี token → `401`
  - Topic 999 → `404`
  - `Body` ว่าง → `400` มี `errors.Body`
  - ถูกต้อง → `201`, `authorName` จาก token, `parentCommentId` เป็น `null`
  - เขียนสองความเห็นแล้ว `GET /api/topics/{id}` คืนเรียงเก่า → ใหม่ และ `commentCount` ใน `GET /api/topics` = 2
- [ ] รันให้แดง
- [ ] Implement: Service เช็ก `Topics.AnyAsync` ก่อน insert
- [ ] รันให้เขียว + quality gates

## Verification

```sh
dotnet test --filter FullyQualifiedName~CommentsApiTests   # Passed! - Failed: 0
dotnet test                                                 # Passed! - Failed: 0
```

## Commit

`feat(api): add comments endpoint`

Stage: `src/Webboard.Api/ tests/Webboard.Api.Tests/CommentsApiTests.cs`
