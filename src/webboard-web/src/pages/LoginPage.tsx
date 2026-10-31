import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { apiRequest } from '../api/client'
import type { LoginRequest, LoginResponse } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { Field, FormError, errorMessage, fieldErrorsOf } from '../components'

export default function LoginPage() {
  const { startMfa } = useAuth()
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<unknown>(null)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const res = await apiRequest<LoginResponse>('/api/auth/login', {
        method: 'POST',
        body: { username, password } satisfies LoginRequest,
      })
      startMfa(res.mfaToken, res.twoFactorSetupRequired)
      navigate('/login/2fa')
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const fieldErrors = fieldErrorsOf(error)
  return (
    <div className="board">
      <h1 className="page-title">เข้าสู่ระบบ</h1>
      <form className="card narrow" onSubmit={submit}>
        <Field label="ชื่อผู้ใช้" name="username" errorKey="Username" errors={fieldErrors} autoComplete="username" value={username} onChange={setUsername} />
        <Field label="รหัสผ่าน" name="password" errorKey="Password" errors={fieldErrors} type="password" autoComplete="current-password" value={password} onChange={setPassword} />
        {error !== null && Object.keys(fieldErrors).length === 0 && <FormError>{errorMessage(error)}</FormError>}
        <div className="row">
          <button className="btn btn-honey" type="submit" disabled={busy}>
            ถัดไป
          </button>
        </div>
      </form>
    </div>
  )
}
