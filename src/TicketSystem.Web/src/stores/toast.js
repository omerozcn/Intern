import { ref } from 'vue'
import { defineStore } from 'pinia'

const DEFAULT_DURATION = 4_000
const MAX_TOASTS = 4

export const useToastStore = defineStore('toast', () => {
  const items = ref([])
  // Store-scoped so a fresh Pinia instance restarts numbering and owns its own timers.
  let nextToastId = 1
  const timers = new Map()

  function add(message, type = 'info', options = {}) {
    if (!message) return null

    const duplicate = items.value.find((item) => item.message === message && item.type === type)
    if (duplicate) return duplicate.id

    const toast = {
      id: nextToastId++,
      message,
      type,
      duration: options.duration ?? DEFAULT_DURATION,
    }

    if (items.value.length >= MAX_TOASTS) dismiss(items.value[0]?.id)
    items.value.push(toast)

    if (toast.duration > 0) {
      timers.set(toast.id, globalThis.setTimeout(() => dismiss(toast.id), toast.duration))
    }

    return toast.id
  }

  function dismiss(id) {
    const timer = timers.get(id)
    if (timer !== undefined) {
      globalThis.clearTimeout(timer)
      timers.delete(id)
    }

    const index = items.value.findIndex((item) => item.id === id)
    if (index >= 0) items.value.splice(index, 1)
  }

  function clear() {
    timers.forEach((timer) => globalThis.clearTimeout(timer))
    timers.clear()
    items.value = []
  }

  const success = (message, options) => add(message, 'success', options)
  const error = (message, options) => add(message, 'error', options)
  const info = (message, options) => add(message, 'info', options)
  const warning = (message, options) => add(message, 'warning', options)

  return { items, add, dismiss, clear, success, error, info, warning }
})
