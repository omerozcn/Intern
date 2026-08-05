/* The API stores timestamps in UTC but serialises them without an offset, so a
   bare `new Date(value)` reads them as local time and every relative label comes
   out wrong by the timezone. Anything that parses an API timestamp goes through
   here. */

const HAS_TIMEZONE = /(?:Z|[+-]\d{2}:?\d{2})$/i

export function parseApiDate(value) {
  if (!value) return null
  if (value instanceof Date) return Number.isNaN(value.getTime()) ? null : value

  const raw = String(value)
  const date = new Date(HAS_TIMEZONE.test(raw) ? raw : `${raw}Z`)
  return Number.isNaN(date.getTime()) ? null : date
}

const UNITS = [
  ['year', 31536000000],
  ['month', 2592000000],
  ['week', 604800000],
  ['day', 86400000],
  ['hour', 3600000],
  ['minute', 60000],
]

/* Intl.RelativeTimeFormat rather than hand-written strings: it gets Turkish and
   English plural rules right on its own, and `numeric: 'auto'` produces "dün"
   and "yesterday" instead of "1 gün önce". */
export function relativeTime(value, locale = 'tr') {
  const date = parseApiDate(value)
  if (!date) return ''

  const diff = date.getTime() - Date.now()
  const formatter = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' })

  for (const [unit, ms] of UNITS) {
    if (Math.abs(diff) >= ms) return formatter.format(Math.round(diff / ms), unit)
  }
  return formatter.format(0, 'minute')
}

/* Whole days elapsed, for ageing badges where "3" needs to be a number rather
   than a phrase. */
export function daysSince(value) {
  const date = parseApiDate(value)
  if (!date) return null
  return Math.max(0, Math.floor((Date.now() - date.getTime()) / 86400000))
}
