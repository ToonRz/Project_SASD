# TASK-ui-coffee-restyle-01 — Restyle UI ตาม mock ธีมกาแฟ

**Status:** DONE
**Owner:** narawichswu (Claude)
**Depends on:** MVP-06 (commit `911f2be`)
**Spec:** `docs/mockups/webboard-ui.html` (ดีไซน์ล่าสุด), `docs/architecture/decisions/ADR-presentation-technology.md`
**Produces:** UI ใน `src/webboard-web/` หน้าตาตรงกับ mock (palette กาแฟ, ฟอนต์, topbar, panel, motion, light/dark)

## Goal

เปลี่ยนหน้าตา UI ของ MVP จาก palette teal (`webboard-layers.html`) เป็น palette กาแฟของ `webboard-ui.html` โดย **ไม่เปลี่ยนพฤติกรรม**: route เดิม, API call เดิม, ข้อความผู้ใช้ escape เหมือนเดิม, token อยู่ใน memory เหมือนเดิม ไม่เพิ่มหน้า/ฟีเจอร์นอก MVP (rooms, tags, reply, profile, MOCK switcher)

## Exact files

- Modify `src/webboard-web/index.html` — โหลดฟอนต์ Chakra Petch, IBM Plex Sans Thai, Mali, IBM Plex Mono
- Rewrite `src/webboard-web/src/styles/app.css` — tokens/ปุ่ม/topbar/panel/post card/form/comment/motion, `prefers-reduced-motion`
- Modify `src/webboard-web/src/App.tsx` — topbar (brand mark, ปุ่มสลับธีม light/dark), `.view` panel
- Modify `src/webboard-web/src/components.tsx` — `BrandMark`
- Modify `src/webboard-web/src/pages/*.tsx` — class/markup ของ hero, post card, comment (ไม่แตะ logic)
- Modify `src/webboard-web/src/App.test.tsx` — test ปุ่มสลับธีม

## Steps

- [x] อ่าน mock, เทียบกับหน้าที่มี
- [x] เขียน test ปุ่มสลับธีม (แดง) แล้ว implement (เขียว)
- [x] เขียน CSS + ปรับ markup
- [x] รัน gates ทั้ง UI และ .NET
- [x] เดินจริงใน browser: register → login → 2FA setup (เรียก `/api/auth/2fa/setup` ครั้งเดียว) → verify → ตั้งกระทู้ → ความเห็น → logout; XSS title; screenshot light/dark/375px

## Verification

`npm run lint`, `npm run typecheck`, `npm test`, `npm run build` (ใน `src/webboard-web`); `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`

## Commit

`feat(ui): restyle pages with coffee palette from webboard-ui mock`

Stage: `src/webboard-web/ task-plans/` (ไม่รวม `node_modules/` `dist/`)

## Receipt

```
$ npm run lint        (src/webboard-web)   -> eslint . --max-warnings 0  : 0 warnings
$ npm run typecheck                        -> tsc -b --noEmit            : clean
$ npm test                                 -> Test Files 2 passed, Tests 17 passed (16 เดิม + 1 theme toggle; แดงก่อน implement)
$ npm run build                            -> dist/assets/index-*.css 11.76 kB, index-*.js 294.90 kB, built in 218ms
$ dotnet build -warnaserror                -> 0 Warning(s), 0 Error(s)
$ dotnet test                              -> Passed! Failed: 0, Passed: 61, Total: 61
$ dotnet format --verify-no-changes        -> exit 0
```

Browser walk (Vite dev + `dotnet run --project src/Webboard.Api`, built-in browser):
register 201 → login 200 → `/api/auth/2fa/setup` เรียก **1 ครั้ง** (QR + sharedKey แสดง, TOTP คำนวณในเครื่องด้วย node) → verify 200 → ตั้งกระทู้ 201 → ความเห็น 201 (`<b>bold?</b>` แสดงเป็นข้อความ) → logout: forms หายเหลือลิงก์เข้าสู่ระบบ
ทุก request ไปที่ `/api/...`; title `<script>alert(1)</script>` แสดงเป็นข้อความ; screenshot light/dark และ 375px ไม่มี horizontal overflow (`scrollWidth == clientWidth == 375`)

ปรับระหว่างเดิน: โหมดมืดที่บันทึกไว้กระพริบเป็นสว่างตอนโหลด (ตั้ง `data-theme` ใน effect) → ตั้งตอนโหลด module; header บน 375px ตกบรรทัด → ลด padding/ขนาดปุ่มให้อยู่แถวเดียว

ไม่ได้ทำ (ตั้งใจ): left nav, rooms/tags/reply/profile/MOCK switcher, mascot/sticker ของ mock — ไม่อยู่ใน MVP

## Receipt — follow-up (self-host fonts, theme preference docs)

```
$ npm i @fontsource/chakra-petch @fontsource/ibm-plex-sans-thai @fontsource/ibm-plex-mono   (Mali ไม่ติดตั้ง: CSS ไม่ใช้)
$ npm run lint / typecheck / test / build  -> 0 warnings / clean / 17 passed / built (20 woff2 ใน dist/assets, ไม่มี fonts.g* ใน dist/index.html)
$ dotnet build -warnaserror -> 0 Warning(s), 0 Error(s);  dotnet test -> Passed 61/61;  dotnet format --verify-no-changes -> exit 0
$ grep -rn "dangerouslySetInnerHTML\|sessionStorage" src/webboard-web/src                -> (ไม่มีผลลัพธ์)
$ grep -rn "localStorage" src/webboard-web/src | grep -v webboard-theme                  -> (ไม่มีผลลัพธ์)
```

Network log (dev server, browser pane): ทุก resource มาจาก origin `http://localhost:5173` เท่านั้น (`/@vite`, `/src`, `/node_modules/@fontsource/*/files/*.woff2`, `/api/...`) ไม่มี request ไป fonts.googleapis.com / fonts.gstatic.com
Thai: `document.fonts.check('400 15px "IBM Plex Sans Thai"', 'กระทู้')` = true; โหลด `ibm-plex-sans-thai-thai-400-normal.woff2` และ `chakra-petch-thai-{600,700}` จริง
