# Webboard — Specification & Requirements

> เอกสารนี้บอกว่า **ระบบต้องทำอะไร** ส่วน **โครงสร้างระบบ** อยู่ใน [ARCHITECTURE.md](ARCHITECTURE.md)
> และ **เทคโนโลยีที่ใช้** อยู่ใน [TECHSTACK.md](TECHSTACK.md) ศัพท์ทั้งหมดใช้ตาม [CONTEXT.md](../CONTEXT.md)

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Scope and Goals](#2-scope-and-goals)
3. [Personas](#3-personas)
4. [Functional Requirements](#4-functional-requirements)
5. [Non-Functional Requirements](#5-non-functional-requirements)
6. [Technology Constraints](#6-technology-constraints)
7. [Change Records](#7-change-records)

---

## 1. Project Overview

Webboard App (โจทย์ C) — เว็บบอร์ดให้ผู้ใช้ตั้งกระทู้และแสดงความคิดเห็น ตัวอย่างอ้างอิง: Reddit, Pantip

## 2. Scope and Goals

### 2.1 In Scope (MVP — Requirement ขั้นต่ำ)

- ตั้งกระทู้ (**Topic**) ได้
- เขียนความเห็น (**Comment**) ในกระทู้ได้
- สมัครสมาชิกและ login ด้วยรหัสผ่าน + 2FA (TOTP) — การตั้งกระทู้และเขียนความเห็นต้อง login

### 2.2 Optional (อาจเพิ่มเติม)

- ส่ง comment ย่อยภายใน comment (**Reply**)
- ใส่ **Tag** ให้กระทู้
- Grouping กระทู้ หรือการแยกห้อง (**Room**)
- อื่นๆ ตามที่ทีมออกแบบ — เพิ่มเป็น requirement ใหม่ในเอกสารนี้ก่อนลงมือทำ

### 2.3 Non-Goals (ตอนนี้)

- ลืมรหัสผ่าน / เปลี่ยนรหัสผ่าน / ยืนยันอีเมล
- Recovery code ของ 2FA (ทำมือถือหาย = ติดต่อผู้ดูแลให้ reset)
- Refresh token, login ด้วย Google/Facebook
- การแก้ไข / ลบ กระทู้และความเห็น
- Real-time update (WebSocket) — หน้าเว็บโหลดใหม่เพื่อดูความเห็นล่าสุด

ถ้าจะทำข้อใดข้างบน ให้ย้ายขึ้นไปที่ §2.2 พร้อมเพิ่ม FR และ Change Record

## 3. Personas

| Persona | ต้องการ |
| --- | --- |
| ผู้อ่าน (Reader) — ไม่ได้ login | ดูรายการกระทู้, เปิดอ่านกระทู้และความเห็น |
| สมาชิก (Member) — login แล้ว | ทุกอย่างของผู้อ่าน + ตั้งกระทู้, ตอบกระทู้, ตอบความเห็น |

## 4. Functional Requirements

รหัส requirement ใช้รูปแบบ `FR-<AREA>-<NNN>` เพื่ออ้างอิงใน issue, PR และ test

### 4.1 Topic (FR-TOPIC) — MVP

| ID | Requirement |
| --- | --- |
| FR-TOPIC-001 | สมาชิกที่ login แล้วสร้าง Topic ได้ โดยกรอก `Title` (บังคับ, ≤ 200 ตัวอักษร) และ `Body` (บังคับ); **Author** คือผู้ใช้เจ้าของ access token (ไม่รับชื่อผู้เขียนจาก request) |
| FR-TOPIC-002 | ดูรายการ Topic ทั้งหมดได้โดยไม่ต้อง login เรียงจากใหม่ไปเก่า แสดง Title, ชื่อที่แสดงของ Author, เวลาที่สร้าง และจำนวน Comment |
| FR-TOPIC-003 | เปิดดู Topic เดียวพร้อม Comment ทั้งหมดของ Topic นั้นได้ |
| FR-TOPIC-004 | ข้อมูลไม่ครบหรือยาวเกินกำหนด → API ตอบ `400` พร้อมบอก field ที่ผิด และไม่บันทึกอะไรลงฐานข้อมูล |

### 4.2 Comment (FR-COMMENT) — MVP

| ID | Requirement |
| --- | --- |
| FR-COMMENT-001 | สมาชิกที่ login แล้วเขียน Comment ใน Topic ได้ โดยกรอก `Body` (บังคับ); Author คือผู้ใช้เจ้าของ access token |
| FR-COMMENT-002 | Comment ใน Topic เรียงจากเก่าไปใหม่ |
| FR-COMMENT-003 | เขียน Comment ใน Topic ที่ไม่มีอยู่ → API ตอบ `404` |

### 4.3 Reply (FR-REPLY) — Optional

| ID | Requirement |
| --- | --- |
| FR-REPLY-001 | ผู้ใช้ตอบ Comment ได้ (สร้าง Comment ที่มี `ParentCommentId`) |
| FR-REPLY-002 | Reply ต้องอยู่ Topic เดียวกับ Comment แม่ ถ้าไม่ตรง → `400` |
| FR-REPLY-003 | หน้า Topic แสดง Reply ซ้อนอยู่ใต้ Comment แม่ |

### 4.4 Tag (FR-TAG) — Optional

| ID | Requirement |
| --- | --- |
| FR-TAG-001 | ใส่ Tag ได้หลายอันตอนสร้าง Topic; Tag ที่ยังไม่มีให้สร้างใหม่ (ชื่อ Tag ไม่สนตัวพิมพ์เล็ก/ใหญ่) |
| FR-TAG-002 | กรองรายการ Topic ตาม Tag ได้ |

### 4.5 Room (FR-ROOM) — Optional

| ID | Requirement |
| --- | --- |
| FR-ROOM-001 | ดูรายการ Room ได้ |
| FR-ROOM-002 | สร้าง Topic ใน Room ที่เลือก; Topic หนึ่งอยู่ได้ Room เดียว |
| FR-ROOM-003 | ดูรายการ Topic เฉพาะใน Room ได้ |

### 4.6 Authentication (FR-AUTH) — MVP

รายละเอียดและเหตุผลอยู่ใน [ADR-auth-password-2fa.md](architecture/decisions/ADR-auth-password-2fa.md)

| ID | Requirement |
| --- | --- |
| FR-AUTH-001 | สมัครสมาชิกได้ด้วย `Username` (บังคับ, 3–30 ตัว, `a-z 0-9 _`, ไม่ซ้ำแบบไม่สนตัวพิมพ์ — เก็บเป็นตัวเล็ก), `DisplayName` (บังคับ, ≤ 50 ตัวอักษร) และ `Password` (บังคับ, 8–128 ตัวอักษร); username ซ้ำ → `409` |
| FR-AUTH-002 | รหัสผ่านเก็บเป็น **PBKDF2-HMAC-SHA256** เท่านั้น: salt สุ่ม 16 bytes ต่อผู้ใช้, 600,000 รอบ, ผลลัพธ์ 32 bytes — ห้ามเก็บ plaintext, ห้ามเข้ารหัสแบบถอดกลับได้, ห้ามใช้ SHA-256 ครั้งเดียวแบบไม่มี salt |
| FR-AUTH-003 | Login ขั้นที่ 1: ส่ง `Username` + `Password` ถูก → ได้ **MFA token** (อายุ 5 นาที) พร้อมบอกว่าต้องตั้ง 2FA ก่อนหรือไม่; ผิด หรือไม่มี username นี้ → `401` ข้อความเดียวกัน |
| FR-AUTH-004 | 2FA บังคับทุกคน: ผู้ใช้ที่ยังไม่ตั้ง 2FA ขอ shared key + `otpauth://` URI ได้ด้วย MFA token (Presentation แสดงเป็น QR ให้สแกน) และ 2FA จะเปิดใช้เมื่อยืนยันรหัสแรกถูก |
| FR-AUTH-005 | Login ขั้นที่ 2: ส่งรหัส TOTP 6 หลักพร้อม MFA token ถูก → ได้ **Access token** (อายุ 60 นาที); ยอมรับรหัสคลาดเวลา ±30 วินาที และรหัสที่ใช้ไปแล้วใช้ซ้ำไม่ได้ |
| FR-AUTH-006 | ผิดติดกัน 5 ครั้ง (รหัสผ่านหรือรหัส 2FA) → บัญชีถูกล็อก 15 นาที ระหว่างนั้นตอบ `429` |
| FR-AUTH-007 | การสร้าง Topic / Comment ต้องมี access token ที่ถูกต้อง ไม่มีหรือหมดอายุ → `401`; MFA token ใช้แทน access token ไม่ได้ |
| FR-AUTH-008 | `GET /api/auth/me` คืนข้อมูลผู้ใช้ปัจจุบัน; logout = Presentation ลบ token ทิ้ง |

## 5. Non-Functional Requirements

| ID | Requirement |
| --- | --- |
| NFR-001 | Presentation Layer คุยกับระบบผ่าน REST API (JSON) เท่านั้น — ห้ามต่อฐานข้อมูลตรง |
| NFR-002 | เวลาเก็บเป็น UTC (`timestamptz`) แสดงผลเป็นเวลาท้องถิ่นที่ Presentation Layer |
| NFR-003 | ข้อความที่ผู้ใช้กรอกต้องแสดงผลแบบ escape (กัน XSS) และ query ทุกตัวผ่าน EF Core (กัน SQL injection) |
| NFR-004 | ห้ามมี secret (connection string จริง, password, JWT signing key, Data Protection key) ใน repository — ใช้ user-secrets หรือ environment variable |
| NFR-005 | Schema ของฐานข้อมูลเปลี่ยนผ่าน EF Core Migrations เท่านั้น |
| NFR-006 | รหัสผ่าน, รหัส 2FA และ token ห้ามปรากฏใน log, response (ยกเว้น token ที่ออกให้) หรือ error message |
| NFR-007 | Deploy นอกเครื่อง dev ต้องเป็น HTTPS เท่านั้น |

## 6. Technology Constraints

กำหนดโดยโจทย์ — เปลี่ยนไม่ได้:

| Layer | Technology |
| --- | --- |
| Presentation Layer | ใช้อะไรทำก็ได้ |
| Domain Layer | ASP.NET (REST API) |
| Data Access Layer | Entity Framework (EF) |
| Database | PostgreSQL |

## 7. Change Records

เมื่อแก้ requirement ที่ตกลงกันแล้ว ให้เพิ่มบันทึกใต้หัวข้อนี้ (ใหม่สุดอยู่บน):

```
#### Change Record — YYYY-MM-DD
- เปลี่ยนอะไร: FR-XXX-NNN ...
- เพราะอะไร: ...
```

#### Change Record — 2026-10-30
- เปลี่ยนอะไร: เพิ่ม FR-AUTH-001…008 และ NFR-006, NFR-007; FR-TOPIC-001 และ FR-COMMENT-001 ต้อง login และไม่รับ `AuthorName` จาก request แล้ว; ย้าย "บัญชีผู้ใช้ / login" ออกจาก Non-Goals
- เพราะอะไร: requirement ใหม่ให้มี login แบบ 2FA และเก็บรหัสผ่านด้วย SHA-256 — ใช้ PBKDF2-HMAC-SHA256 เพราะ SHA-256 ครั้งเดียวไม่ปลอดภัยพอ (ดู ADR-auth-password-2fa)
