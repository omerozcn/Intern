import { computed, ref } from 'vue'
import { defineStore } from 'pinia'

import { api } from '@/services/api'

const STORAGE_KEY = 'turkuvaz.auth.session'
let hydrationPromise = null

export const useAuthStore = defineStore('auth', () => {
  const status = ref('unknown')
  const session = ref(null)

  const user = computed(() => session.value?.user ?? null)
  const accessToken = computed(() => session.value?.accessToken ?? null)
  const expiresAt = computed(() => session.value?.expiresAt ?? null)
  const role = computed(() => user.value?.role ?? null)
  const userRole = computed(() => role.value)
  const isAuthenticated = computed(() => status.value === 'authenticated')

  function restoreStoredSession() {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    if (!raw) return null

    try {
      const stored = JSON.parse(raw)
      if (!stored?.accessToken) return null
      if (stored.expiresAt && Date.parse(stored.expiresAt) <= Date.now()) return null
      return stored
    } catch {
      return null
    }
  }

  function persistSession(nextSession) {
    session.value = nextSession
    status.value = 'authenticated'
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(nextSession))
  }

  function clearSession() {
    session.value = null
    status.value = 'anonymous'
    sessionStorage.removeItem(STORAGE_KEY)
    sessionStorage.removeItem('token')
  }

  async function login(credentials) {
    const response = await api.post('/account/login', credentials, {
      skipAuth: true,
      skipAuthHandling: true,
    })
    const nextSession = normalizeSession(response)

    if (!nextSession.accessToken) {
      throw new Error('Oturum bilgisi sunucudan alınamadı.')
    }

    persistSession(nextSession)
    return nextSession
  }

  async function hydrate({ force = false } = {}) {
    if (!force && status.value !== 'unknown') return user.value
    if (!force && hydrationPromise) return hydrationPromise

    hydrationPromise = (async () => {
      const stored = session.value?.accessToken ? session.value : restoreStoredSession()
      if (!stored) {
        clearSession()
        return null
      }

      session.value = stored

      try {
        const response = await api.get('/account/me', { skipAuthHandling: true })
        const hydratedUser = response?.user ?? response
        persistSession({ ...stored, user: hydratedUser })
        return hydratedUser
      } catch {
        clearSession()
        return null
      }
    })()

    try {
      return await hydrationPromise
    } finally {
      hydrationPromise = null
    }
  }

  async function logout() {
    try {
      if (accessToken.value) {
        await api.post('/account/logout', undefined, { skipAuthHandling: true })
      }
    } catch {
      // The local session must still end when the server is temporarily unreachable.
    } finally {
      clearSession()
    }
  }

  async function fetchUserRole() {
    await hydrate()
    return role.value
  }

  function updateUser(nextUser) {
    if (!session.value) return
    persistSession({ ...session.value, user: { ...session.value.user, ...nextUser } })
  }

  return {
    status,
    session,
    user,
    accessToken,
    expiresAt,
    role,
    userRole,
    isAuthenticated,
    login,
    hydrate,
    logout,
    clearSession,
    fetchUserRole,
    updateUser,
  }
})

function normalizeSession(response = {}) {
  return {
    accessToken: response.accessToken ?? response.token ?? response.AccessToken ?? null,
    expiresAt: response.expiresAt ?? response.ExpiresAt ?? null,
    user: response.user ?? response.User ?? null,
  }
}
