import { readdirSync, readFileSync } from 'node:fs'
import { join } from 'node:path'

import { describe, expect, it } from 'vitest'

/*
  A literal colour in a component only ever looks right in the theme it was
  written for. Every one of the twenty-one that used to be scattered through
  these files was a light-theme value, and each would have survived the flip to
  dark as a white card or unreadable text.

  There is no stylelint in this project and adding one for a single rule is not
  worth the dependency, so the rule lives here instead: it runs in the test suite
  that already exists and fails the first time someone pastes a hex.

  If a colour genuinely cannot be expressed as a token, add the token — that is
  the point. Theme-independent keywords are allowed.
*/

// Vite rewrites import.meta.url to a non-file scheme, so the path is anchored on
// the Vitest root (package.json's directory) instead.
const SRC = join(process.cwd(), 'src')
const SCANNED_DIRS = ['views', 'components']

const HEX = /#[0-9a-fA-F]{3,8}\b/
const FUNCTIONAL = /\b(?:rgba?|hsla?|hwb|lab|lch|oklab|oklch|color)\(/
const NAMED = /(?:^|[:,\s])(?:white|black|red|green|blue|yellow|orange|purple|pink|gray|grey|silver|navy|teal|olive|maroon|lime|aqua|fuchsia)\s*(?:;|$|\))/

function collectVueFiles(dir) {
  const entries = readdirSync(join(SRC, dir), { withFileTypes: true })
  return entries
    .filter((entry) => entry.isFile() && entry.name.endsWith('.vue'))
    .map((entry) => ({ path: `${dir}/${entry.name}`, dir, name: entry.name }))
}

function styleBlocks(source) {
  return [...source.matchAll(/<style[^>]*>([\s\S]*?)<\/style>/g)].map((match) => match[1])
}

function stripComments(css) {
  return css.replace(/\/\*[\s\S]*?\*\//g, '')
}

const files = SCANNED_DIRS.flatMap(collectVueFiles)

describe('component styles use theme tokens', () => {
  it('finds the files it is meant to be guarding', () => {
    // A refactor that moves or renames these directories would otherwise turn
    // this suite into a silently passing no-op.
    expect(files.length).toBeGreaterThan(15)
  })

  it.each(files)('$path declares no literal colours', ({ path }) => {
    const source = readFileSync(join(SRC, path), 'utf8')
    const offenders = []

    for (const block of styleBlocks(source)) {
      stripComments(block)
        .split('\n')
        .forEach((line, index) => {
          if (HEX.test(line) || FUNCTIONAL.test(line) || NAMED.test(line)) {
            offenders.push(`${index + 1}: ${line.trim()}`)
          }
        })
    }

    expect(offenders, `${path} should use var(--tv-*) tokens instead`).toEqual([])
  })
})
