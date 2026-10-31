# task-plans/to-do — งานที่ยังเปิดอยู่ และวิธีแตกงาน

โฟลเดอร์นี้เก็บ **งานที่ยังไม่เสร็จเท่านั้น** งานที่เสร็จแล้วย้ายไป `../complete/` (ดู "Completion")
ก่อนเปิด PR ให้เช็กว่ามีไฟล์ TASK ของงานนั้นอยู่ที่นี่หรือยัง (ใน `backlog/` หรือ breakdown folder) ถ้าไม่มีให้เขียนใน PR เดียวกัน

```
task-plans/
  spec/                  # design spec ต่อฟีเจอร์: spec-<slug>.md
  to-do/
    README.md            # ไฟล์นี้
    backlog/             # งานเดี่ยว: TASK-<slug>-NN.md ไฟล์ละงาน
    <feature>-to-do/     # breakdown folder: tasks.md + TASK-<PFX>-NN-<slug>.md
  complete/              # งานที่เสร็จแล้ว (ย้ายมาด้วย git mv)
```

ที่ชั้นบนสุดของ `to-do/` มีแค่ README นี้, `backlog/` และ breakdown folder — งานเดี่ยวใหม่ใส่ `backlog/` เลย

## Spec กับ SPECREQUIREMENT ต่างกันอย่างไร

| เอกสาร | ตอบคำถาม | อายุ |
| --- | --- | --- |
| [`docs/SPECREQUIREMENT.md`](../../docs/SPECREQUIREMENT.md) | ระบบทั้งหมดต้องทำอะไร (`FR-...`) | ตลอดอายุโปรเจกต์ |
| `task-plans/spec/spec-<slug>.md` | ฟีเจอร์หนึ่งจะสร้าง **อย่างไร**: การตัดสินใจ, contract, acceptance | จนกว่าฟีเจอร์จะเสร็จ แล้วย้ายไป `complete/` |
| `task-plans/to-do/...` | ต้องทำอะไร ลำดับไหน ใครทำ ตรวจยังไง | จนกว่างานจะเสร็จ |

Spec อ้าง `FR-...` เสมอ ถ้าการตัดสินใจใน spec เปลี่ยน requirement ให้แก้ SPECREQUIREMENT พร้อม Change Record ใน PR เดียวกัน

## Spec file

`task-plans/spec/spec-<slug>.md` มีหัวข้อตามนี้:

1. **หัวเรื่อง** — `# SPEC: <ชื่อฟีเจอร์>`
2. **Metadata** — `Status:` (`Draft` / `Approved YYYY-MM-DD (โดยใคร)`), `Date:`, `Board:` (ลิงก์ `tasks.md`), `Requirements:` (`FR-...`)
3. **เป้าหมาย** — ผลลัพธ์ที่ต้องได้ + สิ่งที่ไม่ทำ
4. **การตัดสินใจ** — ตาราง `# | เรื่อง | ตัดสิน | ที่มา` (D1, D2, …) ห้ามเปลี่ยนเลข D หลัง approve
5. **Contracts** — endpoint, request/response, schema ที่ task ต่างๆ ต้องใช้ตรงกัน
6. **Acceptance** — checklist ที่พิสูจน์ได้ว่าฟีเจอร์ใช้ได้จริง
7. **Open questions** — สิ่งที่ยังไม่ตัดสิน (ห้ามเริ่ม task ที่ขึ้นกับข้อนี้)

แตก task ล่วงหน้าได้ตั้งแต่ spec เป็น `Draft` เพื่อให้ทีมเห็นขนาดงาน แต่ **เริ่มทำ task ได้เมื่อ spec เป็น `Approved` แล้วเท่านั้น**

## สองรูปแบบของงาน

1. **งานเดี่ยว** — ไฟล์ `TASK-<slug>-NN.md` เดียวใน `backlog/` ใช้เมื่องานมีเป้าหมายเดียว ตรวจครั้งเดียว ไม่มีลำดับขึ้นต่อกัน
2. **Breakdown folder** — `<feature>-to-do/` สำหรับงานขนาด spec ที่แตกเป็นหลาย task เรียงลำดับ ใช้เป็นค่าเริ่มต้นกับทุกงานที่มาจาก spec

## Breakdown folder

```
<feature>-to-do/
  tasks.md                      # execution board — ต้องมีเสมอ
  TASK-<PFX>-01-<slug>.md       # หนึ่งไฟล์ต่อหนึ่ง task
  TASK-<PFX>-02-<slug>.md
```

กฎการตั้งชื่อ:

