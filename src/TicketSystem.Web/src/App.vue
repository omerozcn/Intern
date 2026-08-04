<template>
  <ToastHost />

  <RouterView v-slot="{ Component, route }">
    <AppShell v-if="route.meta.shell !== false">
      <component :is="Component" />
    </AppShell>

    <div v-else class="auth-layout">
      <a class="skip-link" href="#main-content">{{ t('common.skipToContent') }}</a>
      <main id="main-content" class="auth-layout__content" tabindex="-1">
        <component :is="Component" />
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
