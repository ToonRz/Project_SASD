# TASK-MVP-04 — `GET/POST /api/topics`, `GET /api/topics/{id}`

**Status:** TODO
**Owner:** —
**Depends on:** MVP-03, AUTH-03 (JWT bearer + access-token policy, `ClaimsPrincipalExtensions.GetUserId()`, `TestAuth.GetAccessTokenAsync`)
**Spec:** `task-plans/spec/spec-webboard-mvp.md` §2 D14, §3; FR-TOPIC-001…004, FR-AUTH-007
**Produces:** `TopicService`, `TopicsController`, DTO `TopicSummaryDto`, `TopicDetailDto`, `CommentDto`

## Goal

Reader ดูรายการและรายละเอียดกระทู้ได้โดยไม่ login ส่วน Member ที่มี access token ตั้งกระทู้ได้ โดยผู้เขียนมาจาก token

## Exact files

- Create `src/Webboard.Api/Dtos/TopicDtos.cs`, `CommentDto.cs`
- Create `src/Webboard.Api/Services/TopicService.cs` — `ListAsync`, `GetAsync(id)`, `CreateAsync(userId, request)`
- Create `src/Webboard.Api/Controllers/TopicsController.cs` — `[Authorize]` เฉพาะ `POST`; ผู้เขียนจาก `User.GetUserId()` ของ AUTH-03 (ไม่สร้าง helper ใหม่)
- Modify `src/Webboard.Api/Program.cs` — register `TopicService`
- Create `tests/Webboard.Api.Tests/TopicsApiTests.cs` — `[Collection("Database")]`, ขอ access token ด้วย `TestAuth.GetAccessTokenAsync` จาก AUTH-03

## Steps

- [ ] เขียน test ที่ fail:
  - `GET /api/topics` → `200`, เรียงใหม่ → เก่า, `commentCount` ถูก
  - `GET /api/topics/999` → `404` problem+json
  - `POST /api/topics` ไม่มี token → `401`; ใช้ MFA token → `401`
  - `POST` ด้วย access token, `Title` ว่าง → `400` มี `errors.Title`; Title 201 ตัว → `400`
  - `POST` ถูกต้อง → `201`, header `Location` = `/api/topics/{id}`, `authorName` = display name ของผู้ใช้ใน token
  - `POST` ที่ส่ง `authorName` แปลกปลอมมาใน body → ถูกเพิกเฉย
- [ ] รันให้แดง
- [ ] Implement Service (ใช้ `Select` เป็น DTO ใน query, ไม่ `ToList()` ก่อน project) และ Controller
- [ ] รันให้เขียว + quality gates

## Verification

```sh
dotnet test --filter FullyQualifiedName~TopicsApiTests   # Passed! - Failed: 0
dotnet test                                               # Passed! - Failed: 0
```

## Commit

`feat(api): add topics endpoints`

Stage: `src/Webboard.Api/ tests/Webboard.Api.Tests/TopicsApiTests.cs`
