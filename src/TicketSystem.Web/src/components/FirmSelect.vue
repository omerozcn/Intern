<template>
  <div ref="root" class="firm-select" :class="{ 'is-open': isOpen }">
    <div class="firm-select__control">
      <input
        :id="inputId"
        ref="input"
        v-model.trim="term"
        class="form-control form-control-sm"
        type="text"
        role="combobox"
        autocomplete="off"
        aria-autocomplete="list"
        :aria-expanded="isOpen"
        :aria-controls="isOpen ? listboxId : undefined"
        :aria-activedescendant="activeOptionId"
        :placeholder="placeholder || t('firmSelect.placeholder')"
        :disabled="disabled"
        @focus="open"
        @keydown="onKeydown"
      />
      <button
        v-if="selectedFirm"
        type="button"
        class="firm-select__clear"
        :aria-label="t('common.clear')"
        :disabled="disabled"
        @click="clear"
      >
        <i class="bi bi-x-lg" aria-hidden="true"></i>
      </button>
      <i v-else class="bi bi-chevron-down firm-select__caret" aria-hidden="true"></i>
    </div>

    <Transition name="pop">
      <div v-if="isOpen" class="firm-select__popover">
        <p v-if="loading" class="firm-select__hint">{{ t('common.loading') }}</p>
        <p v-else-if="error" class="firm-select__hint firm-select__hint--error">{{ error }}</p>
        <p v-else-if="!options.length" class="firm-select__hint">{{ t('firmSelect.noResults') }}</p>

        <ul v-else :id="listboxId" class="firm-select__options" role="listbox">
          <li
            v-for="(firm, index) in options"
            :id="`${inputId}-option-${firm.id}`"
            :key="firm.id"
            class="firm-select__option"
            :class="{ 'is-active': index === activeIndex, 'is-selected': firm.id === modelValue }"
            role="option"
            :aria-selected="firm.id === modelValue"
            @mousedown.prevent="select(firm)"
            @mousemove="activeIndex = index"
          >
            {{ firm.name }}
          </li>
        </ul>

        <p v-if="hasMore && options.length" class="firm-select__hint firm-select__hint--footer">
          {{ t('firmSelect.refine') }}
        </p>
      </div>
    </Transition>
  </div>
</template>

<script setup>
import { computed, nextTick, onMounted, onScopeDispose, ref, watch } from 'vue'
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
const root = ref(null)
const input = ref(null)
const term = ref('')
const rows = ref([])
const totalCount = ref(0)
const loading = ref(false)
const error = ref('')
const isOpen = ref(false)
const activeIndex = ref(-1)

let timer = null
let requestId = 0

const listboxId = computed(() => `${props.inputId}-options`)

const options = computed(() =>
  rows.value.filter(
    (firm) => !props.excludeIds.includes(firm.id) && !(props.excludeProtected && firm.isProtected),
  ),
)
const hasMore = computed(() => totalCount.value > rows.value.length)
const selectedFirm = computed(() =>
  rows.value.find((firm) => firm.id === Number(props.modelValue)) ?? null,
)
const activeOptionId = computed(() => {
  const firm = options.value[activeIndex.value]
  return isOpen.value && firm ? `${props.inputId}-option-${firm.id}` : undefined
})

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
      activeIndex.value = options.value.length ? 0 : -1
    }
  }
}

function open() {
  if (props.disabled) return
  isOpen.value = true
}

function close() {
  isOpen.value = false
  activeIndex.value = -1
  // The input shows the search term while browsing; restore the chosen name on close.
  term.value = selectedFirm.value?.name ?? ''
}

function select(firm) {
  emit('update:modelValue', firm.id)
  emit('select', firm)
  term.value = firm.name
  isOpen.value = false
  activeIndex.value = -1
}

function clear() {
  emit('update:modelValue', '')
  emit('select', null)
  term.value = ''
  nextTick(() => input.value?.focus())
}

