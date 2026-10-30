# Project_SASD — Webboard App

เว็บบอร์ดสำหรับตั้งกระทู้และแสดงความคิดเห็น (แนว Reddit, Pantip) — โจทย์ C ของรายวิชา SASD

สถาปัตยกรรมแบบ 3 ชั้น (layered):

| Layer | Technology |
| --- | --- |
| Presentation Layer | ยังไม่กำหนด (เลือกได้อิสระ) — ดู [docs/TECHSTACK.md](docs/TECHSTACK.md) |
| Domain Layer | ASP.NET Core (REST API) |
| Data Access Layer | Entity Framework Core |
| Database | PostgreSQL |

## Requirement ขั้นต่ำ

- ตั้งกระทู้ (topic) ได้
- เขียนความเห็น (comment) ได้
- สมัครสมาชิก / login ด้วยรหัสผ่าน + 2FA (TOTP) — รหัสผ่านเก็บเป็น PBKDF2-HMAC-SHA256 ([เหตุผล](docs/architecture/decisions/ADR-auth-password-2fa.md))

ส่วนเสริม (ถ้ามีเวลา): comment ย่อยใน comment, tag ให้กระทู้, แยกห้อง (room)

## Quick start

> ยังไม่มีโค้ด — ส่วนนี้จะเติมเมื่อสร้าง solution แล้ว (คำสั่ง `dotnet run`, การตั้งค่า connection string, `dotnet ef database update`)

## Documents

| คำถาม | อ่านที่ |
| --- | --- |
| ระบบต้องทำอะไร / ขอบเขต / requirement | [docs/SPECREQUIREMENT.md](docs/SPECREQUIREMENT.md) |
| โครงสร้างระบบ / layer / data model / API | [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) |
| เทคโนโลยีและเวอร์ชันที่ใช้ | [docs/TECHSTACK.md](docs/TECHSTACK.md) |
| ศัพท์ของโดเมน (Topic, Comment, Room …) | [CONTEXT.md](CONTEXT.md) |
| Branch / commit / PR | [CONTRIBUTING.md](CONTRIBUTING.md) |
| สมาชิกทีม | [CONTRIBUTORS.md](CONTRIBUTORS.md) |
| งานที่ยังเปิดอยู่ / ลำดับงาน / ใครทำ | [task-plans/to-do/](task-plans/to-do/README.md) |
| Design spec ต่อฟีเจอร์ | [task-plans/spec/](task-plans/spec/) |
| เหตุผลของการตัดสินใจเชิงสถาปัตยกรรม | [docs/architecture/decisions/](docs/architecture/decisions/) |
| Mock แบบคลิกได้ ดูว่า request วิ่งผ่าน 3 layer อย่างไร | [docs/mockups/webboard-layers.html](docs/mockups/webboard-layers.html) (เปิดในเบราว์เซอร์ได้เลย) |
