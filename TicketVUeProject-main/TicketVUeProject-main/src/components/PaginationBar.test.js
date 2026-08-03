import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import { i18n } from '@/i18n'
import PaginationBar from './PaginationBar.vue'

const global = { plugins: [i18n] }

function mountBar(props) {
  return mount(PaginationBar, {
    props: { page: 1, totalPages: 3, totalCount: 25, hasPrevious: false, hasNext: true, ...props },
    global,
  })
}

describe('PaginationBar', () => {
  it('stays hidden when everything fits on one page', () => {
    const wrapper = mountBar({ totalPages: 1, hasNext: false })

    expect(wrapper.find('nav').exists()).toBe(false)
  })

  it('reports the current position and total', () => {
    const wrapper = mountBar({ page: 2, totalPages: 3, totalCount: 25 })

    expect(wrapper.text()).toContain('2')
    expect(wrapper.text()).toContain('3')
    expect(wrapper.text()).toContain('25')
  })

  it('disables the edges of the range', () => {
    const first = mountBar({ page: 1, hasPrevious: false, hasNext: true })
    const last = mountBar({ page: 3, hasPrevious: true, hasNext: false })

    expect(first.findAll('button')[0].attributes('disabled')).toBeDefined()
    expect(last.findAll('button')[1].attributes('disabled')).toBeDefined()
  })

  it('asks for the neighbouring page', async () => {
    const wrapper = mountBar({ page: 2, hasPrevious: true, hasNext: true })
    const [previous, next] = wrapper.findAll('button')

    await previous.trigger('click')
    await next.trigger('click')

    expect(wrapper.emitted('change')).toEqual([[1], [3]])
  })

  it('emits nothing while a request is in flight', async () => {
    const wrapper = mountBar({ page: 2, hasPrevious: true, hasNext: true, busy: true })

    await wrapper.findAll('button')[1].trigger('click')

    expect(wrapper.emitted('change')).toBeUndefined()
  })
})
