<template>
  <div class="app-shell">
    <a class="skip-link" href="#main-content">{{ t('common.skipToContent') }}</a>

    <aside class="sidebar d-none d-lg-flex" :aria-label="t('nav.primary')">
      <RouterLink class="brand" :to="{ name: 'dashboard' }" :aria-label="t('nav.home')">
        <img class="brand__logo" :src="logoUrl" alt="Turkuvaz" />
        <span class="brand__copy">
          <strong>{{ t('app.shortName') }}</strong>
          <small>{{ t('app.tagline') }}</small>
        </span>
      </RouterLink>

      <nav class="sidebar__nav" :aria-label="t('nav.primary')">
        <RouterLink
          v-for="link in visibleLinks"
          :key="link.name"
          class="sidebar-link"
          :to="{ name: link.name }"
        >
          <i :class="['bi', link.icon]" aria-hidden="true"></i>
          <span>{{ t(link.labelKey) }}</span>
        </RouterLink>
      </nav>

      <div class="sidebar__footer">
        <RouterLink class="user-card" :to="{ name: 'profile' }">
          <span class="avatar" aria-hidden="true">{{ initials }}</span>
          <span class="user-card__copy">
            <strong>{{ displayName }}</strong>
            <small>{{ roleLabel }}</small>
          </span>
          <i class="bi bi-chevron-right" aria-hidden="true"></i>
        </RouterLink>
      </div>
    </aside>

    <div class="app-shell__main">
      <header class="topbar">
        <button
          ref="drawerTrigger"
          class="icon-button d-lg-none"
          type="button"
          :aria-expanded="drawerOpen"
          aria-controls="mobile-navigation"
          :aria-label="t('nav.openMenu')"
          @click="drawerOpen = true"
        >
          <i class="bi bi-list" aria-hidden="true"></i>
        </button>

        <div class="topbar__title">
          <span>{{ currentPageTitle }}</span>
          <small class="d-none d-md-block">{{ formattedToday }}</small>
        </div>

        <div class="topbar__actions">
          <label class="visually-hidden" for="app-locale">{{ t('common.language') }}</label>
          <select
            id="app-locale"
            class="form-select form-select-sm locale-select"
            :value="locale"
            @change="changeLocale"
          >
            <option v-for="item in supportedLocales" :key="item.code" :value="item.code">
              {{ item.shortLabel }}
            </option>
          </select>

          <RouterLink class="icon-button" :to="{ name: 'profile' }" :aria-label="t('nav.profile')">
            <i class="bi bi-person" aria-hidden="true"></i>
          </RouterLink>

          <button
            class="icon-button icon-button--danger"
            type="button"
            :disabled="loggingOut"
            :aria-label="t('nav.signOut')"
            @click="handleLogout"
          >
            <span v-if="loggingOut" class="spinner-border spinner-border-sm" aria-hidden="true"></span>
            <i v-else class="bi bi-box-arrow-right" aria-hidden="true"></i>
          </button>
        </div>
      </header>

      <main id="main-content" class="page-content" tabindex="-1">
        <slot />
      </main>
    </div>

    <Teleport to="body">
      <Transition name="drawer">
        <div v-if="drawerOpen" class="drawer-layer d-lg-none">
          <button
            class="drawer-backdrop"
            type="button"
            :aria-label="t('nav.closeMenu')"
            @click="closeDrawer"
          ></button>
          <aside
            id="mobile-navigation"
            ref="drawerPanel"
            class="mobile-drawer"
            role="dialog"
            aria-modal="true"
            :aria-label="t('nav.mobileMenu')"
            @keydown="trapDrawerFocus"
          >
            <div class="mobile-drawer__header">
              <RouterLink class="brand" :to="{ name: 'dashboard' }" :aria-label="t('nav.home')" @click="closeDrawer">
                <img class="brand__logo" :src="logoUrl" alt="Turkuvaz" />
                <span class="brand__copy">
                  <strong>{{ t('app.shortName') }}</strong>
                  <small>{{ t('app.tagline') }}</small>
                </span>
              </RouterLink>
              <button
                class="icon-button"
                type="button"
                :aria-label="t('nav.closeMenu')"
                @click="closeDrawer"
              >
                <i class="bi bi-x-lg" aria-hidden="true"></i>
              </button>
            </div>

            <nav class="mobile-drawer__nav" :aria-label="t('nav.primary')">
              <RouterLink
                v-for="link in visibleLinks"
                :key="link.name"
                class="sidebar-link"
                :to="{ name: link.name }"
                @click="closeDrawer"
              >
                <i :class="['bi', link.icon]" aria-hidden="true"></i>
                <span>{{ t(link.labelKey) }}</span>
              </RouterLink>
            </nav>

            <div class="mobile-drawer__footer">
              <RouterLink class="user-card" :to="{ name: 'profile' }" @click="closeDrawer">
                <span class="avatar" aria-hidden="true">{{ initials }}</span>
                <span class="user-card__copy">
                  <strong>{{ displayName }}</strong>
                  <small>{{ roleLabel }}</small>
                </span>
              </RouterLink>
              <button class="btn btn-outline-danger w-100" type="button" @click="handleLogout">
                <i class="bi bi-box-arrow-right me-2" aria-hidden="true"></i>
                {{ t('nav.signOut') }}
              </button>
            </div>
          </aside>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import logoUrl from '@/assets/turkuvaz-logo.webp'
