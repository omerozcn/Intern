/* The single source of navigation.

   The sidebar, the mobile drawer and the command palette all read this list. It
   used to be inlined in AppShell, which meant anything else offering navigation
   had to duplicate it and could silently drift out of sync — including on the
   role filter, where drift shows a link the user is not allowed to follow. */

export const navLinks = Object.freeze([
  { name: 'dashboard', labelKey: 'nav.dashboard', icon: 'bi-grid-1x2', group: 'overview' },
  {
    name: 'create-ticket',
    labelKey: 'nav.createTicket',
    icon: 'bi-plus-square',
    group: 'requests',
    roles: ['User'],
  },
  { name: 'my-tickets', labelKey: 'nav.myTickets', icon: 'bi-inbox', group: 'requests', roles: ['User'] },
  {
    name: 'send-feedback',
    labelKey: 'nav.sendFeedback',
    icon: 'bi-chat-square-text',
    group: 'requests',
    roles: ['User'],
  },
  { name: 'admin-tickets', labelKey: 'nav.tickets', icon: 'bi-kanban', group: 'requests', roles: ['Admin'] },
  // The service and firm catalogues are platform-wide, so they belong to the firm that
  // owns the platform rather than to every administrator.
  { name: 'services', labelKey: 'nav.services', icon: 'bi-box-seam', group: 'catalog', roles: ['Admin'], superAdmin: true },
  { name: 'firms', labelKey: 'nav.firms', icon: 'bi-buildings', group: 'catalog', roles: ['Admin'], superAdmin: true },
  { name: 'accounts', labelKey: 'nav.accounts', icon: 'bi-people', group: 'admin', roles: ['Admin'] },
  {
    name: 'admin-feedback',
    labelKey: 'nav.feedback',
    icon: 'bi-chat-left-dots',
    group: 'admin',
    roles: ['Admin'],
  },
])

export function visibleNavLinks(role, isSuperAdmin = false) {
  return navLinks.filter(
    (link) =>
      (!link.roles || link.roles.includes(role)) && (!link.superAdmin || isSuperAdmin),
  )
}

/* Shortcuts that are not routes. Kept here so the palette and any future
   quick-action surface agree on what exists per role. */
export const quickActions = Object.freeze([
  {
    id: 'pending-queue',
    labelKey: 'palette.pendingQueue',
    icon: 'bi-hourglass-split',
    roles: ['Admin'],
    to: { name: 'admin-tickets', query: { status: 'pending' } },
  },
  {
    id: 'new-ticket',
    labelKey: 'palette.newTicket',
    icon: 'bi-plus-lg',
    roles: ['User'],
    to: { name: 'create-ticket' },
  },
])
