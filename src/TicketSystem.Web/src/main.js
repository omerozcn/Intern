import { createApp, watch } from 'vue'

import 'bootstrap/dist/css/bootstrap.min.css'
import 'bootstrap-icons/font/bootstrap-icons.css'
import '@/styles/theme.css'

import App from '@/App.vue'
/* Imported for its side effect: aligns the reactive theme state with the
   attribute the inline script in index.html already wrote, and starts following
   the OS preference when the stored mode is 'system'. */
import '@/composables/useTheme'
import { vReveal } from '@/composables/useScrollReveal'
import { i18n, setLocale } from '@/i18n'
import router from '@/router'
import { configureApi } from '@/services/api'
import { pinia } from '@/stores'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'

const app = createApp(App)

app.use(pinia)
app.use(i18n)
app.use(router)
app.directive('reveal', vReveal)

const auth = useAuthStore(pinia)
const toast = useToastStore(pinia)
let handlingUnauthorized = false
let handlingForbidden = false

configureApi({
  getAccessToken: () => auth.accessToken,
  onUnauthorized: async () => {
    if (handlingUnauthorized) return
    handlingUnauthorized = true

    const returnUrl = router.currentRoute.value.meta.requiresAuth
      ? router.currentRoute.value.fullPath
      : undefined

    try {
      auth.clearSession()
      toast.warning(i18n.global.t('errors.sessionExpired'))
      await router.replace({ name: 'sign-in', query: returnUrl ? { returnUrl } : {} })
    } finally {
      // A navigation rejected by a guard must not latch this handler off for good.
      handlingUnauthorized = false
    }
  },
  onForbidden: async () => {
    toast.error(i18n.global.t('errors.forbidden'))
    if (handlingForbidden || router.currentRoute.value.name === 'dashboard') return
    handlingForbidden = true
    try {
      await router.replace({ name: 'dashboard' })
    } finally {
      handlingForbidden = false
    }
  },
})

function updateDocumentMetadata(route = router.currentRoute.value) {
  const title = route.meta.titleKey ? i18n.global.t(route.meta.titleKey) : null
  document.title = title
    ? `${title} · ${i18n.global.t('app.shortName')}`
    : i18n.global.t('app.name')
  document.documentElement.lang = i18n.global.locale.value
}

setLocale(i18n.global.locale.value)
router.afterEach((to) => updateDocumentMetadata(to))
watch(i18n.global.locale, () => updateDocumentMetadata())

app.mount('#app')
