<template>
  <!--
    aria-live is deliberately absent: the intermediate values are decoration, and
    announcing every frame would flood a screen reader. The final value is read
    from the surrounding label when the user reaches it.
  -->
  <span>{{ formatted }}</span>
</template>

<script setup>
import { computed, toRef } from 'vue'
import { useI18n } from 'vue-i18n'

import { useCountUp } from '@/composables/useCountUp'

const props = defineProps({
  value: { type: Number, default: 0 },
})

const { locale } = useI18n()
const displayed = useCountUp(toRef(props, 'value'))

const formatted = computed(() =>
  new Intl.NumberFormat(locale.value === 'tr' ? 'tr-TR' : 'en-US').format(displayed.value),
)
</script>
