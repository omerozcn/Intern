import { createRouter, createWebHistory } from 'vue-router'

import { pinia } from '@/stores'
import { useAuthStore } from '@/stores/auth'

const routes = [
  {
    path: '/',
    name: 'dashboard',
    component: () => import('@/views/DashboardView.vue'),
    meta: { requiresAuth: true, roles: ['Admin', 'User'], titleKey: 'pageTitles.dashboard' },
  },
  {
    path: '/sign-in',
    name: 'sign-in',
    component: () => import('@/views/SignInView.vue'),
    meta: { guestOnly: true, shell: false, titleKey: 'pageTitles.signIn' },
  },
  {
    path: '/reset-password',
    name: 'reset-password',
    component: () => import('@/views/ResetPasswordView.vue'),
    meta: { guestOnly: true, shell: false, titleKey: 'pageTitles.resetPassword' },
  },
  {
    path: '/tickets/new',
    name: 'create-ticket',
    component: () => import('@/views/CreateTicketView.vue'),
    meta: { requiresAuth: true, roles: ['User'], titleKey: 'pageTitles.createTicket' },
  },
  {
    path: '/tickets',
    name: 'my-tickets',
    component: () => import('@/views/MyTicketsView.vue'),
    meta: { requiresAuth: true, roles: ['User'], titleKey: 'pageTitles.myTickets' },
  },
  {
    path: '/feedback/new',
    name: 'send-feedback',
    component: () => import('@/views/SendFeedbackView.vue'),
    meta: { requiresAuth: true, roles: ['User'], titleKey: 'pageTitles.feedback' },
  },
  {
    path: '/admin/tickets',
    name: 'admin-tickets',
    component: () => import('@/views/AdminTicketsView.vue'),
    meta: { requiresAuth: true, roles: ['Admin'], titleKey: 'pageTitles.adminTickets' },
  },
  {
    path: '/services',
    name: 'services',
    component: () => import('@/views/ServicesView.vue'),
    meta: { requiresAuth: true, roles: ['Admin'], titleKey: 'pageTitles.services', superAdmin: true },
  },
  {
    path: '/firms',
    name: 'firms',
    component: () => import('@/views/FirmsView.vue'),
    meta: { requiresAuth: true, roles: ['Admin'], titleKey: 'pageTitles.firms', superAdmin: true },
  },
  {
    path: '/accounts',
    name: 'accounts',
    component: () => import('@/views/AccountsView.vue'),
    meta: { requiresAuth: true, roles: ['Admin'], titleKey: 'pageTitles.accounts' },
  },
  {
    path: '/admin/feedback',
    name: 'admin-feedback',
    component: () => import('@/views/AdminFeedbackView.vue'),
    meta: { requiresAuth: true, roles: ['Admin'], titleKey: 'pageTitles.adminFeedback' },
  },
  {
    path: '/profile',
    name: 'profile',
    component: () => import('@/views/ProfileView.vue'),
    meta: { requiresAuth: true, roles: ['Admin', 'User'], titleKey: 'pageTitles.profile' },
  },
  { path: '/:pathMatch(.*)*', redirect: '/' },
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior(_to, _from, savedPosition) {
    return savedPosition || { top: 0 }
  },
})

router.beforeEach(async (to) => {
  const auth = useAuthStore(pinia)
  await auth.hydrate()

  if (to.meta.guestOnly && auth.isAuthenticated) {
    return resolvePostLoginRoute(auth.role, to.query.returnUrl)
  }

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return {
      name: 'sign-in',
      query: to.fullPath === '/' ? {} : { returnUrl: to.fullPath },
    }
  }

  const allowedRoles = to.meta.roles
  if (allowedRoles?.length && !allowedRoles.includes(auth.role)) {
    return auth.isAuthenticated ? { name: 'dashboard' } : { name: 'sign-in' }
  }

  // Platform-wide screens. The API rejects these calls independently; this only keeps
  // an administrator from landing on a page that would fail every request.
  if (to.meta.superAdmin && !auth.isSuperAdmin) {
    return auth.isAuthenticated ? { name: 'dashboard' } : { name: 'sign-in' }
  }

  return true
})

export function resolvePostLoginRoute(role, requestedPath) {
  if (isSafeReturnUrl(requestedPath)) {
    const resolved = router.resolve(requestedPath)
    if (resolved.matched.length && resolved.name !== 'sign-in') {
      // Only the role is checked here: this runs before the session exists, so the
      // super-admin flag is not known yet. The guard above re-checks on arrival.
      const roles = resolved.meta.roles
      if (!roles?.length || roles.includes(role)) return requestedPath
    }
  }

  return { name: 'dashboard' }
}

function isSafeReturnUrl(value) {
  return typeof value === 'string' && value.startsWith('/') && !value.startsWith('//')
}

export default router
