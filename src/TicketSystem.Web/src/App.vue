<template>
  <ToastHost />

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
import ToastHost from '@/components/ToastHost.vue'

const { t } = useI18n()
</script>
