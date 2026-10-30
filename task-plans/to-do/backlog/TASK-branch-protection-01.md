# TASK-branch-protection-01 — สร้าง `develop` และตั้ง branch protection ตาม CONTRIBUTING

**Status:** TODO — ต้องทำโดย admin ของ repo (ToonRz)
**Owner:** ToonRz
**Depends on:** — (ตั้ง required check `ci` เพิ่มหลัง TASK-ci-build-test-01)
**Spec:** `CONTRIBUTING.md` → Git Branching Strategy → Long-Lived Branches
**Produces:** `main` และ `develop` ที่ push ตรงไม่ได้

## Goal

กฎใน CONTRIBUTING ("ต้องผ่าน PR, ≥ 1 review, ห้าม push ตรง") ถูกบังคับโดย GitHub จริง ไม่ใช่แค่ข้อตกลง

## Exact files

ไม่มีไฟล์ใน repo — ตั้งค่าบน GitHub (Settings → Branches หรือ Rulesets)

## Steps

- [ ] สร้าง `develop` จาก `main`: `git switch main && git pull && git switch -c develop && git push -u origin develop`
- [ ] ตั้ง `develop` เป็น default branch (Settings → General) เพื่อให้ PR ใหม่ชี้ `develop` อัตโนมัติ
- [ ] Rule ของ `main`: require pull request, required approvals = 1, block force push, block deletion
- [ ] Rule ของ `develop`: require pull request, block force push
- [ ] หลัง TASK-ci-build-test-01 เสร็จ: เพิ่ม required status check `ci` ให้ทั้งสอง branch

## Verification

```sh
git push origin HEAD:main            # ต้องถูกปฏิเสธ: protected branch
gh api repos/ToonRz/Project_SASD --jq .default_branch   # develop
```

## Commit

ไม่มี commit — บันทึกผล verification เป็น receipt ในไฟล์นี้แล้วย้ายไป `complete/`
