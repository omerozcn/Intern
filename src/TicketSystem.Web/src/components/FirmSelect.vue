<template>
  <div class="firm-select">
    <input
      :id="inputId"
      v-model.trim="term"
      class="form-control form-control-sm"
      type="search"
      role="combobox"
      autocomplete="off"
      :aria-expanded="listboxVisible"
      :aria-controls="listboxVisible ? `${inputId}-options` : undefined"
      :placeholder="placeholder || t('firmSelect.placeholder')"
      :disabled="disabled"
    />

    <p v-if="loading" class="firm-select__hint">{{ t('common.loading') }}</p>
    <p v-else-if="error" class="firm-select__hint firm-select__hint--error">{{ error }}</p>
    <p v-else-if="loaded && !options.length" class="firm-select__hint">{{ t('firmSelect.noResults') }}</p>

    <ul v-else-if="listboxVisible" :id="`${inputId}-options`" class="firm-select__options" role="listbox">
      <li v-for="firm in options" :key="firm.id" role="option" :aria-selected="firm.id === modelValue">
        <button
          type="button"
          class="firm-select__option"
          :class="{ 'is-selected': firm.id === modelValue }"
          :disabled="disabled"
          @click="select(firm)"
        >
          {{ firm.name }}
        </button>
      </li>
    </ul>

    <p v-if="hasMore" class="firm-select__hint">{{ t('firmSelect.refine') }}</p>
  </div>
</template>

<script setup>
import { computed, onMounted, onScopeDispose, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'

import { api } from '@/services/api'

const PAGE_SIZE = 10
const DEBOUNCE_MS = 300

const props = defineProps({
  modelValue: { type: [Number, String], default: '' },
  /** Firm ids to hide, e.g. the ones already assigned. */
  excludeIds: { type: Array, default: () => [] },
  /** Hide the protected system firm; it cannot own regular user accounts. */
  excludeProtected: { type: Boolean, default: false },
  inputId: { type: String, required: true },
  placeholder: { type: String, default: '' },
  disabled: { type: Boolean, default: false },
})

const emit = defineEmits(['update:modelValue', 'select'])

const { t } = useI18n()
const term = ref('')
const rows = ref([])
const totalCount = ref(0)
const loading = ref(false)
const error = ref('')
const loaded = ref(false)

let timer = null
let requestId = 0

const options = computed(() =>
  rows.value.filter(
    (firm) => !props.excludeIds.includes(firm.id) && !(props.excludeProtected && firm.isProtected),
  ),
)
const hasMore = computed(() => totalCount.value > rows.value.length)
// aria-expanded must track the listbox, and aria-controls must not point at an id
// that is absent from the DOM while loading, erroring or empty.
const listboxVisible = computed(() => !loading.value && !error.value && options.value.length > 0)

async function fetchFirms() {
  loading.value = true
  error.value = ''
  const currentRequest = ++requestId

  const params = new URLSearchParams({ page: '1', pageSize: String(PAGE_SIZE) })
  if (term.value) params.set('search', term.value)

  try {
    const result = await api.get(`/api/firms?${params.toString()}`)
    if (currentRequest !== requestId) return
    rows.value = (result?.items ?? []).map((item) => ({
      id: Number(item.id),
      name: item.name,
      isProtected: Boolean(item.isProtected),
    }))
    totalCount.value = result?.totalCount ?? rows.value.length
  } catch (requestError) {
    if (currentRequest !== requestId) return
    error.value = requestError.message || t('errors.loadFirms')
    rows.value = []
  } finally {
    if (currentRequest === requestId) {
      loading.value = false
      loaded.value = true
    }
  }
}

function select(firm) {
  emit('update:modelValue', firm.id)
  emit('select', firm)
}

watch(term, () => {
  globalThis.clearTimeout(timer)
  timer = globalThis.setTimeout(fetchFirms, DEBOUNCE_MS)
})

// This component is rendered once per row on the services page; a pending debounce
// must not outlive it.
onScopeDispose(() => globalThis.clearTimeout(timer))

onMounted(fetchFirms)

defineExpose({ reload: fetchFirms })
</script>

<style scoped>
.firm-select { display: flex; flex-direction: column; gap: .35rem; min-width: 0; }
.firm-select__hint { margin: 0; color: var(--color-text-muted); font-size: .78rem; }
.firm-select__hint--error { color: #b42318; }
.firm-select__options { display: flex; flex-direction: column; max-height: 190px; margin: 0; padding: 0; overflow-y: auto; border: 1px solid var(--color-border); border-radius: 10px; list-style: none; }
.firm-select__option { width: 100%; padding: .45rem .65rem; border: 0; color: inherit; background: transparent; text-align: left; font-size: .85rem; }
.firm-select__option:hover:not(:disabled) { background: var(--color-primary-soft); }
.firm-select__option.is-selected { color: #03696b; background: var(--color-primary-soft); font-weight: 700; }
</style>
