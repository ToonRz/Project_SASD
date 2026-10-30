# ADR: Authentication — PBKDF2-HMAC-SHA256 Passwords, Mandatory TOTP 2FA, JWT Bearer

**Status:** Accepted
**Date:** 2026-10-30
**Spec:** [`SPECREQUIREMENT.md`](../../SPECREQUIREMENT.md) §4.6 (FR-AUTH)

ADR นี้บันทึกว่าระบบเก็บรหัสผ่านอย่างไร ทำ 2FA แบบไหน และส่งตัวตนผู้ใช้ระหว่าง layer อย่างไร
พร้อมเหตุผล เพื่อให้การเปลี่ยนที่ทำให้ระบบอ่อนลง (เช่น "เปลี่ยนเป็น SHA-256 ตรงๆ จะได้ง่าย") ถูกมองว่าเป็นการเปลี่ยนสถาปัตยกรรม

---

## 1. Context

Requirement ใหม่: ผู้ใช้ต้อง login, login ต้องผ่าน 2FA และรหัสผ่านต้องเก็บด้วย SHA-256

คำว่า "encrypt เป็น SHA-256" ต้องตีความก่อน: SHA-256 เป็น **hash** ไม่ใช่การเข้ารหัส (ถอดกลับไม่ได้ ซึ่งถูกต้องแล้วสำหรับรหัสผ่าน)
แต่การเก็บ `SHA256(password)` ตรงๆ ไม่ปลอดภัย ดูเหตุผลใน Decision 1

## 2. Decision 1 — รหัสผ่านเก็บเป็น PBKDF2-HMAC-SHA256 ไม่ใช่ SHA-256 ตรงๆ

```
password_hash = "PBKDF2-SHA256$600000$<salt base64>$<hash base64>"
hash = PBKDF2(HMAC-SHA256, password, salt = 16 random bytes, iterations = 600,000, length = 32 bytes)
```

ใน .NET ใช้ `Rfc2898DeriveBytes.Pbkdf2(password, salt, 600_000, HashAlgorithmName.SHA256, 32)` (มีใน BCL ไม่ต้องลง package)
และเทียบผลด้วย `CryptographicOperations.FixedTimeEquals`

### Why

SHA-256 ยังเป็นแกนของการคำนวณ (ตรงตาม requirement) แต่ห่อด้วยสองอย่างที่ SHA-256 เปล่าไม่มี:

1. **Salt ต่อผู้ใช้** — `SHA256("password123")` ให้ค่าเดิมทุกครั้ง ผู้โจมตีที่ได้ฐานข้อมูลไปเปิด rainbow table หาเจอทันที และเห็นด้วยว่าใครใช้รหัสเดียวกัน
2. **ช้าโดยตั้งใจ** — GPU ทั่วไปคำนวณ SHA-256 ได้หลายพันล้านครั้งต่อวินาที การเดารหัส 8 ตัวจึงใช้เวลาไม่นาน
   600,000 รอบ (ค่าที่ OWASP แนะนำสำหรับ PBKDF2-HMAC-SHA256) ทำให้แต่ละครั้งช้าลงเป็นแสนเท่า ขณะที่ผู้ใช้จริงรอแค่เสี้ยววินาทีตอน login

จำนวนรอบเก็บไว้ใน string เอง ถ้าวันหน้าต้องเพิ่ม ก็ hash ใหม่ตอนผู้ใช้ login สำเร็จได้โดยไม่ทำให้ hash เก่าใช้ไม่ได้

### Consequences

- รหัสผ่านไม่ถูก log, ไม่อยู่ใน response, ไม่อยู่ใน exception message
- ห้าม "เข้ารหัสแบบถอดกลับได้" (AES ฯลฯ) สำหรับรหัสผ่าน — ระบบไม่มีเหตุต้องรู้รหัสผ่านจริงหลังจากตรวจแล้ว
- ไม่ใช้ ASP.NET Core Identity เพราะ `PasswordHasher` ของ Identity ใช้ HMAC-SHA512 (ไม่ตรง requirement) และ Identity เพิ่มตาราง/แนวคิดเกินขนาดโปรเจกต์นี้

## 3. Decision 2 — 2FA เป็น TOTP (RFC 6238) และบังคับทุกคน

- แอป Authenticator (Google Authenticator, Microsoft Authenticator ฯลฯ) สร้างรหัส 6 หลักทุก 30 วินาที จาก secret ที่ผู้ใช้สแกนเป็น QR ตอนตั้งค่า
- ผู้ใช้ที่ยังไม่เคยตั้ง 2FA จะได้ access token ก็ต่อเมื่อตั้งค่าและยืนยันรหัสแรกสำเร็จ
- ยอมรับรหัสคลาดเวลา ±1 ช่วง (30 วินาที) และ **ห้ามใช้รหัสของช่วงเวลาเดิมซ้ำ** (เก็บ `totp_last_step`)
- TOTP secret ต้องอ่านกลับได้เพื่อคำนวณรหัส จึง hash ไม่ได้ — เก็บแบบเข้ารหัสด้วย ASP.NET Core Data Protection (`IDataProtector.Protect`)

### Why

TOTP ไม่ต้องพึ่งบริการภายนอก (ไม่มีค่า SMS, ไม่ต้องตั้ง SMTP) ทำงาน offline บนมือถือผู้ใช้ และเป็นมาตรฐานที่แอปทั่วไปรองรับ
SMS OTP ถูกโจมตีแบบ SIM swap ได้ และ NIST SP 800-63B จัดเป็นวิธีที่มีข้อจำกัด

## 4. Decision 3 — Login สองขั้น ด้วย token สองชนิด (JWT Bearer)

| Token | ได้มาจาก | อายุ | ใช้กับ |
| --- | --- | --- | --- |
| **MFA token** | รหัสผ่านถูก (`POST /api/auth/login`) | 5 นาที | `/api/auth/2fa/*` เท่านั้น |
| **Access token** | รหัส 2FA ถูก (`POST /api/auth/2fa/verify`) | 60 นาที | ทุก endpoint ที่ต้อง login |

ทั้งคู่เป็น JWT (HS256) ที่มี claim `purpose` = `mfa` หรือ `access`; endpoint ปกติรับเฉพาะ `access`
Presentation ส่งใน header `Authorization: Bearer <token>`

### Why

- **Bearer token** ทำให้ Presentation Layer จะใช้เทคโนโลยีอะไรก็ได้ (โจทย์ให้เลือกอิสระ) ไม่ต้องจัดการ cookie ข้าม origin
- **แยก MFA token** ทำให้คนที่รู้แค่รหัสผ่านทำได้อย่างเดียวคือส่งรหัส 2FA — เรียก API อื่นไม่ได้
- **ไม่มี refresh token** — ครบ 60 นาทีให้ login ใหม่ ลดความซับซ้อน; logout = Presentation ลบ token ทิ้ง

## 5. Decision 4 — กันการเดา

- รหัสผ่านผิดหรือ username ไม่มี → ตอบ `401` ข้อความเดียวกัน ("Invalid username or password.") และยังคำนวณ PBKDF2 กับ hash หลอกเมื่อไม่พบ username เพื่อไม่ให้เวลาตอบบอกได้ว่า username มีจริง
- ผิดติดกัน 5 ครั้ง (รหัสผ่านหรือรหัส 2FA) → ล็อก 15 นาที ตอบ `429`
- Deploy จริงต้องเป็น HTTPS เท่านั้น เพราะรหัสผ่านและ token วิ่งใน request
