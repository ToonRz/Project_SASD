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
ชื่อที่แสดงว่าใครเขียน **Topic** หรือ **Comment** ใน MVP เป็นแค่ display name ที่ผู้ใช้กรอก ยังไม่ผูกกับบัญชีผู้ใช้
_Avoid_: User, Member (ใช้เมื่อมีระบบบัญชีแล้วเท่านั้น)

## Flagged ambiguities

**"Reply" vs "Comment"**:
ในภาษาพูด "ตอบกระทู้" กับ "ตอบความเห็น" ใช้คำเดียวกัน แต่ในระบบ ทั้งคู่คือ **Comment**
**Resolved:** มี entity เดียวคือ `Comment` ความต่างอยู่ที่ `ParentCommentId` เท่านั้น — ไม่สร้างตาราง `Reply` แยก

**"Tag" vs "Room"**:
ทั้งคู่ใช้จัดกลุ่มกระทู้
**Resolved:** **Room** = ที่อยู่ของ Topic (หนึ่งต่อหนึ่ง, บังคับเลือกเมื่อเปิดใช้ฟีเจอร์ Room), **Tag** = คำอธิบายเพิ่มเติม (หลายต่อหลาย, ไม่บังคับ)
