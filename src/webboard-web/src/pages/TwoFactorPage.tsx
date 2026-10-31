import QRCode from 'qrcode'
import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router'
import type { AuthResponse, TwoFactorSetupResponse, VerifyTwoFactorRequest } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { Field, FormError, errorMessage, fieldErrorsOf } from '../components'

export default function TwoFactorPage() {
  const { mfaToken, isAuthenticated, twoFactorSetupRequired, requestWithMfa, completeLogin } = useAuth()
  const navigate = useNavigate()
  const [setup, setSetup] = useState<TwoFactorSetupResponse | null>(null)
  const [qr, setQr] = useState('')
  const [code, setCode] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<unknown>(null)

  // Setup must run once per MFA token: the API issues a fresh shared key on every call,
  // so a repeat call (e.g. React StrictMode re-running effects in dev) would invalidate the key on screen.
  const setupFor = useRef<string | null>(null)
  useEffect(() => {
    if (!mfaToken || !twoFactorSetupRequired || setupFor.current === mfaToken) return
    setupFor.current = mfaToken
    requestWithMfa<TwoFactorSetupResponse>('/api/auth/2fa/setup', { method: 'POST' })
      .then(async (res) => {
        // The otpauth URI contains the shared key, so the QR is drawn locally — never by an external service.
        setQr(await QRCode.toDataURL(res.otpAuthUri, { margin: 2, width: 200 }))
        setSetup(res)
      })
      .catch(setError)
  }, [mfaToken, twoFactorSetupRequired, requestWithMfa])

  if (!mfaToken && !isAuthenticated) return <Navigate to="/login" replace />

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const auth = await requestWithMfa<AuthResponse>('/api/auth/2fa/verify', {
        method: 'POST',
        body: { code } satisfies VerifyTwoFactorRequest,
      })
      completeLogin(auth)
      navigate('/')
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const fieldErrors = fieldErrorsOf(error)
  return (
    <div className="board">
      <h1 className="page-title">{twoFactorSetupRequired ? 'ตั้งค่าการยืนยันตัวตนสองขั้นตอน' : 'ยืนยันตัวตนสองขั้นตอน'}</h1>
      {twoFactorSetupRequired && (
        <div className="qr">
          {qr ? (
            <div className="qr-box">
              <img src={qr} alt="QR code สำหรับแอป Authenticator" width={150} height={150} />
            </div>
          ) : (
            <p className="hint">กำลังสร้าง QR…</p>
          )}
          <div className="qr-info">
            <ol className="steps">
              <li>เปิดแอป Authenticator แล้วสแกน QR นี้</li>
              <li>หรือพิมพ์รหัสนี้เอง:</li>
            </ol>
            {setup && <span className="key">{setup.sharedKey}</span>}
          </div>
        </div>
      )}
      <form className="card narrow" onSubmit={submit}>
        <Field
          label="รหัส 6 หลักจากแอป Authenticator"
          name="code"
          errorKey="Code"
          errors={fieldErrors}
          inputMode="numeric"
          maxLength={6}
          autoComplete="one-time-code"
          className="code"
          placeholder="000000"
          value={code}
          onChange={setCode}
        />
        {error !== null && Object.keys(fieldErrors).length === 0 && <FormError>{errorMessage(error)}</FormError>}
        <div className="row">
          <button className="btn btn-honey" type="submit" disabled={busy}>
            {twoFactorSetupRequired ? 'ยืนยันและเปิดใช้ 2FA' : 'เข้าสู่ระบบ'}
          </button>
          <Link className="link" to="/login">
            เริ่มใหม่
          </Link>
        </div>
      </form>
    </div>
  )
}
