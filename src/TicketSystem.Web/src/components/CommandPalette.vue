<template>
  <Teleport to="body">
    <Transition name="dialog-fade">
      <!--
        v-if, never v-show. Playwright's getByRole name matching is substring
        based, so a hidden-but-mounted entry labelled "Çıkış yap" would make
        e2e/auth.spec.js's sign-out lookup ambiguous. v-if keeps it out of the
        accessibility tree entirely.
      -->
      <div v-if="open" class="dialog-layer palette-layer" @mousedown.self="close">
        <section
          ref="panel"
          class="command-palette"
          role="dialog"
          aria-modal="true"
          :aria-label="t('palette.title')"
          @keydown="onKeydown"
        >
          <div class="command-palette__field">
            <i class="bi bi-search" aria-hidden="true"></i>
            <input
              ref="input"
              v-model="query"
              class="command-palette__input"
              type="text"
              role="combobox"
              aria-expanded="true"
              :aria-controls="listId"
              :aria-activedescendant="results.length ? optionId(activeIndex) : undefined"
              aria-autocomplete="list"
              autocomplete="off"
              :placeholder="t('palette.placeholder')"
              :aria-label="t('palette.placeholder')"
            />
            <kbd class="command-palette__kbd">Esc</kbd>
          </div>

          <ul :id="listId" class="command-palette__list" role="listbox" :aria-label="t('palette.title')">
            <template v-for="(section, sectionIndex) in sections" :key="section.key">
              <li v-if="section.items.length" class="command-palette__section" role="presentation">
                {{ t(section.labelKey) }}
              </li>
              <li
                v-for="item in section.items"
                :id="optionId(item.index)"
                :key="`${sectionIndex}-${item.id}`"
                class="command-palette__option"
                :class="{ 'is-active': item.index === activeIndex }"
                role="option"
                :aria-selected="item.index === activeIndex"
                @click="run(item)"
                @mousemove="activeIndex = item.index"
              >
                <i :class="['bi', item.icon]" aria-hidden="true"></i>
                <span>{{ item.label }}</span>
                <small v-if="item.hint">{{ item.hint }}</small>
              </li>
            </template>

            <li v-if="!results.length" class="command-palette__empty" role="presentation">
              {{ t('palette.empty') }}
            </li>
          </ul>
        </section>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { computed, nextTick, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'

import { quickActions, visibleNavLinks } from '@/config/navigation'
import { setLocale, supportedLocales } from '@/i18n'
import { cycleTheme, themeMode } from '@/composables/useTheme'
import { acquireModalLock, releaseModalLock } from '@/utils/modalLock'
import { useAuthStore } from '@/stores/auth'

const props = defineProps({
  open: { type: Boolean, required: true },
})
const emit = defineEmits(['update:open'])

const { t, locale } = useI18n()
const router = useRouter()
const auth = useAuthStore()

const panel = ref(null)
const input = ref(null)
const query = ref('')
const activeIndex = ref(0)
const listId = `palette-list-${useId()}`
let returnFocusTo = null

const optionId = (index) => `${listId}-option-${index}`

const pages = computed(() =>
  visibleNavLinks(auth.role, auth.isSuperAdmin).map((link) => ({
    id: `page-${link.name}`,
    label: t(link.labelKey),
    hint: t(`nav.groups.${link.group}`),
    icon: link.icon,
    run: () => router.push({ name: link.name }),
  })),
)

const actions = computed(() => {
  const roleActions = quickActions
    .filter((action) => !action.roles || action.roles.includes(auth.role))
    .map((action) => ({
      id: action.id,
      label: t(action.labelKey),
      icon: action.icon,
      run: () => router.push(action.to),
    }))

  const otherLocale = supportedLocales.find((item) => item.code !== locale.value)

  return [
    ...roleActions,
    {
      id: 'theme',
      label: t('theme.toggle'),
      hint: t(`theme.${themeMode.value}`),
      icon: 'bi-circle-half',
      // Stays open: cycling through three modes is something you do more than
      // once, and closing after each press would make that tedious.
      keepOpen: true,
      run: cycleTheme,
    },
    {
      id: 'locale',
      label: t('palette.switchLanguage', { language: otherLocale?.label ?? '' }),
      icon: 'bi-translate',
      run: () => setLocale(otherLocale?.code),
    },
    {
      id: 'sign-out',
      label: t('nav.signOut'),
      icon: 'bi-box-arrow-right',
      run: async () => {
        await auth.logout()
        await router.replace({ name: 'sign-in' })
      },
    },
  ]
})

function matches(item, needle) {
  return `${item.label} ${item.hint ?? ''}`.toLocaleLowerCase(locale.value).includes(needle)
}

const sections = computed(() => {
  const needle = query.value.trim().toLocaleLowerCase(locale.value)
  const filter = (items) => (needle ? items.filter((item) => matches(item, needle)) : items)

  // The running index spans both sections so aria-activedescendant and the
  // arrow keys address one flat list, which is what a listbox expects.
  let index = 0
  const withIndex = (items) => items.map((item) => ({ ...item, index: index++ }))

  return [
    { key: 'pages', labelKey: 'palette.sections.pages', items: withIndex(filter(pages.value)) },
    { key: 'actions', labelKey: 'palette.sections.actions', items: withIndex(filter(actions.value)) },
  ]
})

const results = computed(() => sections.value.flatMap((section) => section.items))

watch(query, () => {
  activeIndex.value = 0
})

watch(
  () => props.open,
  async (open) => {
    if (open) {
      acquireModalLock()
      returnFocusTo = document.activeElement
      query.value = ''
      activeIndex.value = 0
      await nextTick()
      input.value?.focus()
    } else {
      releaseModalLock()
      returnFocusTo?.focus?.()
      returnFocusTo = null
    }
  },
)

function close() {
  emit('update:open', false)
}

async function run(item) {
  const target = results.value.find((result) => result.index === item.index) ?? item
  if (!target.keepOpen) close()
  await target.run()
}

function move(step) {
  if (!results.value.length) return
  const count = results.value.length
  activeIndex.value = (activeIndex.value + step + count) % count
}

function onKeydown(event) {
  switch (event.key) {
    case 'Escape':
      event.preventDefault()
      close()
      break
    case 'ArrowDown':
      event.preventDefault()
      move(1)
      break
    case 'ArrowUp':
      event.preventDefault()
      move(-1)
      break
    case 'Home':
      event.preventDefault()
      activeIndex.value = 0
      break
    case 'End':
      event.preventDefault()
      activeIndex.value = Math.max(0, results.value.length - 1)
      break
    case 'Enter': {
      event.preventDefault()
      const target = results.value.find((item) => item.index === activeIndex.value)
      if (target) run(target)
      break
    }
    case 'Tab':
      // Only two focusable things exist in here and the input owns the keyboard,
      // so the trap is simply "focus never leaves the input".
      event.preventDefault()
      break
    default:
      break
  }
}
</script>

<style scoped>
.palette-layer {
  align-items: flex-start;
  justify-content: center;
  padding: clamp(16px, 12vh, 140px) 16px 16px;
  background: var(--tv-overlay);
}

.command-palette {
  display: flex;
  width: min(100%, 620px);
  max-height: min(70vh, 560px);
  flex-direction: column;
  overflow: hidden;
  border: 1px solid var(--tv-border);
  border-radius: var(--tv-radius-lg);
  background: var(--tv-surface-3);
  box-shadow: var(--tv-shadow-lg);
}

.command-palette__field {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.9rem 1rem;
  border-bottom: 1px solid var(--tv-border);
  color: var(--tv-text-subtle);
}

.command-palette__input {
  min-width: 0;
  flex: 1;
  border: 0;
  background: transparent;
  color: var(--tv-text-strong);
  font-size: 1rem;
  outline: none;
}

.command-palette__input::placeholder {
  color: var(--tv-text-subtle);
}

.command-palette__kbd {
  padding: 0.15rem 0.45rem;
  border: 1px solid var(--tv-border);
  border-radius: 6px;
  background: var(--tv-surface-sunken);
  color: var(--tv-text-subtle);
  font-size: 0.7rem;
}

.command-palette__list {
  margin: 0;
  padding: 0.5rem;
  overflow-y: auto;
  list-style: none;
}

.command-palette__section {
  padding: 0.6rem 0.65rem 0.35rem;
  color: var(--tv-text-subtle);
  font-size: 0.7rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.command-palette__option {
  display: flex;
  min-height: 44px;
  align-items: center;
  gap: 0.75rem;
  padding: 0.5rem 0.65rem;
  border-radius: 10px;
  color: var(--tv-text);
  cursor: pointer;
}

.command-palette__option .bi {
  width: 20px;
  color: var(--tv-text-subtle);
  text-align: center;
}

.command-palette__option span {
  flex: 1;
  font-weight: 600;
}

.command-palette__option small {
  color: var(--tv-text-subtle);
  font-size: 0.75rem;
}

.command-palette__option.is-active {
  background: var(--tv-accent-soft);
  color: var(--tv-accent-on-soft);
}

.command-palette__option.is-active .bi {
  color: var(--tv-accent-on-soft);
}

/* The hint keeps the subtle tone on a plain row, but on the accent tint of the
   highlighted row that tone falls under 4.5:1 — it has to move onto the token
   that is defined as readable on this exact background. */
.command-palette__option.is-active small {
  color: var(--tv-accent-on-soft);
}

.command-palette__empty {
  padding: 1.5rem 0.65rem;
  color: var(--tv-text-muted);
  text-align: center;
}
</style>
