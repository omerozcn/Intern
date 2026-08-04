import { createRouter, createWebHistory } from 'vue-router'

import { pinia } from '@/stores'
import { useAuthStore } from '@/stores/auth'

const routes = [
  {
    path: '/',
    name: 'dashboard',
    component: () => import('@/views/Home.vue'),
    meta: { requiresAuth: true, roles: ['Admin', 'User'], titleKey: 'pageTitles.dashboard' },
  },
  {
    path: '/sign-in',
    name: 'sign-in',
    component: () => import('@/views/SignIn.vue'),
    meta: { guestOnly: true, shell: false, titleKey: 'pageTitles.signIn' },
  },
  {
    path: '/reset-password',
    name: 'reset-password',
    component: () => import('@/views/ResetPassword.vue'),
    meta: { guestOnly: true, shell: false, titleKey: 'pageTitles.resetPassword' },
  },
  {
    path: '/ticket',
    name: 'create-ticket',
    component: () => import('@/views/Ticket.vue'),
    meta: { requiresAuth: true, roles: ['User'], titleKey: 'pageTitles.createTicket' },
  },
  {
    path: '/request',
    name: 'my-tickets',
    component: () => import('@/views/Request.vue'),
    meta: { requiresAuth: true, roles: ['User'], titleKey: 'pageTitles.myTickets' },
  },
  {
    path: '/communication',
    name: 'communication',
    component: () => import('@/views/Communication.vue'),
    meta: { requiresAuth: true, roles: ['User'], titleKey: 'pageTitles.feedback' },
  },
  {
    path: '/adminticket',
    name: 'admin-tickets',
    component: () => import('@/views/AdminTicket.vue'),
    meta: { requiresAuth: true, roles: ['Admin'], titleKey: 'pageTitles.adminTickets' },
  },
  {
    path: '/product',
    name: 'services',
    component: () => import('@/views/Product.vue'),
    meta: { requiresAuth: true, roles: ['Admin'], titleKey: 'pageTitles.services' },
  },
  {
    path: '/firm',
    name: 'firms',
    component: () => import('@/views/Firm.vue'),
    meta: { requiresAuth: true, roles: ['Admin'], titleKey: 'pageTitles.firms' },
  },
  {
    path: '/register',
    name: 'accounts',
    component: () => import('@/views/Register.vue'),
    meta: { requiresAuth: true, roles: ['Admin'], titleKey: 'pageTitles.accounts' },
  },
  {
    path: '/feedback',
    name: 'admin-feedback',
    component: () => import('@/views/Feedback.vue'),
    meta: { requiresAuth: true, roles: ['Admin'], titleKey: 'pageTitles.adminFeedback' },
  },
  {
    path: '/profile',
    name: 'profile',
    component: () => import('@/views/Profile.vue'),
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

  return true
})

export function resolvePostLoginRoute(role, requestedPath) {
  if (isSafeReturnUrl(requestedPath)) {
    const resolved = router.resolve(requestedPath)
    if (resolved.matched.length && resolved.name !== 'sign-in') {
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
