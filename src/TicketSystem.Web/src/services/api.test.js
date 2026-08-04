import { beforeEach, describe, expect, it, vi } from 'vitest'

import { ApiError, api, configureApi } from './api'

describe('api client', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    configureApi({
      getAccessToken: () => 'test-token',
      onUnauthorized: () => {},
      onForbidden: () => {},
    })
  })

  it('adds the bearer token and parses JSON responses', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ id: 7 }), {
        status: 200,
        headers: { 'content-type': 'application/json' },
      }),
    )

    await expect(api.get('/tickets')).resolves.toEqual({ id: 7 })
    const [, options] = fetchMock.mock.calls[0]
    expect(options.headers.get('Authorization')).toBe('Bearer test-token')
  })

  it('returns null for a 204 response', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 204 }))
    await expect(api.delete('/tickets/1')).resolves.toBeNull()
  })

  it('normalizes ProblemDetails and invokes the centralized 401 handler', async () => {
    const onUnauthorized = vi.fn()
    configureApi({ onUnauthorized })
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          type: 'https://example.test/problem',
          title: 'Unauthorized',
          status: 401,
          detail: 'The session is invalid.',
          errors: { token: ['Expired'] },
        }),
        { status: 401, headers: { 'content-type': 'application/problem+json' } },
      ),
    )

    const error = await api.get('/private').catch((reason) => reason)
    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      status: 401,
      message: 'The session is invalid.',
      errors: { token: ['Expired'] },
    })
    expect(onUnauthorized).toHaveBeenCalledOnce()
  })
})
