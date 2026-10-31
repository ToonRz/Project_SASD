import { Link, Route, Routes } from 'react-router'
import { AuthProvider, useAuth } from './auth/AuthContext'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import TopicPage from './pages/TopicPage'
import TopicsPage from './pages/TopicsPage'
import TwoFactorPage from './pages/TwoFactorPage'
import './styles/app.css'

function SiteHeader() {
  const { user, isAuthenticated, logout } = useAuth()
  return (
    <header className="site">
      <Link className="brand" to="/">
        Webboard
      </Link>
      <div className="acts">
        {isAuthenticated ? (
          <>
            <span className="me">
              สวัสดี <b>{user?.displayName}</b>
            </span>
            <button className="btn ghost sm" type="button" onClick={logout}>
              ออกจากระบบ
            </button>
          </>
        ) : (
          <>
            <Link className="btn ghost sm" to="/login">
              เข้าสู่ระบบ
            </Link>
            <Link className="btn sm" to="/register">
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
      <div className="layer">
        <SiteHeader />
        <main>
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
