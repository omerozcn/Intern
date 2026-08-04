import { createI18n } from 'vue-i18n'

import en from './en.json'
import tr from './tr.json'

const LOCALE_KEY = 'turkuvaz.locale'
const SUPPORTED_LOCALES = ['tr', 'en']
const storedLocale = localStorage.getItem(LOCALE_KEY)
const initialLocale = SUPPORTED_LOCALES.includes(storedLocale) ? storedLocale : 'tr'

export const i18n = createI18n({
  legacy: false,
  locale: initialLocale,
  fallbackLocale: 'tr',
  missingWarn: import.meta.env.DEV,
  fallbackWarn: import.meta.env.DEV,
  messages: { tr, en },
  datetimeFormats: {
    tr: {
      short: { year: 'numeric', month: 'short', day: 'numeric' },
      long: {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      },
    },
    en: {
      short: { year: 'numeric', month: 'short', day: 'numeric' },
      long: {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      },
    },
  },
})

export const supportedLocales = Object.freeze([
  { code: 'tr', label: 'Türkçe', shortLabel: 'TR' },
  { code: 'en', label: 'English', shortLabel: 'EN' },
])

export function setLocale(locale) {
  const nextLocale = SUPPORTED_LOCALES.includes(locale) ? locale : 'tr'
  i18n.global.locale.value = nextLocale
  localStorage.setItem(LOCALE_KEY, nextLocale)
  document.documentElement.lang = nextLocale
}

export function formatDate(value, format = 'long') {
  if (!value) return '—'
  const date = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(date.getTime())) return '—'
  return i18n.global.d(date, format)
}
