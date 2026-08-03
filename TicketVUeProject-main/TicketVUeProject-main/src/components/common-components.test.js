import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import { i18n } from '@/i18n'
import EmptyState from './EmptyState.vue'
import StatusBadge from './StatusBadge.vue'

const global = { plugins: [i18n] }

describe('shared state components', () => {
  it.each([
    ['pending', 'Bekliyor', 'status-badge--pending'],
    ['inProgress', 'İşlemde', 'status-badge--inProgress'],
    [3, 'Tamamlandı', 'status-badge--completed'],
  ])('renders the %s status consistently', (status, label, className) => {
    const wrapper = mount(StatusBadge, { props: { status }, global })
    expect(wrapper.text()).toContain(label)
    expect(wrapper.classes()).toContain(className)
  })

  it('renders the named empty-state action slot', () => {
    const wrapper = mount(EmptyState, {
      props: { title: 'Kayıt yok', message: 'Yeni kayıt ekleyin.' },
      slots: { actions: '<button type="button">Yeni kayıt</button>' },
      global,
    })

    expect(wrapper.get('button').text()).toBe('Yeni kayıt')
  })
})
