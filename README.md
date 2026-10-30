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
| เหตุผลของการตัดสินใจเชิงสถาปัตยกรรม | [docs/architecture/decisions/](docs/architecture/decisions/) |
