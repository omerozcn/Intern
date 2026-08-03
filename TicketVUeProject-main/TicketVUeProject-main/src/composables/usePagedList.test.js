import { flushPromises } from '@vue/test-utils'
import { ref } from 'vue'
import { beforeEach, describe, expect, it, vi } from 'vitest'

import { usePagedList } from './usePagedList'
import { api } from '@/services/api'

vi.mock('@/services/api', () => ({ api: { get: vi.fn() } }))

function envelope(items, overrides = {}) {
  return {
    items,
    page: 1,
    pageSize: 20,
    totalCount: items.length,
    totalPages: 1,
    hasPrevious: false,
    hasNext: false,
    ...overrides,
  }
}

function lastQuery() {
  const url = api.get.mock.calls.at(-1)[0]
  return new URLSearchParams(url.slice(url.indexOf('?') + 1))
}

describe('usePagedList', () => {
  beforeEach(() => {
    vi.useRealTimers()
    api.get.mockReset()
  })

  it('sends the paging parameters and exposes the envelope', async () => {
    api.get.mockResolvedValue(envelope([{ id: 1 }], { totalCount: 42, totalPages: 3, hasNext: true }))

    const list = usePagedList('/api/Firm/listFirm')
    await list.load()

    expect(lastQuery().get('page')).toBe('1')
    expect(lastQuery().get('pageSize')).toBe('20')
    expect(list.totalCount.value).toBe(42)
    expect(list.totalPages.value).toBe(3)
    expect(list.hasNext.value).toBe(true)
  })

  it('maps rows before handing them to the view', async () => {
    api.get.mockResolvedValue(envelope([{ id: '7', name: 'Firma' }]))

    const list = usePagedList('/api/Firm/listFirm', {
      map: (rows) => rows.map((row) => ({ id: Number(row.id) })),
    })
    await list.load()

    expect(list.items.value).toEqual([{ id: 7 }])
  })

  it('clamps the requested page to the available range', async () => {
    api.get.mockResolvedValue(envelope([{ id: 1 }], { totalPages: 2 }))
    const list = usePagedList('/api/Firm/listFirm')
    await list.load()

    await list.goToPage(99)

    expect(list.page.value).toBe(2)
  })

  it('adds extra filter parameters and drops empty ones', async () => {
    api.get.mockResolvedValue(envelope([]))
    const params = ref({ status: 'pending', firmId: '' })

    const list = usePagedList('/api/Ticket/listTicket', { params })
    await list.load()

    expect(lastQuery().get('status')).toBe('pending')
    expect(lastQuery().has('firmId')).toBe(false)
  })

  it('restarts at the first page when a filter changes', async () => {
    api.get.mockResolvedValue(envelope([{ id: 1 }], { totalPages: 5 }))
    const params = ref({ status: '' })
    const list = usePagedList('/api/Ticket/listTicket', { params })
    await list.load()
    await list.goToPage(3)
    expect(list.page.value).toBe(3)

    params.value = { status: 'completed' }
    await flushPromises()

    expect(list.page.value).toBe(1)
    expect(lastQuery().get('status')).toBe('completed')
  })

  it('steps back when the last row of a page is removed', async () => {
    api.get
      .mockResolvedValueOnce(envelope([{ id: 1 }], { page: 2, totalPages: 2, hasPrevious: true }))
      .mockResolvedValueOnce(envelope([], { page: 2, totalCount: 1, totalPages: 1 }))
      .mockResolvedValueOnce(envelope([{ id: 1 }], { totalCount: 1, totalPages: 1 }))

    const list = usePagedList('/api/Firm/listFirm')
    await list.load()
    await list.goToPage(2)
    await list.load()

    expect(list.page.value).toBe(1)
  })

  it('surfaces the error message and clears the rows', async () => {
    api.get.mockRejectedValue(new Error('Sunucuya ulaşılamadı.'))

    const list = usePagedList('/api/Firm/listFirm')
    await list.load()

    expect(list.error.value).toBe('Sunucuya ulaşılamadı.')
    expect(list.items.value).toEqual([])
    expect(list.loading.value).toBe(false)
  })

  it('ignores a slow response that a newer request has superseded', async () => {
    let resolveFirst
    api.get
      .mockImplementationOnce(() => new Promise((resolve) => { resolveFirst = resolve }))
      .mockResolvedValueOnce(envelope([{ id: 'new' }]))

    const list = usePagedList('/api/Firm/listFirm')
    const stale = list.load()
    const fresh = list.load()
    resolveFirst(envelope([{ id: 'stale' }]))
    await Promise.all([stale, fresh])

    expect(list.items.value).toEqual([{ id: 'new' }])
  })
})
