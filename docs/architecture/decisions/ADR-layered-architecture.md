# ADR: Three-Layer Architecture — Presentation / Domain (ASP.NET) / Data Access (EF Core)

**Status:** Accepted
**Date:** 2026-10-30
**Spec:** [`SPECREQUIREMENT.md`](../../SPECREQUIREMENT.md) §6

ADR นี้บันทึกว่าเส้นแบ่งระหว่าง layer อยู่ตรงไหนและทำไม เพื่อให้การเปลี่ยนที่ทำให้เส้นแบ่งหายไปในอนาคต
ถูกมองว่าเป็น "การเปลี่ยนสถาปัตยกรรม" ไม่ใช่แค่ refactor

---

## 1. Context

โจทย์กำหนดเทคโนโลยีแต่ละชั้นไว้: Presentation (อิสระ), Domain = ASP.NET REST API,
Data Access = Entity Framework, Database = PostgreSQL ทีมต้องตัดสินว่า code แต่ละส่วนอยู่ project ไหน
และ layer ไหนเรียกอะไรได้บ้าง

## 2. Decision 1 — Presentation คุยกับระบบผ่าน REST API เท่านั้น

UI ไม่ว่าจะเลือกเทคโนโลยีอะไร ต้องเรียก `/api/...` เท่านั้น ไม่ต่อ PostgreSQL ตรง และไม่ reference project C# ของ Domain

### Why

ทำให้ทีม UI เลือกเทคโนโลยีได้อิสระตามที่โจทย์อนุญาต และทำให้ business rule มีที่อยู่ที่เดียว (Domain)
ถ้า UI ต่อ DB ตรง rule เช่น "Reply ต้องอยู่ Topic เดียวกับแม่" จะต้องเขียนซ้ำสองที่

## 3. Decision 2 — แยก Domain และ Data Access เป็นสอง project

`Webboard.Api` (Domain) reference `Webboard.DataAccess` ทางเดียว Entity และ `DbContext` อยู่ใน DataAccess

### Why

compiler บังคับทิศทางให้: DataAccess ไม่สามารถเรียก Controller หรือรู้จัก HTTP ได้เพราะไม่มี reference
ถ้าอยู่ project เดียว เส้นแบ่งเป็นแค่ชื่อโฟลเดอร์ที่ไม่มีอะไรกัน

## 4. Decision 3 — ไม่ทำ Repository pattern ครอบ EF Core

Service ใน Domain ใช้ `WebboardDbContext` ตรง (ผ่าน DI) ไม่สร้าง `ITopicRepository` ครอบอีกชั้น

### Why

`DbContext` เป็น Unit of Work และ `DbSet<T>` เป็น repository อยู่แล้ว การครอบอีกชั้นเพิ่ม interface
และ class ที่ไม่ได้ซ่อนอะไรสำหรับขนาดโปรเจกต์นี้ การ test ใช้ PostgreSQL จริงหรือ test container แทนการ mock repository

### Consequences

- Service ผูกกับ EF Core — ยอมรับได้เพราะโจทย์บังคับ EF อยู่แล้ว
- ถ้าภายหลังมี query ซับซ้อนที่ใช้ซ้ำหลาย Service ค่อยแยกเป็น query class ใน DataAccess ตอนนั้น

## 5. Decision 4 — Response เป็น DTO ไม่ใช่ Entity

Controller คืน `record` DTO เสมอ

### Why

Entity มี navigation property (`Topic.Comments`, `Comment.Parent`) ที่ทำให้ serializer วนลูป
และทำให้ schema ของ DB กลายเป็นสัญญาของ API — เปลี่ยนชื่อ column ทีเดียว UI พัง
