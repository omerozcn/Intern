<template>
  <ToastHost />
  <RouteProgress />

  <RouterView v-slot="{ Component, route }">
    <AppShell v-if="route.meta.shell !== false">
      <!-- Keyed on the route so each page animates in; mode="out-in" avoids the two
           views overlapping mid-transition. -->
      <Transition name="page" mode="out-in">
        <component :is="Component" :key="route.name" />
      </Transition>
    </AppShell>

    <div v-else class="auth-layout">
      <a class="skip-link" href="#main-content">{{ t('common.skipToContent') }}</a>

      <!-- Placed before <main> so the preferences are reachable by keyboard
           without tabbing through the sign-in form first, and so the theme can
           be set before anyone signs in. -->
      <div class="auth-layout__bar">
        <ThemeToggle />
        <label class="visually-hidden" for="auth-locale">{{ t('common.language') }}</label>
        <select
          id="auth-locale"
          class="form-select form-select-sm locale-select"
          :value="locale"
          @change="setLocale($event.target.value)"
        >
          <option v-for="item in supportedLocales" :key="item.code" :value="item.code">
            {{ item.shortLabel }}
          </option>
        </select>
      </div>

      <main id="main-content" class="auth-layout__content" tabindex="-1">
        <Transition name="page" mode="out-in">
          <component :is="Component" :key="route.name" />
        </Transition>
      </main>
    </div>
  </RouterView>
</template>

<script setup>
import { useI18n } from 'vue-i18n'
import { RouterView } from 'vue-router'

import AppShell from '@/components/AppShell.vue'
import RouteProgress from '@/components/RouteProgress.vue'
import ThemeToggle from '@/components/ThemeToggle.vue'
import ToastHost from '@/components/ToastHost.vue'
import { setLocale, supportedLocales } from '@/i18n'

const { t, locale } = useI18n()
</script>