- โฟลเดอร์: kebab-case ลงท้าย `-to-do`
- ไฟล์ task: `TASK-<PFX>-NN-<slug>.md` — `<PFX>` เป็นตัวพิมพ์ใหญ่สั้นๆ ไม่ซ้ำกับงานอื่น (MVP, AUTH, …) `NN` เป็นเลขสองหลัก slug เป็น kebab-case ตรงกับสิ่งที่ส่งมอบ
- **ห้ามเปลี่ยนชื่อไฟล์ task หลังเริ่มงานแล้ว** — ID ถูกอ้างใน board, commit และ PR

### `tasks.md` — execution board

หน้าเดียวที่ตอบ "ทำอะไร ลำดับไหน มีเงื่อนไขอะไร":

- หัวเรื่อง + **Source spec**
- **Goal** และ **Global constraints** ที่ใช้กับทุก task
- **ตาราง task**: `Task | Title | Depends on | Owner | Status` — หนึ่งแถวต่อหนึ่งไฟล์ ลิงก์ไปที่ไฟล์
- **Dependency order** เป็นแผนภาพสั้นๆ
- **Gates ที่สำคัญกว่าสถานะ task** — เงื่อนไขที่ทำให้ task ที่ติ๊กครบแล้วยังไม่ผ่าน
- **Receipts** — เพิ่มเมื่อ task เสร็จ: คำสั่งจริง + output จริง ไม่ใช่แค่บอกว่าผ่าน

สถานะใช้แค่: `TODO` → `IN PROGRESS` → `DONE` และ `BLOCKED` (ต้องบอกเหตุผล)

### TASK file — หนึ่งไฟล์ หนึ่งสิ่งที่ส่งมอบ

แต่ละ `TASK-<PFX>-NN-<slug>.md` อ่านคู่กับ spec แล้วต้องทำได้เลย:

1. **หัวเรื่อง** — `# TASK-<PFX>-NN — <สิ่งที่ส่งมอบ>`
2. **Metadata** — `Status:`, `Owner:`, `Spec:` (path + หัวข้อ), `Depends on:`, `Produces:` (สิ่งที่ task ถัดไปใช้)
3. **Goal** — ย่อหน้าสั้นๆ บอกผลลัพธ์ ไม่ใช่กิจกรรม
4. **Exact files** — ทุกไฟล์ที่สร้าง/แก้ ระบุชื่อ ถ้ามี migration ให้ระบุชื่อ migration
5. **Steps** — checkbox เรียงแบบเขียน test ก่อน: เขียน test ที่ fail → รันให้แดง → implement → รันให้เขียว → รัน quality gates
6. **Verification** — คำสั่งจริงที่ใครก็รันได้ พร้อมผลที่คาดหวัง
7. **Commit** — ข้อความ Conventional Commits และ stage เฉพาะ path ที่ระบุ (ห้าม `git add -A`)

Task ที่ไปต่อไม่ได้ (รอการตัดสินใจ, รอ task อื่น) ให้ใส่ `BLOCKED: <เหตุผล>` ที่บรรทัด Status แล้วทิ้งไว้ที่เดิม ห้ามลบหรือย้าย

## Completion — ย้ายไป `../complete/`

เมื่อ task เป็น `DONE` (มี receipt และ gates ผ่าน):

- **ทั้ง breakdown folder เสร็จ** → `git mv task-plans/to-do/<feature>-to-do task-plans/complete/` และย้าย spec ไปด้วย: `git mv task-plans/spec/spec-<slug>.md task-plans/complete/`
- **งานเดี่ยวเสร็จ** → `git mv task-plans/to-do/backlog/TASK-<slug>-NN.md task-plans/complete/`

Breakdown folder ย้ายได้เมื่อ **ทุก** task เป็น `DONE` เท่านั้น

---

## Project hooks — ค่าเฉพาะของโปรเจกต์นี้

- **Quality gates** (ต้องผ่านก่อน task จะเป็น `DONE`):
  `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`
  และถ้าแก้ `src/webboard-web/`: `npm run lint`, `npm run typecheck`, `npm test`, `npm run build` (ดู ADR-presentation-technology D5)
- **Spec location**: `task-plans/spec/`
- **Migration**: สร้างด้วย `dotnet ef migrations add <Name> --project src/Webboard.DataAccess --startup-project src/Webboard.Api` แล้วรัน `dotnet ef database update` ให้ผ่านก่อน commit — ห้ามแก้ migration ที่ merge แล้ว
- **Reject gates** (สำคัญกว่า checklist ที่ติ๊กครบ):
  - มี secret (connection string ที่มี password, JWT signing key) ใน commit → reject
  - รหัสผ่านเก็บแบบอื่นนอกจาก PBKDF2-HMAC-SHA256 → reject
  - Presentation ต่อฐานข้อมูลตรง หรือ Controller ใช้ `DbContext` ตรง → reject
