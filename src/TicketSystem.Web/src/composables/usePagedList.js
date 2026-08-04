import { isRef, ref, unref, watch } from 'vue'

import { api } from '@/services/api'

const SEARCH_DEBOUNCE_MS = 300

/**
 * Drives a paginated list endpoint: keeps page state, debounces the search box and
 * exposes the envelope the API returns ({ items, page, pageSize, totalCount, ... }).
 *
 * @param {string} path      list endpoint, e.g. "/api/Product/listProduct"
 * @param {object} [options]
 * @param {number} [options.pageSize]
 * @param {(rows: any[]) => any[]} [options.map] shape each row before it reaches the view
 * @param {import('vue').Ref<object>} [options.params] extra query params; changing them reloads from page 1
 */
export function usePagedList(path, { pageSize = 20, map, params } = {}) {
  const items = ref([])
  const page = ref(1)
  const totalCount = ref(0)
  const totalPages = ref(0)
  const hasPrevious = ref(false)
  const hasNext = ref(false)
  const search = ref('')
  const loading = ref(true)
  const error = ref('')

  let searchTimer = null
  let requestId = 0

  async function load() {
    loading.value = true
    error.value = ''
    const currentRequest = ++requestId

    const query = new URLSearchParams({
      page: String(page.value),
      pageSize: String(pageSize),
    })
    if (search.value) query.set('search', search.value)
    for (const [key, value] of Object.entries(unref(params) ?? {})) {
      if (value !== null && value !== undefined && value !== '') query.set(key, String(value))
    }

    try {
      const result = await api.get(`${path}?${query.toString()}`)
      // A slower earlier request must not overwrite a newer one.
      if (currentRequest !== requestId) return

      const rows = result?.items ?? []
      items.value = map ? map(rows) : rows
      totalCount.value = result?.totalCount ?? rows.length
      totalPages.value = result?.totalPages ?? 1
      hasPrevious.value = result?.hasPrevious ?? false
      hasNext.value = result?.hasNext ?? false

      // A deletion can empty the last page; step back so the user is not stranded.
      if (!rows.length && page.value > 1) {
        page.value -= 1
        await load()
      }
    } catch (requestError) {
      if (currentRequest !== requestId) return
      error.value = requestError.message || ''
      items.value = []
    } finally {
      if (currentRequest === requestId) loading.value = false
    }
  }

  function goToPage(next) {
    const target = Math.min(Math.max(1, next), Math.max(1, totalPages.value))
    if (target === page.value) return
    page.value = target
    return load()
  }

  watch(search, () => {
    globalThis.clearTimeout(searchTimer)
    searchTimer = globalThis.setTimeout(() => {
      page.value = 1
      load()
    }, SEARCH_DEBOUNCE_MS)
  })

  // Filters apply across the whole result set, so changing one restarts at page 1.
  if (isRef(params)) {
    watch(params, () => {
      page.value = 1
      load()
    }, { deep: true })
  }

  return {
    items,
    page,
    totalCount,
    totalPages,
    hasPrevious,
    hasNext,
    search,
    loading,
    error,
    load,
    goToPage,
  }
}
