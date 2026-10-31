import { useEffect, useState } from 'react'
import { Link, Route, Routes } from 'react-router'
import { AuthProvider, useAuth } from './auth/AuthContext'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import TopicPage from './pages/TopicPage'
import TopicsPage from './pages/TopicsPage'
import TwoFactorPage from './pages/TwoFactorPage'
import { BrandMark } from './components'
import './styles/app.css'

type Theme = 'light' | 'dark'

function systemTheme(): Theme {
  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

function initialTheme(): Theme {
  try {
    const saved = localStorage.getItem('webboard-theme')
    if (saved === 'light' || saved === 'dark') return saved
  } catch {
    /* storage can be blocked */
  }
  return systemTheme()
}

// Applied at load, before the first paint, so a saved choice never flashes the other theme.
document.documentElement.dataset.theme = initialTheme()

/** Overrides the OS colour scheme for this page; the stored value is only a per-viewer convenience. */
function ThemeToggle() {
  const [theme, setTheme] = useState<Theme>(initialTheme)
  useEffect(() => {
    document.documentElement.dataset.theme = theme
  }, [theme])
  function flip() {
    const next: Theme = theme === 'dark' ? 'light' : 'dark'
    setTheme(next)
    try {
      localStorage.setItem('webboard-theme', next)
    } catch {
      /* storage can be blocked */
    }
  }
  return (
    <button className="icon-btn" type="button" onClick={flip} aria-label={theme === 'dark' ? 'สลับเป็นธีมสว่าง' : 'สลับเป็นธีมมืด'}>
      <svg className="i" viewBox="0 0 24 24" aria-hidden="true">
        {theme === 'dark' ? (
          <path d="M12 3.5v2M12 18.5v2M3.5 12h2M18.5 12h2M5.9 5.9l1.4 1.4M16.7 16.7l1.4 1.4M18.1 5.9l-1.4 1.4M7.3 16.7l-1.4 1.4M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8z" />
        ) : (
          <path d="M20 14.5A8 8 0 0 1 9.5 4a8 8 0 1 0 10.5 10.5z" />
        )}
      </svg>
    </button>
  )
}

function SiteHeader() {
  const { user, isAuthenticated, logout } = useAuth()
  return (
    <header className="site">
      <Link className="brand" to="/">
        <BrandMark />
        <span>Webboard</span>
      </Link>
      <div className="acts">
        <ThemeToggle />
        {isAuthenticated ? (
          <>
            <span className="me">
              สวัสดี <b>{user?.displayName}</b>
            </span>
            <button className="btn btn-white btn-sm" type="button" onClick={logout}>
              ออกจากระบบ
            </button>
          </>
        ) : (
          <>
            <Link className="btn btn-white btn-sm" to="/login">
              เข้าสู่ระบบ
            </Link>
            <Link className="btn btn-primary btn-sm" to="/register">
              สมัครสมาชิก
            </Link>
          </>
        )}
      </div>
    </header>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <div className="app">
        <SiteHeader />
        <main className="view">
          <Routes>
            <Route path="/" element={<TopicsPage />} />
            <Route path="/topics/:id" element={<TopicPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/login/2fa" element={<TwoFactorPage />} />
            <Route path="*" element={<p className="board">ไม่พบหน้านี้</p>} />
          </Routes>
        </main>
      </div>
    </AuthProvider>
  )
}
