process.env.TZ = 'Asia/Bangkok'

import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterAll, afterEach, beforeAll } from 'vitest'
import { requestedPaths, server } from './server'

// Record every request the app makes so tests can assert it stays under /api/.
server.events.on('request:start', ({ request }) => {
  requestedPaths.push(new URL(request.url).pathname)
})

beforeAll(() => server.listen({ onUnhandledFrame: 'error' }))
afterEach(() => {
  server.resetHandlers()
  cleanup()
  requestedPaths.length = 0
})
afterAll(() => server.close())
