# ADR: Presentation Layer — React + Vite + TypeScript

**Status:** Accepted (narawichswu, 2026-10-31)
**Date:** 2026-10-31
**Spec:** [`spec-webboard-mvp.md`](../../../task-plans/spec/spec-webboard-mvp.md) §5 Q1; [`SPECREQUIREMENT.md`](../../SPECREQUIREMENT.md) NFR-001…003

ADR นี้บันทึกว่า Presentation Layer ใช้เทคโนโลยีอะไร ทำไม และมีผลอะไรกับการรัน dev, การ test และความปลอดภัย
โจทย์ให้เลือกอิสระ เงื่อนไขเดียวคือคุยกับระบบผ่าน REST API ([ADR-layered-architecture](ADR-layered-architecture.md) D1)

---

## 1. Context

Domain และ Data Access เสร็จแล้ว (MVP-01…05, AUTH-01…03) หน้าเว็บต้องทำได้ครบตาม Acceptance ของทั้งสอง spec:
ดูรายการ/กระทู้, สมัคร, login สองขั้นพร้อมแสดง QR ของ 2FA, ตั้งกระทู้, เขียนความเห็น
โดยแบบหน้าจอมีอยู่แล้วใน [`docs/mockups/webboard-layers.html`](../../mockups/webboard-layers.html)

เกณฑ์ที่ใช้เทียบ (จาก TASK-presentation-tech-decision-01):

1. คนในทีมเขียนเป็นหรือไม่
2. เรียก REST + `Authorization: Bearer` ได้ง่าย
3. มี library สร้าง QR ในเครื่อง (ห้ามส่ง `otpAuthUri` ไปบริการภายนอก เพราะมี shared key อยู่ใน URI)
4. รันคู่กับ `dotnet run` ได้ง่าย
5. เข้ากับรูปแบบ task ที่เขียน test ก่อน และแสดงข้อความผู้ใช้แบบ escape โดยค่าเริ่มต้น (NFR-003)

## 2. ตัวเลือกที่พิจารณา

| ตัวเลือก | ข้อดี | ข้อเสีย |
| --- | --- | --- |
| **Blazor WebAssembly** | C# ทั้งระบบ, `dotnet build/test` ชุดเดียว, Razor escape ให้เอง, bUnit + xUnit, QR ด้วย QRCoder | โหลดครั้งแรกช้า, ต้อง copy DTO มาไว้ฝั่ง UI (ห้าม reference Domain), ecosystem UI เล็กกว่า |
| **React + Vite + TypeScript** | dev loop เร็ว (HMR), ecosystem ใหญ่, Vite proxy `/api` ทำให้ dev ไม่ต้องตั้ง CORS, JSX escape ให้เอง, Vitest + Testing Library, QR ด้วย `qrcode` | เพิ่ม toolchain ที่สอง (Node/npm) และ gate ของ UI แยก, มี dependency จาก npm ที่ต้องดูแล |
| **HTML + JavaScript ล้วน** (แบบ mock) | ไม่มี build, ยก mock มาใช้ได้เกือบทันที | escape เองทุกจุด (พลาดง่ายกับ `innerHTML`), routing/state เขียนเอง, ไม่มีชุด test ที่ใช้จริง |

## 3. Decision 1 — ใช้ React + Vite + TypeScript ที่ `src/webboard-web/`

Single-page app แบบ React เขียนด้วย TypeScript (`strict`) build ด้วย Vite routing ด้วย React Router
โปรเจกต์อยู่ที่ `src/webboard-web/` ไม่อยู่ใน `Webboard.sln` และไม่ reference project C# ใดๆ

### Why

React เป็นเทคโนโลยี UI ที่มีคนเขียนเป็น ตัวอย่าง และ library ให้ใช้มากที่สุดในสามตัวเลือก (เกณฑ์ 1)
ส่วน dev loop ของ Vite ทำให้ปรับหน้าจอตาม mock ได้เร็ว
TypeScript ทำให้ contract ใน spec §3 (DTO) ถูกเขียนเป็น type ฝั่ง UI และ compiler จับได้เมื่อ field ไม่ตรง
JSX escape ข้อความทุกตัวโดยค่าเริ่มต้น จึงผ่าน NFR-003 ได้ตราบที่ไม่ใช้ `dangerouslySetInnerHTML` (เกณฑ์ 5)

## 4. Decision 2 — Dev: Vite proxy `/api` ไปที่ API ไม่ตั้ง CORS