import { setLocale, supportedLocales } from '@/i18n'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const toast = useToastStore()
const { t, locale } = useI18n()

const drawerOpen = ref(false)
const drawerPanel = ref(null)
const drawerTrigger = ref(null)
const loggingOut = ref(false)

const links = [
  { name: 'dashboard', labelKey: 'nav.dashboard', icon: 'bi-grid-1x2' },
  { name: 'create-ticket', labelKey: 'nav.createTicket', icon: 'bi-plus-square', roles: ['User'] },
  { name: 'my-tickets', labelKey: 'nav.myTickets', icon: 'bi-inbox', roles: ['User'] },
  { name: 'send-feedback', labelKey: 'nav.sendFeedback', icon: 'bi-chat-square-text', roles: ['User'] },
  { name: 'admin-tickets', labelKey: 'nav.tickets', icon: 'bi-kanban', roles: ['Admin'] },
  { name: 'services', labelKey: 'nav.services', icon: 'bi-box-seam', roles: ['Admin'] },
  { name: 'firms', labelKey: 'nav.firms', icon: 'bi-buildings', roles: ['Admin'] },
  { name: 'accounts', labelKey: 'nav.accounts', icon: 'bi-people', roles: ['Admin'] },
  { name: 'admin-feedback', labelKey: 'nav.feedback', icon: 'bi-chat-left-dots', roles: ['Admin'] },
]

const visibleLinks = computed(() =>
  links.filter((link) => !link.roles || link.roles.includes(auth.role)),
)
const displayName = computed(() => {
  const fullName = [auth.user?.firstName, auth.user?.lastName].filter(Boolean).join(' ')
  return fullName || auth.user?.userName || auth.user?.email || t('profile.unknownUser')
})
const initials = computed(() => {
  const values = [auth.user?.firstName, auth.user?.lastName].filter(Boolean)
  if (!values.length) values.push(auth.user?.userName || auth.user?.email || 'T')
  return values.map((value) => value.charAt(0)).slice(0, 2).join('').toLocaleUpperCase(locale.value)
})
const roleLabel = computed(() => t(`roles.${String(auth.role || 'User').toLowerCase()}`))
const currentPageTitle = computed(() =>
  route.meta.titleKey ? t(route.meta.titleKey) : t('app.shortName'),
)
const formattedToday = computed(() =>
  new Intl.DateTimeFormat(locale.value, {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  }).format(new Date()),
)

watch(drawerOpen, async (open) => {
  document.body.classList.toggle('drawer-is-open', open)
  if (open) {
    await nextTick()
    drawerPanel.value?.querySelector('a, button, select, [tabindex]:not([tabindex="-1"])')?.focus()
  }
})

watch(
  () => route.fullPath,
  () => {
    if (drawerOpen.value) closeDrawer(false)
  },
)

function changeLocale(event) {
  setLocale(event.target.value)
}

function closeDrawer(restoreFocus = true) {
  drawerOpen.value = false
  if (restoreFocus) nextTick(() => drawerTrigger.value?.focus())
}

function trapDrawerFocus(event) {
  if (event.key === 'Escape') {
    event.preventDefault()
    closeDrawer()
    return
  }
  if (event.key !== 'Tab') return

  const focusable = drawerPanel.value?.querySelectorAll(
    'a[href], button:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])',
  )
  if (!focusable?.length) return

  const first = focusable[0]
  const last = focusable[focusable.length - 1]
  if (event.shiftKey && document.activeElement === first) {
    event.preventDefault()
    last.focus()
  } else if (!event.shiftKey && document.activeElement === last) {
    event.preventDefault()
    first.focus()
  }
}

async function handleLogout() {
  if (loggingOut.value) return
  loggingOut.value = true
  try {
    await auth.logout()
    toast.info(t('auth.signedOut'))
    closeDrawer(false)
    await router.replace({ name: 'sign-in' })
  } finally {
    loggingOut.value = false
  }
}

onBeforeUnmount(() => document.body.classList.remove('drawer-is-open'))
</script>
