import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const apiMock = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
}))

vi.mock('@/services/api', () => ({ api: apiMock }))

import { useAuthStore } from './auth'

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    apiMock.get.mockReset()
    apiMock.post.mockReset()
    sessionStorage.clear()
  })

  it('stores the standardized login session', async () => {
    const response = {
      accessToken: 'jwt-token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: { id: '1', firstName: 'Ada', role: 'Admin' },
    }
    apiMock.post.mockResolvedValue(response)
    const auth = useAuthStore()

    await expect(auth.login({ email: 'ada@example.test', password: 'secret' })).resolves.toEqual(
      response,
    )
    expect(auth.status).toBe('authenticated')
    expect(auth.user).toEqual(response.user)
    expect(auth.role).toBe('Admin')
    expect(JSON.parse(sessionStorage.getItem('turkuvaz.auth.session'))).toEqual(response)
  })

  it('hydrates a stored session through /me only once', async () => {
    const stored = {
      accessToken: 'jwt-token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      user: null,
    }
    sessionStorage.setItem('turkuvaz.auth.session', JSON.stringify(stored))
    apiMock.get.mockResolvedValue({ id: '1', firstName: 'Grace', role: 'User' })
    const auth = useAuthStore()

    const [first, second] = await Promise.all([auth.hydrate(), auth.hydrate()])

    expect(first).toEqual(second)
    expect(apiMock.get).toHaveBeenCalledOnce()
    expect(apiMock.get).toHaveBeenCalledWith('/account/me', { skipAuthHandling: true })
    expect(auth.status).toBe('authenticated')
    expect(auth.role).toBe('User')
  })

  it('becomes anonymous without a stored token', async () => {
    const auth = useAuthStore()
    await expect(auth.hydrate()).resolves.toBeNull()
    expect(auth.status).toBe('anonymous')
    expect(apiMock.get).not.toHaveBeenCalled()
  })
})
