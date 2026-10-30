# Webboard — Domain Language

อภิธานศัพท์ของโดเมน ใช้คำเหล่านี้ให้ตรงกันทั้งในโค้ด (ชื่อ class / table / endpoint), เอกสาร และการคุยกันในทีม

## Language

**Topic** (กระทู้):
โพสต์หลักที่ผู้ใช้ตั้งขึ้น มีหัวข้อ (`Title`) และเนื้อหา (`Body`) เป็นจุดเริ่มของการสนทนา
_Avoid_: Post, Thread, Question (ใช้คำเดียวคือ Topic)

**Comment** (ความเห็น):
ข้อความที่ตอบใน **Topic** หนึ่ง ทุก Comment อยู่ใต้ Topic เดียวเสมอ
_Avoid_: Reply (ใช้เฉพาะความหมายด้านล่าง), Answer, Post

**Reply** (ความเห็นย่อย):
**Comment** ที่ตอบ Comment อื่น — ไม่ใช่ entity แยก แต่เป็น Comment ที่มี `ParentCommentId` ไม่ว่าง
_Avoid_: Sub-comment, Child post

**Root Comment**:
**Comment** ที่ตอบ **Topic** โดยตรง (`ParentCommentId` ว่าง)

**Tag** (แท็ก):
ป้ายคำสั้นๆ ที่ติดให้ **Topic** ได้หลายอัน ใช้ค้นหา/กรอง ไม่มีลำดับชั้น
_Avoid_: Category, Label (Category ชวนให้เข้าใจว่าเป็น **Room**)

**Room** (ห้อง):
กลุ่มของ **Topic** — หนึ่ง Topic อยู่ได้หนึ่ง Room เท่านั้น (ต่างจาก **Tag** ที่ติดได้หลายอัน)
_Avoid_: Board, Category, Forum, Group

**Author** (ผู้เขียน):
**Member** ที่เขียน **Topic** หรือ **Comment** นั้น — มาจาก access token เสมอ ไม่ใช่ชื่อที่พิมพ์มาใน request
_Avoid_: Poster, Owner

### Accounts

**Member** (สมาชิก):
ผู้ใช้ที่มีบัญชี (`Username`, `DisplayName`, รหัสผ่าน, 2FA) ในโค้ดเป็น class `User` บนตาราง `app_user` (`user` เป็นคำสงวนของ PostgreSQL)
_Avoid_: Account, Customer

**Reader** (ผู้อ่าน):
ผู้ใช้ที่ไม่ได้ login — อ่านได้ เขียนไม่ได้
_Avoid_: Guest, Anonymous user

**Username** / **DisplayName**:
`Username` ใช้ login (ตัวเล็ก, ไม่ซ้ำ, เปลี่ยนไม่ได้) ส่วน `DisplayName` คือชื่อที่แสดงบนกระทู้และความเห็น
_Avoid_: Login name, Nickname

**Password Hash**:
ผลของ PBKDF2-HMAC-SHA256 ที่เก็บแทนรหัสผ่าน ถอดกลับเป็นรหัสผ่านไม่ได้
_Avoid_: Encrypted password (ไม่ได้เข้ารหัส — เป็น hash)

**Two-Factor (2FA)** / **TOTP**:
ขั้นที่สองของ login: รหัส 6 หลักที่แอป Authenticator สร้างใหม่ทุก 30 วินาทีจาก **Shared Key**
_Avoid_: OTP (ชวนนึกถึง OTP ทาง SMS ซึ่งระบบนี้ไม่มี)

**Shared Key**:
Secret ของ TOTP ที่ระบบกับแอป Authenticator ถือร่วมกัน ได้มาตอนตั้ง 2FA (สแกน QR) เก็บในฐานข้อมูลแบบเข้ารหัส
_Avoid_: Seed, 2FA password

**MFA Token**:
Token อายุ 5 นาทีที่ได้หลังรหัสผ่านถูก ใช้ได้กับขั้น 2FA เท่านั้น
_Avoid_: Temp token, Pre-auth token

**Access Token**:
JWT อายุ 60 นาทีที่ได้หลังผ่าน 2FA ส่งใน header `Authorization: Bearer` เพื่อเรียก endpoint ที่ต้อง login
_Avoid_: Session, Cookie

## Flagged ambiguities

**"Reply" vs "Comment"**:
ในภาษาพูด "ตอบกระทู้" กับ "ตอบความเห็น" ใช้คำเดียวกัน แต่ในระบบ ทั้งคู่คือ **Comment**
**Resolved:** มี entity เดียวคือ `Comment` ความต่างอยู่ที่ `ParentCommentId` เท่านั้น — ไม่สร้างตาราง `Reply` แยก

**"Tag" vs "Room"**:
ทั้งคู่ใช้จัดกลุ่มกระทู้
**Resolved:** **Room** = ที่อยู่ของ Topic (หนึ่งต่อหนึ่ง, บังคับเลือกเมื่อเปิดใช้ฟีเจอร์ Room), **Tag** = คำอธิบายเพิ่มเติม (หลายต่อหลาย, ไม่บังคับ)

**"encrypt รหัสผ่านเป็น SHA-256"**:
SHA-256 เป็น hash ไม่ใช่การเข้ารหัส และ SHA-256 ครั้งเดียวไม่พอสำหรับรหัสผ่าน
**Resolved:** เก็บเป็น **Password Hash** แบบ PBKDF2-HMAC-SHA256 (มี salt, 600,000 รอบ) — ดู ADR-auth-password-2fa
