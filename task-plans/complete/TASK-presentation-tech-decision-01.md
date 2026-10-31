# TASK-presentation-tech-decision-01 — เลือกเทคโนโลยีของ Presentation Layer

**Status:** DONE
**Owner:** narawichswu
**Depends on:** —
**Spec:** `task-plans/spec/spec-webboard-mvp.md` §5 Q1; `docs/TECHSTACK.md` §4
**Produces:** ADR ที่ระบุเทคโนโลยี UI; ปลด BLOCKED ของ MVP-06

## Goal

ทีมตกลงว่า Presentation Layer ใช้อะไร (โจทย์ให้เลือกอิสระ) และบันทึกเหตุผลไว้ เพื่อให้ MVP-06 เริ่มได้

## Exact files

- Create `docs/architecture/decisions/ADR-presentation-technology.md` — ตัวเลือกที่พิจารณา, ที่เลือก, เหตุผล, ผลที่ตามมา (CORS, การรัน dev, QR library)
- Modify `docs/TECHSTACK.md` §4 — เติมตาราง Framework / HTTP client / Dev server / QR code
- Modify `task-plans/to-do/webboard-mvp-to-do/TASK-MVP-06-presentation.md` — เติม Exact files และเปลี่ยน Status เป็น `TODO`
- Modify `task-plans/to-do/webboard-mvp-to-do/tasks.md` — สถานะ MVP-06

## Steps

- [x] ทีมเทียบตัวเลือกด้วยเกณฑ์: คนในทีมเขียนเป็นหรือไม่, เรียก REST + Bearer token ได้ง่าย, มี library สร้าง QR, รันคู่กับ `dotnet run` ได้ง่าย
- [x] ตัวเลือกตั้งต้น (ไม่จำกัด): Blazor WebAssembly (C# ทั้งระบบ), React + Vite, HTML + JavaScript ล้วน (แบบ mock)
- [x] เขียน ADR และแก้ไฟล์ตาม Exact files

## Verification

- ADR มีสถานะ `Accepted` และชื่อคนที่ตัดสิน
- `TASK-MVP-06` ไม่มีคำว่า `BLOCKED` แล้ว

## Commit

`docs: decide presentation layer technology`

Stage: `docs/architecture/decisions/ADR-presentation-technology.md docs/TECHSTACK.md task-plans/to-do/webboard-mvp-to-do/`
