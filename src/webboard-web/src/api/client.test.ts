import { http, HttpResponse } from 'msw'
import { describe, expect, it, vi } from 'vitest'
import { ApiError, apiRequest } from './client'
import { problem, server } from '../test/server'

describe('apiRequest', () => {
  it('adds the Bearer header and parses JSON', async () => {
    server.use(
      http.get('/api/auth/me', ({ request }) => HttpResponse.json({ auth: request.headers.get('Authorization') })),
    )
    await expect(apiRequest('/api/auth/me', { token: 't1' })).resolves.toEqual({ auth: 'Bearer t1' })
  })

  it('calls onUnauthorized once on 401 when a token was sent', async () => {
    server.use(http.get('/api/auth/me', () => problem(401, 'Unauthorized')))
    const onUnauthorized = vi.fn()
    await expect(apiRequest('/api/auth/me', { token: 't1', onUnauthorized })).rejects.toBeInstanceOf(ApiError)
    expect(onUnauthorized).toHaveBeenCalledTimes(1)
  })

  it('does not call onUnauthorized for a 401 without a token (bad credentials)', async () => {
    server.use(http.post('/api/auth/login', () => problem(401, 'Invalid username or password.')))
    const onUnauthorized = vi.fn()
    await expect(
      apiRequest('/api/auth/login', { method: 'POST', body: {}, onUnauthorized }),
    ).rejects.toMatchObject({ status: 401, message: 'Invalid username or password.' })
    expect(onUnauthorized).not.toHaveBeenCalled()
  })

  it('turns validation Problem Details into field errors', async () => {
    server.use(
      http.post('/api/topics', () =>
        HttpResponse.json(
          { status: 400, title: 'One or more validation errors occurred.', errors: { Title: ['too long'] } },
          { status: 400 },
        ),
      ),
    )
    const err = await apiRequest('/api/topics', { method: 'POST', body: {}, token: 't' }).catch((e: unknown) => e)
    expect(err).toBeInstanceOf(ApiError)
    expect((err as ApiError).fieldErrors).toEqual({ Title: ['too long'] })
  })
})
