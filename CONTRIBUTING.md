<!-- omit in toc -->
# Contributing to Project_SASD (Webboard)

แนวทางการทำงานร่วมกันของทีม: การแตก branch, การเขียน commit, การเปิด PR และกฎเรื่อง secret

<!-- omit in toc -->
## Table of Contents

- [ก่อนเริ่ม](#ก่อนเริ่ม)
- [รายงาน Bug / เสนอ Feature](#รายงาน-bug--เสนอ-feature)
- [Git Branching Strategy](#git-branching-strategy)
- [Commit Messages](#commit-messages)
- [Pull Requests](#pull-requests)
- [Code Style](#code-style)
- [Secrets](#secrets)

## ก่อนเริ่ม

อ่าน [README.md](README.md) แล้วตามด้วย:

- [docs/SPECREQUIREMENT.md](docs/SPECREQUIREMENT.md) — งานที่ทำต้องอ้างถึง `FR-...` ข้อใดข้อหนึ่ง ถ้าไม่มีข้อที่ตรง ให้เพิ่ม requirement ก่อน
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — โดยเฉพาะ §3 กฎการพึ่งพาระหว่าง layer
- [CONTEXT.md](CONTEXT.md) — ใช้ศัพท์ให้ตรง (Topic ไม่ใช่ Post, Comment ไม่ใช่ Reply ฯลฯ)

## รายงาน Bug / เสนอ Feature

เปิด [Issue](https://github.com/ToonRz/Project_SASD/issues/new) แล้วใส่:

**Bug**
- ขั้นตอนทำซ้ำ (step-by-step)
- ผลที่คาดหวัง vs ผลที่เกิดขึ้นจริง
- request/response ของ API (ถ้าเกี่ยว), error message / stack trace
- branch และ commit ที่ใช้

**Feature**
- อ้างถึง `FR-...` หรือเสนอ FR ใหม่
- พฤติกรรมปัจจุบัน vs พฤติกรรมที่ต้องการ และเหตุผล
- ภาพหน้าจอ/ภาพร่าง ถ้าเกี่ยวกับ UI

## Git Branching Strategy

ใช้แบบ GitFlow ย่อ: branch ถาวรสองตัว + branch งานอายุสั้น

<!-- omit in toc -->
### Long-Lived Branches

| Branch | ใช้ทำ | Protection |
| --- | --- | --- |
| `main` | เวอร์ชันที่พร้อมส่ง/พร้อม demo ทุก commit ต้องรันได้ | ต้องผ่าน PR, ≥ 1 review, build + test ผ่าน, ห้าม push ตรง |
| `develop` | รวมงานของทุกคนก่อนขึ้น `main` | ต้องผ่าน PR, build + test ผ่าน |

> branch ถาวรต้อง **เขียวเสมอ** — ห้าม push โค้ดที่ build ไม่ผ่านขึ้น `main` หรือ `develop`

<!-- omit in toc -->
### Short-Lived Working Branches

| Prefix | แตกจาก | Merge เข้า | ใช้ทำ | ตัวอย่าง |
| --- | --- | --- | --- | --- |
| `feature/` | `develop` | `develop` | ฟีเจอร์ใหม่ | `feature/12-create-topic` |
| `fix/` | `develop` | `develop` | แก้ bug | `fix/18-comment-404` |
| `docs/` | `develop` | `develop` | แก้เอกสารอย่างเดียว | `docs/api-table` |
| `chore/` | `develop` | `develop` | tooling, dependency, refactor ที่ไม่เปลี่ยนพฤติกรรม | `chore/add-editorconfig` |

<!-- omit in toc -->
### Branch Naming

- ตัวพิมพ์เล็ก, kebab-case, ASCII เท่านั้น — ห้ามเว้นวรรค ห้าม underscore ห้ามภาษาไทย
- ใส่เลข issue ถ้ามี: `feature/12-...`
- สั้นและอ่านรู้เรื่อง (≤ 60 ตัวอักษร) — ห้ามใช้ชื่อคน (`toon-work`) หรือชื่อกว้างๆ (`update`, `wip`)

```
<type>/<issue-id>-<short-description>
```

<!-- omit in toc -->
### Workflow

```text
  feature/  fix/  docs/  chore/      ← สั้น, 1 PR ต่อ branch
            │  PR + review + CI
            ▼
        ┌─────────┐   PR เมื่อพร้อม demo/ส่งงาน   ┌──────┐
        │ develop │ ──────────────────────────▶ │ main │
        └─────────┘                              └──────┘
```

1. อัปเดต base: `git switch develop && git pull --rebase`
2. แตก branch: `git switch -c feature/12-create-topic`
3. commit เป็นชิ้นเล็กๆ ตาม [Commit Messages](#commit-messages)
4. ก่อนเปิด PR ให้ rebase: `git fetch && git rebase origin/develop`
5. push แล้วเปิด PR เข้า `develop`
6. รอ build/test ผ่านและได้ review ≥ 1 คน
7. Squash-and-merge แล้ว **ลบ branch**
8. เมื่อ `develop` พร้อม demo → เปิด PR `develop` → `main`

<!-- omit in toc -->
### DO / DON'T

**DO**
- แตกทุกงานจาก `develop`
- PR เล็ก ทำเรื่องเดียว
- ลบ branch หลัง merge

**DON'T**
- push ตรงเข้า `main` หรือ `develop`
- force-push branch ที่คนอื่นกำลัง review อยู่
- เก็บ branch ส่วนตัวไว้นาน — merge หรือลบภายในไม่กี่วัน

## Commit Messages

ใช้ **Conventional Commits** — type ต้องสอดคล้องกับ prefix ของ branch หนึ่ง commit = หนึ่งเรื่อง

<!-- omit in toc -->
### Format

```text
<type>(<optional-scope>): <imperative-subject>

<optional-body — อธิบายว่า "ทำไม" ไม่ใช่ "ทำอะไร">

<optional-footer — Closes #12>
```

- Subject ภาษาอังกฤษ, ขึ้นต้นตัวเล็ก, รูปคำสั่ง (`add` ไม่ใช่ `added`), ไม่มีจุดท้าย, ≤ 72 ตัวอักษร
- อ้าง issue ใน footer (`Closes #12`) ไม่ใช่ใน subject
- Body ภาษาไทยหรืออังกฤษก็ได้

<!-- omit in toc -->
### Types

| Type | Branch prefix | ใช้เมื่อ |
| --- | --- | --- |
| `feat` | `feature/` | ฟีเจอร์ใหม่ |
| `fix` | `fix/` | แก้ bug |
| `docs` | `docs/` | เอกสารอย่างเดียว |
| `refactor` | `chore/` | ปรับโครงสร้างโค้ด ไม่เปลี่ยนพฤติกรรม |
| `test` | `chore/` | เพิ่ม/แก้ test |
| `build` | `chore/` | `.csproj`, NuGet package, Dockerfile |
| `ci` | `chore/` | GitHub Actions |
| `chore` | `chore/` | งานจิปาถะ (tooling, script) |
| `style` | `chore/` | จัด format, ไม่เปลี่ยน logic |
| `revert` | — | ย้อน commit ก่อนหน้า |

<!-- omit in toc -->
### Scopes

ใช้ชื่อ layer/ส่วนของระบบ: `ui` (Presentation), `api` (Domain), `data` (Data Access / EF), `db` (migration/schema), `auth` (login, 2FA, password hashing), `docs`, `ci`

<!-- omit in toc -->
### Examples

**Good**
```
feat(api): add POST /api/topics endpoint

Validates title/body/authorName and returns 201 with Location
header. Implements FR-TOPIC-001.

Closes #12
```

```
fix(api): return 404 when commenting on missing topic

Closes #18
```

```
feat(db): add parent_comment_id to comment table
```

**Bad**
```
update                      # ไม่มี type, ไม่บอกอะไร
Fixed the bug               # past tense, ตัวใหญ่
feat: WIP                   # WIP ใน history
add topic api and fix ui    # สองเรื่องใน commit เดียว
```

## Pull Requests

- หัวข้อ PR ใช้รูปแบบเดียวกับ commit subject (กลายเป็น commit ตอน squash)
- ใน description ระบุ:
  - `FR-...` ที่ PR นี้ทำ
  - วิธีทดสอบ (คำสั่ง, request ตัวอย่าง, ภาพหน้าจอถ้าเป็น UI)
- Checklist ก่อนขอ review:
  - [ ] `dotnet build` ผ่าน ไม่มี warning
  - [ ] `dotnet test` ผ่าน
  - [ ] ถ้าแก้ Entity → มี migration ใหม่ที่สร้างด้วย `dotnet ef migrations add` และรัน `database update` ผ่าน
  - [ ] ถ้าเปลี่ยน API/โครงสร้าง/requirement → อัปเดต `docs/` ใน PR เดียวกัน
  - [ ] ไม่มี secret ใน diff

## Code Style

- ทำตาม `.editorconfig`; รัน `dotnet format` ก่อน commit
- C#: `PascalCase` สำหรับ class/method/property, `camelCase` สำหรับ local/parameter, `_camelCase` สำหรับ private field
- ตั้งชื่อตามศัพท์ใน [CONTEXT.md](CONTEXT.md) — `TopicService`, `CommentDto` (ไม่ใช่ `PostService`)
- เคารพกฎ layer ใน [ARCHITECTURE.md §3](docs/ARCHITECTURE.md#3-กฎการพึ่งพา-dependency-rules)
- ห้ามแก้ไฟล์ใน `Migrations/` ที่ merge ไปแล้ว — ถ้าต้องเปลี่ยน schema ให้สร้าง migration ใหม่

## Secrets

- **ห้าม commit secret** — connection string จริง, password ของฐานข้อมูล, JWT signing key, Data Protection key, API key
- เก็บ connection string ตอน dev ด้วย `dotnet user-secrets` หรือ environment variable
- `appsettings.json` ที่ commit ได้มีแค่ค่าตัวอย่าง (เช่น `Host=localhost;Database=webboard;Username=postgres`) ไม่มี password
- ถ้าเผลอ commit secret ไปแล้ว: เปลี่ยน password นั้นทันที แล้วแจ้งทีม — การลบใน commit ถัดไปไม่พอ เพราะยังอยู่ใน history
