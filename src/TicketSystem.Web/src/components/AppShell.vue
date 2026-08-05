<template>
  <div class="app-shell" :data-rail="isRail">
    <a class="skip-link" href="#main-content">{{ t('common.skipToContent') }}</a>

    <aside class="sidebar d-none d-lg-flex" :aria-label="t('nav.primary')">
      <RouterLink class="brand" :to="{ name: 'dashboard' }" :aria-label="t('nav.home')">
        <img class="brand__logo" :src="logoUrl" alt="Turkuvaz" />
        <!-- The asset is a ~4:1 wordmark and cannot shrink into an 84px rail
             without becoming unreadable, so the rail gets a monogram instead. -->
        <span class="brand__mark" aria-hidden="true">{{ brandInitial }}</span>
        <span class="brand__copy">
          <strong>{{ t('app.shortName') }}</strong>
          <small>{{ t('app.tagline') }}</small>
        </span>
      </RouterLink>

      <nav id="primary-navigation" class="sidebar__nav" :aria-label="t('nav.primary')">
        <RouterLink
          v-for="link in visibleLinks"
          :key="link.name"
          class="sidebar-link"
          :to="{ name: link.name }"
          :data-label="t(link.labelKey)"
        >
          <i :class="['bi', link.icon]" aria-hidden="true"></i>
          <!-- visually-hidden rather than display:none in rail mode: the link
               keeps its accessible name either way. -->
          <span :class="{ 'visually-hidden': isRail }">{{ t(link.labelKey) }}</span>
        </RouterLink>
      </nav>

      <div class="sidebar__footer">
        <button
          class="rail-toggle"
          type="button"
          :aria-expanded="!isRail"
          aria-controls="primary-navigation"
          :aria-label="isRail ? t('nav.expandSidebar') : t('nav.collapseSidebar')"
          :title="isRail ? t('nav.expandSidebar') : t('nav.collapseSidebar')"
          @click="toggleSidebar"
        >
          <i :class="['bi', isRail ? 'bi-chevron-double-right' : 'bi-chevron-double-left']" aria-hidden="true"></i>
          <span :class="{ 'visually-hidden': isRail }">{{ t('nav.collapseSidebar') }}</span>
        </button>

        <RouterLink class="user-card" :to="{ name: 'profile' }" :title="displayName">
          <span class="avatar" aria-hidden="true">{{ initials }}</span>
          <span class="user-card__copy" :class="{ 'visually-hidden': isRail }">
            <strong>{{ displayName }}</strong>
            <small>{{ roleLabel }}</small>
          </span>
          <i v-if="!isRail" class="bi bi-chevron-right" aria-hidden="true"></i>
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
          <!-- The router is flat: twelve sibling routes, no hierarchy. A
               breadcrumb trail would be invented structure, so this shows the
               real grouping the navigation already uses. -->
          <small v-if="currentGroup" class="topbar__group">{{ t(`nav.groups.${currentGroup}`) }}</small>
          <span>{{ currentPageTitle }}</span>
          <small class="d-none d-md-block">{{ formattedToday }}</small>
        </div>

        <div class="topbar__actions">
          <button class="palette-trigger d-none d-md-flex" type="button" @click="paletteOpen = true">
            <i class="bi bi-search" aria-hidden="true"></i>
            <span>{{ t('palette.open') }}</span>
            <kbd>{{ shortcutHint }}</kbd>
          </button>

          <ThemeToggle />

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

    <CommandPalette v-model:open="paletteOpen" />

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
              <!-- The topbar hides its non-danger icon buttons below 768px, so the
                   theme control needs a home that survives on a phone. -->
              <div class="drawer-preferences">
                <span class="drawer-preferences__label">{{ t('theme.label') }}</span>
                <ThemeToggle variant="segmented" />
              </div>

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
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import logoUrl from '@/assets/turkuvaz-logo.webp'
import CommandPalette from '@/components/CommandPalette.vue'
import ThemeToggle from '@/components/ThemeToggle.vue'
import { sidebarMode, toggleSidebar } from '@/composables/useSidebar'
import { navLinks, visibleNavLinks } from '@/config/navigation'
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
const paletteOpen = ref(false)

const isRail = computed(() => sidebarMode.value === 'rail')
const visibleLinks = computed(() => visibleNavLinks(auth.role, auth.isSuperAdmin))
const currentGroup = computed(
  () => navLinks.find((link) => link.name === route.name)?.group ?? '',
)
const brandInitial = computed(() => t('app.shortName').charAt(0).toLocaleUpperCase(locale.value))
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
const shortcutHint = computed(() =>
  /Mac|iPhone|iPad/.test(globalThis.navigator?.platform ?? '') ? '⌘K' : 'Ctrl K',
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

function onGlobalKeydown(event) {
  if (event.key !== 'k' && event.key !== 'K') return
  if (!event.metaKey && !event.ctrlKey) return
  event.preventDefault()
  paletteOpen.value = !paletteOpen.value
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

onMounted(() => window.addEventListener('keydown', onGlobalKeydown))

onBeforeUnmount(() => {
  window.removeEventListener('keydown', onGlobalKeydown)
  document.body.classList.remove('drawer-is-open')
})
</script>
