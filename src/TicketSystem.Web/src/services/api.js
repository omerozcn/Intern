const DEFAULT_TIMEOUT_MS = 15_000
const API_BASE_URL = normalizeBaseUrl(import.meta.env.VITE_API_BASE_URL || '/api')

let accessTokenProvider = () => null
let unauthorizedHandler = null
let forbiddenHandler = null

export class ApiError extends Error {
  constructor(problem = {}) {
    const message = problem.detail || problem.title || problem.message || 'İstek tamamlanamadı.'
    super(message)
    this.name = 'ApiError'
    this.status = problem.status || 0
    this.title = problem.title || 'Request failed'
    this.detail = problem.detail || message
    this.errors = problem.errors || {}
    this.traceId = problem.traceId || problem.extensions?.traceId || null
    this.problem = problem
  }
}

export function configureApi({ getAccessToken, onUnauthorized, onForbidden } = {}) {
  if (typeof getAccessToken === 'function') accessTokenProvider = getAccessToken
  if (typeof onUnauthorized === 'function') unauthorizedHandler = onUnauthorized
  if (typeof onForbidden === 'function') forbiddenHandler = onForbidden
}

async function request(method, path, body, options = {}) {
  const {
    headers: customHeaders,
    signal: externalSignal,
    timeout = DEFAULT_TIMEOUT_MS,
    skipAuth = false,
    skipAuthHandling = false,
    ...fetchOptions
  } = options

  const controller = new AbortController()
  const abortFromExternalSignal = () => controller.abort(externalSignal?.reason)
  if (externalSignal?.aborted) {
    controller.abort(externalSignal.reason)
  } else {
    externalSignal?.addEventListener('abort', abortFromExternalSignal, { once: true })
  }
  const timeoutId = globalThis.setTimeout(() => controller.abort('timeout'), timeout)

  const headers = new Headers(customHeaders || {})
  headers.set('Accept', 'application/json')

  const token = skipAuth ? null : accessTokenProvider()
  if (token) headers.set('Authorization', `Bearer ${token}`)

  let payload = body
  const hasBody = body !== undefined && body !== null && method !== 'GET' && method !== 'HEAD'
  const isFormData = typeof FormData !== 'undefined' && body instanceof FormData

  if (hasBody && !isFormData && typeof body !== 'string' && !(body instanceof Blob)) {
    headers.set('Content-Type', 'application/json')
    payload = JSON.stringify(body)
  }

  try {
    const response = await fetch(resolveUrl(path), {
      method,
      headers,
      body: hasBody ? payload : undefined,
      signal: controller.signal,
      credentials: 'same-origin',
      ...fetchOptions,
    })

    if (response.status === 204) return null

    const data = await parseResponse(response)

    if (!response.ok) {
      const error = toApiError(response, data)
      if (!skipAuthHandling && response.status === 401) await unauthorizedHandler?.(error)
      if (!skipAuthHandling && response.status === 403) await forbiddenHandler?.(error)
      throw error
    }

    return data
  } catch (error) {
    if (error instanceof ApiError) throw error

    if (error?.name === 'AbortError' || controller.signal.aborted) {
      const timedOut = !externalSignal?.aborted
      throw new ApiError({
        status: 0,
        title: timedOut ? 'Request timeout' : 'Request cancelled',
        detail: timedOut
          ? 'Sunucu 15 saniye içinde yanıt vermedi. Lütfen tekrar deneyin.'
          : 'İstek iptal edildi.',
      })
    }

    throw new ApiError({
      status: 0,
      title: 'Network error',
      detail: 'Sunucuya ulaşılamadı. Bağlantınızı kontrol edip tekrar deneyin.',
    })
  } finally {
    globalThis.clearTimeout(timeoutId)
    externalSignal?.removeEventListener('abort', abortFromExternalSignal)
  }
}

async function parseResponse(response) {
  const contentType = response.headers.get('content-type') || ''

  if (contentType.includes('json')) {
    try {
      return await response.json()
    } catch {
      return null
    }
  }

  const text = await response.text()
  return text || null
}

function toApiError(response, data) {
  if (data && typeof data === 'object') {
    return new ApiError({
      ...data,
      status: data.status || response.status,
      title: data.title || response.statusText,
    })
  }

  return new ApiError({
    status: response.status,
    title: response.statusText || 'Request failed',
    detail: typeof data === 'string' && data ? data : 'İstek tamamlanamadı.',
  })
}

function normalizeBaseUrl(value) {
  return String(value || '').replace(/\/+$/, '')
}

function resolveUrl(path) {
  if (/^https?:\/\//i.test(path)) return path

  let normalizedPath = String(path || '').trim()
  if (!normalizedPath.startsWith('/')) normalizedPath = `/${normalizedPath}`

  if (API_BASE_URL.endsWith('/api') && normalizedPath.toLowerCase().startsWith('/api/')) {
    normalizedPath = normalizedPath.slice(4)
  }

  return `${API_BASE_URL}${normalizedPath}`
}

export const api = {
  get(path, options) {
    return request('GET', path, undefined, options)
  },
  post(path, body, options) {
    return request('POST', path, body, options)
  },
  put(path, body, options) {
    return request('PUT', path, body, options)
  },
  patch(path, body, options) {
    return request('PATCH', path, body, options)
  },
  delete(path, bodyOrOptions, maybeOptions) {
    const hasBody = maybeOptions !== undefined
    return request(
      'DELETE',
      path,
      hasBody ? bodyOrOptions : undefined,
      hasBody ? maybeOptions : bodyOrOptions,
    )
  },
}

export const apiConfig = Object.freeze({
  baseUrl: API_BASE_URL,
  timeout: DEFAULT_TIMEOUT_MS,
})
