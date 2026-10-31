import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { problem, requestedPaths, server, topicDetail, topicSummary, user } from './test/server'
import { renderApp } from './test/render'

async function loginAs() {
  server.use(
    http.post('/api/auth/login', () => HttpResponse.json({ mfaToken: 'mfa-1', twoFactorSetupRequired: false })),
    http.post('/api/auth/2fa/verify', () => HttpResponse.json({ accessToken: 'access-1', expiresIn: 3600, user })),
  )
  const ue = userEvent.setup()
  await ue.click(await screen.findByRole('link', { name: 'เข้าสู่ระบบ' }))
  await ue.type(screen.getByLabelText('ชื่อผู้ใช้'), 'ton')
  await ue.type(screen.getByLabelText('รหัสผ่าน'), 'webboard123')
  await ue.click(screen.getByRole('button', { name: 'ถัดไป' }))
  await ue.type(await screen.findByLabelText(/รหัส 6 หลัก/), '123456')
  await ue.click(screen.getByRole('button', { name: 'เข้าสู่ระบบ' }))
  await screen.findByRole('button', { name: 'ออกจากระบบ' })
  return ue
}

describe('anonymous visitor', () => {
  it('sees no new-topic form on the topic list', async () => {
    renderApp('/')
    await screen.findByText('Hello board')
    expect(screen.queryByRole('button', { name: 'ตั้งกระทู้' })).not.toBeInTheDocument()
    expect(screen.queryByLabelText('หัวข้อกระทู้')).not.toBeInTheDocument()
  })

  it('sees no comment form on a topic page', async () => {
    renderApp('/topics/1')
    await screen.findByText('First post')
    expect(screen.queryByRole('button', { name: 'ส่งความเห็น' })).not.toBeInTheDocument()
    expect(screen.queryByLabelText('ความเห็น')).not.toBeInTheDocument()
  })
})

describe('login with 2FA', () => {
  it('shows a QR image and the shared key when setup is required', async () => {
    server.use(
      http.post('/api/auth/login', () => HttpResponse.json({ mfaToken: 'mfa-1', twoFactorSetupRequired: true })),
      http.post('/api/auth/2fa/setup', ({ request }) => {
        expect(request.headers.get('Authorization')).toBe('Bearer mfa-1')
        return HttpResponse.json({
          sharedKey: 'JBSWY3DPEHPK3PXP',
          otpAuthUri:
            'otpauth://totp/Webboard:ton?secret=JBSWY3DPEHPK3PXP&issuer=Webboard&algorithm=SHA1&digits=6&period=30',
        })
      }),
    )
    renderApp('/login')
    const ue = userEvent.setup()
    await ue.type(screen.getByLabelText('ชื่อผู้ใช้'), 'ton')
    await ue.type(screen.getByLabelText('รหัสผ่าน'), 'webboard123')
    await ue.click(screen.getByRole('button', { name: 'ถัดไป' }))

    const img = await screen.findByAltText(/QR/)
    expect(img.getAttribute('src')).toMatch(/^data:image\/png/)
    expect(screen.getByText('JBSWY3DPEHPK3PXP')).toBeInTheDocument()
  })

  it('shows the error and stays on the code form when the code is wrong', async () => {
    server.use(
      http.post('/api/auth/login', () => HttpResponse.json({ mfaToken: 'mfa-1', twoFactorSetupRequired: false })),
      http.post('/api/auth/2fa/verify', () => problem(401, 'Invalid or already used code.')),
    )
    renderApp('/login')
    const ue = userEvent.setup()
    await ue.type(screen.getByLabelText('ชื่อผู้ใช้'), 'ton')
    await ue.type(screen.getByLabelText('รหัสผ่าน'), 'webboard123')
    await ue.click(screen.getByRole('button', { name: 'ถัดไป' }))
    await ue.type(await screen.findByLabelText(/รหัส 6 หลัก/), '000000')
    await ue.click(screen.getByRole('button', { name: 'เข้าสู่ระบบ' }))
    expect(await screen.findByText('Invalid or already used code.')).toBeInTheDocument()
    expect(screen.getByLabelText(/รหัส 6 หลัก/)).toBeInTheDocument()
  })
})