`vite.config.ts` proxy ทุก path ที่ขึ้นต้น `/api` ไปที่ URL ของ `Webboard.Api` (อ่านจาก env `WEBBOARD_API_URL`
ค่าเริ่มต้นตาม `launchSettings.json`) หน้าเว็บจึงเรียก `/api/...` แบบ relative เสมอ

```sh
dotnet run --project src/Webboard.Api        # terminal 1
npm run dev --prefix src/webboard-web        # terminal 2 → เปิด URL ที่ Vite แสดง
```

### Why

Browser มองว่า request ไป origin เดียวกับหน้าเว็บ จึงไม่ต้องเปิด CORS ที่ `Program.cs` และไม่มีทางเผลอเปิด
origin กว้างเกินไป (เกณฑ์ 4) และยืนยันได้ง่ายว่าหน้าเว็บเรียกแค่ `/api/...` (NFR-001)

### Consequences

- Deploy จริง (นอกขอบเขต MVP) ต้องเสิร์ฟไฟล์จาก `npm run build` ผ่าน reverse proxy ที่ส่ง `/api` ไป API บน HTTPS (NFR-007)
  หรือเปิด CORS เฉพาะ origin นั้นใน ADR ใหม่

## 5. Decision 3 — Token อยู่ในหน่วยความจำเท่านั้น

Access token, MFA token และข้อมูลผู้ใช้อยู่ใน React state (context) — ไม่ใช้ `localStorage`, `sessionStorage`, cookie หรือ URL
ค่าตั้งค่า UI ที่ไม่ลับ (ปัจจุบันมีอย่างเดียวคือธีม light/dark ใน key `webboard-theme`) เก็บใน `localStorage` ได้ โดยอ่าน/เขียนใน `try/catch` และหน้าเว็บต้องทำงานได้เมื่อ storage ใช้ไม่ได้
ฟอนต์เสิร์ฟจากแพ็กเกจ `@fontsource/*` ของแอปเอง ไม่โหลดจาก CDN ภายนอก เพื่อให้ทุก request ไปที่ origin เดียวกัน
API client (`fetch` wrapper เดียว) ใส่ `Authorization: Bearer` เมื่อมี token และเมื่อได้ `401` จะลบ token แล้วพาไปหน้า login

### Why

ตาม [ARCHITECTURE §9.1](../ARCHITECTURE.md#91-แต่ละ-layer-ทำอะไร) และลดผลของ XSS: script แปลกปลอมอ่าน storage ไม่ได้
ผลคือ refresh หน้า = ต้อง login ใหม่ ซึ่งยอมรับได้เพราะ access token มีอายุ 60 นาทีและไม่มี refresh token อยู่แล้ว

## 6. Decision 4 — QR สร้างในเบราว์เซอร์ด้วย `qrcode`

`QRCode.toDataURL(otpAuthUri)` แล้วแสดงเป็น `<img>` พร้อมแสดง `sharedKey` ให้พิมพ์เองได้

### Why

`otpAuthUri` มี shared key ของ 2FA ถ้าส่งไปบริการสร้าง QR ภายนอก key จะรั่วออกนอกระบบ (NFR-006, เกณฑ์ 3)

## 7. Decision 5 — Quality gates ของ UI

Task ที่แก้ `src/webboard-web/` ต้องผ่าน gate เหล่านี้เพิ่มจาก gate ของ .NET:

```sh
npm ci --prefix src/webboard-web
npm run lint --prefix src/webboard-web        # ESLint, 0 warning; react/no-danger = error
npm run typecheck --prefix src/webboard-web   # tsc --noEmit
npm test --prefix src/webboard-web            # Vitest + Testing Library + MSW (จำลอง /api)
npm run build --prefix src/webboard-web
```

### Why

ให้ UI มีมาตรฐานเดียวกับฝั่ง .NET (build ไม่มี warning, test ก่อน implement) และให้ `react/no-danger`
บังคับ NFR-003 ด้วยเครื่องมือแทนการจำ

### Consequences

- Node.js LTS (≥ 20) เป็นเครื่องมือที่ทุกคนต้องติดตั้งเพิ่ม — ปักใน `package.json` `engines` และ `.nvmrc`
- `package-lock.json` ต้อง commit; เพิ่ม dependency ใหม่ต้องบอกเหตุผลใน PR
- CI (TASK-ci-build-test-01) ต้องรัน gate ชุดนี้ด้วย
