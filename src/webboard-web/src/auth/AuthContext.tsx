import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from 'react'
import { useNavigate } from 'react-router'
import { apiRequest } from '../api/client'
import type { AuthResponse, UserDto } from '../api/types'

// Tokens live in React state (and a ref for the fetch wrapper) only — never storage, cookies or URLs (ADR D3).

interface AuthState {
  accessToken: string | null
  mfaToken: string | null
  twoFactorSetupRequired: boolean
  user: UserDto | null
}

const empty: AuthState = { accessToken: null, mfaToken: null, twoFactorSetupRequired: false, user: null }

export interface AuthContextValue {
  user: UserDto | null
  isAuthenticated: boolean
  mfaToken: string | null
  twoFactorSetupRequired: boolean
  /** Request authenticated with the access token; a 401 logs out and goes to /login. */
  request: <T>(path: string, init?: { method?: 'GET' | 'POST'; body?: unknown }) => Promise<T>
  /** Request authenticated with the MFA token (only valid on /api/auth/2fa/*). */
  requestWithMfa: <T>(path: string, init?: { method?: 'GET' | 'POST'; body?: unknown }) => Promise<T>
  startMfa: (mfaToken: string, twoFactorSetupRequired: boolean) => void
  completeLogin: (auth: AuthResponse) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(empty)
  const stateRef = useRef<AuthState>(empty)
  const navigate = useNavigate()

  const update = useCallback((next: AuthState) => {
    stateRef.current = next
    setState(next)
  }, [])

  const logout = useCallback(() => {
    update(empty)
    navigate('/')
  }, [navigate, update])

  const value = useMemo<AuthContextValue>(
    () => ({
      user: state.user,
      isAuthenticated: state.accessToken !== null,
      mfaToken: state.mfaToken,
      twoFactorSetupRequired: state.twoFactorSetupRequired,
      request: (path, init) =>
        apiRequest(path, {
          ...init,
          token: stateRef.current.accessToken,
          onUnauthorized: () => {
            update(empty)
            navigate('/login')
          },
        }),
      // A 401 here can simply mean a wrong code, so it is shown as an error rather than redirecting.
      requestWithMfa: (path, init) => apiRequest(path, { ...init, token: stateRef.current.mfaToken }),
      startMfa: (mfaToken, twoFactorSetupRequired) =>
        update({ ...empty, mfaToken, twoFactorSetupRequired }),
      completeLogin: (auth) =>
        update({ accessToken: auth.accessToken, mfaToken: null, twoFactorSetupRequired: false, user: auth.user }),
      logout,
    }),
    [state, logout, navigate, update],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>')
  return ctx
}