describe('logged-in member', () => {
  it('sees the forms after login and loses them after logout', async () => {
    renderApp('/')
    const ue = await loginAs()
    await ue.click(screen.getByRole('link', { name: 'Webboard' }))
    expect(await screen.findByLabelText('หัวข้อกระทู้')).toBeInTheDocument()
    await ue.click(screen.getByRole('button', { name: 'ออกจากระบบ' }))
    await waitFor(() => expect(screen.queryByLabelText('หัวข้อกระทู้')).not.toBeInTheDocument())
    expect(screen.getByRole('link', { name: 'เข้าสู่ระบบ' })).toBeInTheDocument()
  })

  it('sends the access token when creating a topic', async () => {
    let auth: string | null = null
    server.use(
      http.post('/api/topics', async ({ request }) => {
        auth = request.headers.get('Authorization')
        const body = (await request.json()) as { title: string }
        return HttpResponse.json(topicSummary({ id: 2, title: body.title }), { status: 201 })
      }),
    )
    renderApp('/')
    const ue = await loginAs()
    await ue.type(await screen.findByLabelText('หัวข้อกระทู้'), 'New one')
    await ue.type(screen.getByLabelText('เนื้อหา'), 'Body')
    await ue.click(screen.getByRole('button', { name: 'ตั้งกระทู้' }))
    await waitFor(() => expect(auth).toBe('Bearer access-1'))
  })

  it('drops the token and goes to /login when the API answers 401', async () => {
    server.use(http.post('/api/topics', () => problem(401, 'Unauthorized')))
    renderApp('/')
    const ue = await loginAs()
    await ue.type(await screen.findByLabelText('หัวข้อกระทู้'), 'x')
    await ue.type(screen.getByLabelText('เนื้อหา'), 'y')
    await ue.click(screen.getByRole('button', { name: 'ตั้งกระทู้' }))
    expect(await screen.findByRole('button', { name: 'ถัดไป' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'ออกจากระบบ' })).not.toBeInTheDocument()
  })

  it('posts a comment and shows it', async () => {
    const comment = {
      id: 9,
      topicId: 1,
      parentCommentId: null,
      authorName: 'Ton',
      body: 'Nice',
      createdAt: '2026-10-31T09:00:00Z',
    }
    let posted = false
    server.use(
      http.get('/api/topics/1', () => HttpResponse.json(topicDetail({ comments: posted ? [comment] : [] }))),
      http.post('/api/topics/1/comments', () => {
        posted = true
        return HttpResponse.json(comment, { status: 201 })
      }),
    )
    renderApp('/topics/1')
    const ue = await loginAs()
    await ue.click(await screen.findByRole('link', { name: 'Hello board' }))
    await ue.type(await screen.findByLabelText('ความเห็น'), 'Nice')
    await ue.click(screen.getByRole('button', { name: 'ส่งความเห็น' }))
    expect(await screen.findByText('Nice')).toBeInTheDocument()
  })
})

describe('rendering of API data', () => {
  it('renders a <script> title as text, not markup', async () => {
    const evil = '<script>alert(1)</script>'
    server.use(
      http.get('/api/topics', () => HttpResponse.json([topicSummary({ title: evil })])),
      http.get('/api/topics/1', () => HttpResponse.json(topicDetail({ title: evil, body: evil }))),
    )
    const first = renderApp('/')
    expect(await screen.findByText(evil)).toBeInTheDocument()
    expect(first.container.querySelector('script')).toBeNull()
    first.unmount()
    const second = renderApp('/topics/1')
    expect(await screen.findByRole('heading', { name: evil })).toBeInTheDocument()
    expect(second.container.querySelector('script')).toBeNull()
  })

  it('shows createdAt (UTC) in local time', async () => {
    renderApp('/')
    const row = (await screen.findByText('Hello board')).closest('li')!
    // setup.ts pins TZ=Asia/Bangkok (UTC+7): 08:00Z -> 15:00
    expect(within(row).getByText(/15:00/)).toBeInTheDocument()
  })

  it('only calls URLs under /api/', async () => {
    renderApp('/')
    await screen.findByText('Hello board')
    expect(requestedPaths.length).toBeGreaterThan(0)
    expect(requestedPaths.every((u) => u.startsWith('/api/'))).toBe(true)
  })

  it('shows the API message for a missing topic', async () => {
    server.use(http.get('/api/topics/99', () => problem(404, 'Topic not found.')))
    renderApp('/topics/99')
    expect(await screen.findByText('Topic not found.')).toBeInTheDocument()
  })
})