function move(step) {
  if (!options.value.length) return
  open()
  const next = activeIndex.value + step
  activeIndex.value = (next + options.value.length) % options.value.length
}

function onKeydown(event) {
  switch (event.key) {
    case 'ArrowDown':
      event.preventDefault()
      move(1)
      break
    case 'ArrowUp':
      event.preventDefault()
      move(-1)
      break
    case 'Enter':
      if (isOpen.value && options.value[activeIndex.value]) {
        event.preventDefault()
        select(options.value[activeIndex.value])
      }
      break
    case 'Escape':
      if (isOpen.value) {
        event.preventDefault()
        close()
      }
      break
    default:
      break
  }
}

function onPointerDown(event) {
  if (root.value && !root.value.contains(event.target)) close()
}

watch(term, (value) => {
  // Typing is a search, not a selection: reopen so the results are visible.
  if (value !== selectedFirm.value?.name) open()
  globalThis.clearTimeout(timer)
  timer = globalThis.setTimeout(fetchFirms, DEBOUNCE_MS)
})

// Keeps the input label in step when the parent clears or sets the value.
watch(
  () => props.modelValue,
  () => {
    if (!isOpen.value) term.value = selectedFirm.value?.name ?? ''
  },
)

onMounted(() => {
  document.addEventListener('pointerdown', onPointerDown)
  fetchFirms()
})

// This component is rendered once per row on the services page; neither a pending
// debounce nor the document listener must outlive it.
onScopeDispose(() => {
  globalThis.clearTimeout(timer)
  document.removeEventListener('pointerdown', onPointerDown)
})

defineExpose({ reload: fetchFirms })
</script>

<style scoped>
.firm-select { position: relative; min-width: 0; }
.firm-select__control { position: relative; display: flex; align-items: center; }
.firm-select__control input { padding-right: 2.1rem; }

.firm-select__caret,
.firm-select__clear {
  position: absolute;
  right: .5rem;
  display: grid;
  place-items: center;
  width: 22px;
  height: 22px;
  color: var(--color-text-muted);
  font-size: .7rem;
  pointer-events: none;
}

.firm-select__clear {
  border: 0;
  border-radius: 999px;
  background: transparent;
  pointer-events: auto;
  transition: background-color 140ms ease, color 140ms ease;
}

.firm-select__clear:hover:not(:disabled) { color: var(--color-text); background: var(--tv-slate-100); }
.firm-select__caret { transition: transform 180ms ease; }
.firm-select.is-open .firm-select__caret { transform: rotate(180deg); }

/* Overlays the page instead of pushing the layout: this control sits inside filter
   toolbars and table rows, which an inline list would stretch out of shape. */
.firm-select__popover {
  position: absolute;
  z-index: 30;
  top: calc(100% + 4px);
  right: 0;
  left: 0;
  overflow: hidden;
  border: 1px solid var(--color-border);
  border-radius: var(--tv-radius-sm);
  background: var(--tv-white);
  box-shadow: var(--tv-shadow-md);
}

.firm-select__options { max-height: 220px; margin: 0; padding: .25rem; overflow-y: auto; list-style: none; }

.firm-select__option {
  padding: .45rem .6rem;
  border-radius: 8px;
  cursor: pointer;
  font-size: .85rem;
}

.firm-select__option.is-active { background: var(--tv-slate-100); }
.firm-select__option.is-selected { color: #03696b; background: var(--color-primary-soft); font-weight: 700; }

.firm-select__hint { margin: 0; padding: .6rem .7rem; color: var(--color-text-muted); font-size: .78rem; }
.firm-select__hint--error { color: var(--tv-danger); }
.firm-select__hint--footer { border-top: 1px solid var(--color-border); }

.pop-enter-active, .pop-leave-active { transition: opacity 140ms ease, transform 140ms ease; }
.pop-enter-from, .pop-leave-to { opacity: 0; transform: translateY(-4px); }
</style>
