# TASK-MVP-06 — หน้าเว็บ: รายการ, กระทู้, login + 2FA, ตั้งกระทู้, ความเห็น

**Status:** BLOCKED: รอ [TASK-presentation-tech-decision-01](../backlog/TASK-presentation-tech-decision-01.md) เลือกเทคโนโลยี
**Owner:** —
**Depends on:** MVP-05, AUTH-03
**Spec:** `task-plans/spec/spec-webboard-mvp.md` §1.3, §4; `docs/ARCHITECTURE.md` §9.1 (แถว Presentation)
**Produces:** `src/webboard-web/` ที่รันคู่กับ API ได้

## Goal

ผู้ใช้ทำทุกอย่างใน Acceptance ของทั้งสอง spec ผ่านหน้าเว็บได้ โดยหน้าเว็บเรียกแค่ `/api/...` ใช้ [docs/mockups/webboard-layers.html](../../../docs/mockups/webboard-layers.html) เป็นแบบของหน้าจอและ flow

## Exact files

ระบุหลังเลือกเทคโนโลยีแล้ว — อย่างน้อยต้องมี:

- หน้า: รายการกระทู้ (`/`), กระทู้ (`/topics/{id}`), สมัคร (`/register`), login ขั้น 1 (`/login`), ขั้น 2 + ตั้ง 2FA พร้อม QR (`/login/2fa`)
- API client ที่ใส่ `Authorization: Bearer` เมื่อมี access token และลบ token เมื่อได้ `401`
- `src/Webboard.Api/Program.cs` — CORS ให้ origin ของหน้าเว็บตอน dev (อนุญาต header `Authorization`)

## Steps

- [ ] Unblock: ทำ TASK-presentation-tech-decision-01 และเติม Exact files ด้านบน
- [ ] เขียน test ของ API client (หรือ e2e ตามเครื่องมือที่เลือก) ที่ fail: ไม่มี token แล้วไม่แสดงฟอร์มตั้งกระทู้; ได้ `401` แล้วกลับไปหน้า login
- [ ] Implement หน้าต่างๆ; แสดงข้อความผู้ใช้แบบ escape เสมอ (NFR-003); เก็บ token ในหน่วยความจำ ไม่ใส่ใน URL
- [ ] เดินตาม Acceptance ของทั้งสอง spec ด้วยมือในเบราว์เซอร์ บันทึกผลเป็น receipt

## Verification

- เดิน flow: สมัคร → ตั้ง 2FA ด้วยแอป Authenticator จริง → ตั้งกระทู้ → เขียนความเห็น → logout → เห็นว่าฟอร์มหายไป
- ใส่ `<script>alert(1)</script>` เป็นหัวข้อกระทู้ → แสดงเป็นข้อความ ไม่รัน
- DevTools Network: ทุก request ไปที่ `/api/...` เท่านั้น

## Commit

`feat(ui): add webboard pages for topics, comments and login`

Stage: `src/webboard-web/ src/Webboard.Api/Program.cs`
