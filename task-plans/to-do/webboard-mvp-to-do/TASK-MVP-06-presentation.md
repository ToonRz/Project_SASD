# TASK-MVP-06 — หน้าเว็บ: รายการ, กระทู้, login + 2FA, ตั้งกระทู้, ความเห็น

**Status:** DONE
**Owner:** narawichswu (Claude)
**Depends on:** MVP-05, AUTH-03
**Spec:** `task-plans/spec/spec-webboard-mvp.md` §1.3, §3, §4; `docs/ARCHITECTURE.md` §9.1 (แถว Presentation); `docs/architecture/decisions/ADR-presentation-technology.md`
**Produces:** `src/webboard-web/` (React + Vite + TypeScript) ที่รันคู่กับ API ได้

## Goal

ผู้ใช้ทำทุกอย่างใน Acceptance ของทั้งสอง spec ผ่านหน้าเว็บได้ โดยหน้าเว็บเรียกแค่ `/api/...` ใช้ [docs/mockups/webboard-layers.html](../../../docs/mockups/webboard-layers.html) เป็นแบบของหน้าจอและ flow

## Exact files

- Create `src/webboard-web/` ด้วย `npm create vite@latest webboard-web -- --template react-ts` (รันใน `src/`)
- Create `src/webboard-web/package.json` — script `dev`, `build`, `lint`, `typecheck`, `test`; `engines.node` ≥ 20; dependency `react-router`, `qrcode`; devDependency `vitest`, `@testing-library/react`, `@testing-library/user-event`, `jsdom`, `msw`, `eslint-plugin-react`
- Create `src/webboard-web/package-lock.json`, `.nvmrc`
- Create `src/webboard-web/vite.config.ts` — proxy `/api` → `process.env.WEBBOARD_API_URL ?? <URL http ของ Webboard.Api ใน launchSettings.json>`; ตั้ง Vitest (`environment: 'jsdom'`)
- Create `src/webboard-web/eslint.config.js` — `react/no-danger: 'error'`, `--max-warnings 0`
- Create `src/webboard-web/src/api/types.ts` — type ตาม DTO ใน spec-webboard-mvp §3 และ spec-auth-login-2fa §3 (camelCase)
- Create `src/webboard-web/src/api/client.ts` — `fetch` wrapper: ใส่ `Authorization: Bearer`, แปลง Problem Details เป็น error, `401` → เรียก `onUnauthorized`
- Create `src/webboard-web/src/auth/AuthContext.tsx` — เก็บ MFA token / access token / user ใน state เท่านั้น, `logout()`
- Create `src/webboard-web/src/pages/TopicsPage.tsx` (`/`), `TopicPage.tsx` (`/topics/:id`), `RegisterPage.tsx` (`/register`), `LoginPage.tsx` (`/login`), `TwoFactorPage.tsx` (`/login/2fa` — ตั้ง 2FA พร้อม QR ถ้า `twoFactorSetupRequired`, แล้วกรอกรหัส)
- Create `src/webboard-web/src/App.tsx`, `main.tsx`, `styles/` — สีและ layout ยกจาก mock
- Create `src/webboard-web/src/**/*.test.tsx` และ `src/test/server.ts` (MSW handlers)
- Modify `README.md` — Quick start ของหน้าเว็บ (`npm ci`, `npm run dev`) และ gate ของ UI
- Modify `docs/TECHSTACK.md` §4 — เวอร์ชันจริงจาก `package.json`

ไม่ต้องแก้ `src/Webboard.Api/Program.cs` — dev ใช้ Vite proxy ไม่ใช้ CORS (ADR D2)

## Steps

- [x] Unblock: ทำ TASK-presentation-tech-decision-01 และเติม Exact files ด้านบน
- [x] Scaffold ด้วย Vite แล้วตั้ง ESLint, Vitest, MSW ให้ `npm run lint`, `npm run typecheck`, `npm test`, `npm run build` รันผ่านกับโปรเจกต์เปล่า
- [x] เขียน test ที่ fail (MSW จำลอง API):
  - ไม่มี token → หน้า `/` ไม่แสดงฟอร์มตั้งกระทู้ และหน้า `/topics/:id` ไม่แสดงฟอร์มความเห็น
  - `client` ได้ `401` → ลบ token และพาไป `/login`
  - login ได้ `twoFactorSetupRequired: true` → หน้า 2FA แสดง QR (`<img src="data:image/png...">`) และ `sharedKey`
  - หัวข้อกระทู้ `<script>alert(1)</script>` แสดงเป็นข้อความ (มี text นั้นใน DOM, ไม่มี `<script>` ที่สร้างจากข้อมูล)
  - เวลา `createdAt` (UTC) แสดงเป็นเวลาท้องถิ่น (NFR-002)
  - ไม่มีการเรียก URL ที่ไม่ขึ้นต้นด้วย `/api/` (MSW `onUnhandledRequest: 'error'`)
- [x] รันให้แดง
- [x] Implement หน้าต่างๆ; ห้าม `dangerouslySetInnerHTML`; token อยู่ใน memory เท่านั้น ไม่ใส่ใน URL หรือ storage
- [x] รันให้เขียว + gate ของ UI (ADR D5) + gate ของ .NET
- [x] เดินตาม Acceptance ของทั้งสอง spec ด้วยมือในเบราว์เซอร์ บันทึกผลเป็น receipt

## Verification

```sh
dotnet run --project src/Webboard.Api                 # terminal 1 (ต้องตั้ง user-secrets ตาม README)
npm ci --prefix src/webboard-web
npm run lint --prefix src/webboard-web                # 0 problems
npm run typecheck --prefix src/webboard-web           # ไม่มี error
npm test --prefix src/webboard-web                    # ผ่านทั้งหมด
npm run build --prefix src/webboard-web               # built in …
npm run dev --prefix src/webboard-web                 # terminal 2
grep -rn "dangerouslySetInnerHTML\|localStorage\|sessionStorage" src/webboard-web/src   # ไม่มีผลลัพธ์
```

- เดิน flow: สมัคร → ตั้ง 2FA ด้วยแอป Authenticator จริง → ตั้งกระทู้ → เขียนความเห็น → logout → เห็นว่าฟอร์มหายไป
- ใส่ `<script>alert(1)</script>` เป็นหัวข้อกระทู้ → แสดงเป็นข้อความ ไม่รัน
- DevTools Network: ทุก request ไปที่ `/api/...` เท่านั้น

## Commit

`feat(ui): add webboard pages for topics, comments and login`

Stage: `src/webboard-web/ README.md docs/TECHSTACK.md task-plans/to-do/webboard-mvp-to-do/` (ไม่ stage `node_modules/` และ `dist/` — ต้องอยู่ใน `.gitignore`)
