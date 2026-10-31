// The only place that calls fetch. Every URL is a relative /api/... path (ADR-presentation-technology D2).

export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

export interface RequestOptions {
  method?: 'GET' | 'POST'
  body?: unknown
  /** MFA token or access token; sent as `Authorization: Bearer`. */
  token?: string | null
  /** Called on a 401 answer to a request that carried a token. */
  onUnauthorized?: () => void
}

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, token, onUnauthorized } = options
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (token) headers.Authorization = `Bearer ${token}`

  const response = await fetch(path, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (response.ok) {
    return (response.status === 204 ? undefined : await response.json()) as T
  }

  if (response.status === 401 && token) onUnauthorized?.()

  let problem: ProblemDetails = {}
  try {
    problem = (await response.json()) as ProblemDetails
  } catch {
    // body was not JSON; fall back to the status text
  }
  throw new ApiError(
    response.status,
    problem.detail ?? problem.title ?? (response.statusText || `HTTP ${response.status}`),
    problem.errors,
  )
}
