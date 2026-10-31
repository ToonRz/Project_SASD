import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { apiRequest } from '../api/client'
import type { RegisterRequest, UserDto } from '../api/types'
import { Field, FormError, errorMessage, fieldErrorsOf } from '../components'

export default function RegisterPage() {
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<unknown>(null)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await apiRequest<UserDto>('/api/auth/register', {
        method: 'POST',
        body: { username, displayName, password } satisfies RegisterRequest,
      })
      navigate('/login')
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const fieldErrors = fieldErrorsOf(error)
  return (
    <div className="board">
      <h1 className="page-title">สมัครสมาชิก</h1>
      <form className="card narrow" onSubmit={submit}>
        <Field label="ชื่อผู้ใช้ (ใช้ login)" name="username" errorKey="Username" errors={fieldErrors} placeholder="3–30 ตัว: a-z 0-9 _" autoComplete="username" value={username} onChange={setUsername} />
        <Field label="ชื่อที่แสดง" name="displayName" errorKey="DisplayName" errors={fieldErrors} placeholder="ไม่เกิน 50 ตัวอักษร" value={displayName} onChange={setDisplayName} />
        <Field label="รหัสผ่าน" name="password" errorKey="Password" errors={fieldErrors} type="password" placeholder="อย่างน้อย 8 ตัว" autoComplete="new-password" value={password} onChange={setPassword} />
        {error !== null && Object.keys(fieldErrors).length === 0 && <FormError>{errorMessage(error)}</FormError>}
        <div className="row">
          <button className="btn btn-honey" type="submit" disabled={busy}>
            สมัครสมาชิก
          </button>
        </div>
      </form>
    </div>
  )
}
