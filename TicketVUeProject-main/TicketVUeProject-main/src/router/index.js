import {ref} from "vue";
import { createRouter, createWebHistory } from "vue-router";
import axios from "axios";

let isAdmin = ref(false);
let userRole = null;




const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: "/", name: "Home", component: () => import("../views/Home.vue"), meta: { requiresAuth: true } },
    {
      path: "/register",
      component: () => import("../views/Register.vue" ), meta: { requiresAuth: true , requiredRole: 'Admin' }
    },
    {
      path: "/sign-in",
      component: () => import("../views/SignIn.vue") },
    {
      path: "/firm",
      component: () => import("../views/Firm.vue"), meta: { requiresAuth: true , requiredRole: 'Admin' }
    },
    {
      path: "/product",
      component: () => import("../views/Product.vue"), meta: { requiresAuth: true , requiredRole: 'Admin' }
    },
    {
      path: "/ticket",
      component: () => import("../views/Ticket.vue"), meta: { requiresAuth: true }
    },
    {
      path: "/profile",
      component: () => import("../views/Profile.vue"), meta: { requiresAuth: true }
    },
    {
      path: "/request",
      component: () => import("../views/Request.vue"), meta: { requiresAuth: true }
    },
    {
      path: "/communication",
      component: () => import("../views/Communication.vue"), meta: { requiresAuth: true }
    },
    {
      path: "/adminticket",
      component: () => import("../views/AdminTicket.vue"), meta: { requiresAuth: true , requiredRole: 'Admin' }
    },
    {
      path: "/feedback",
      component: () => import("../views/Feedback.vue"), meta: { requiresAuth: true , requiredRole: 'Admin' }
    },
    { path: "/:pathMatch(.*)*", redirect: "/" },
  ],
});



router.beforeEach(async (to, from, next) => {
  const isAuthenticated = !!sessionStorage.getItem('token');
  let userRole = null;


  if (isAuthenticated) {
    let usertoken = sessionStorage.getItem('token');

    try {
      const response = await axios.post('http://localhost:5005/api/Account/getuserRole', {
        token: usertoken
      }, {
        headers: {
          'Content-Type': 'application/json'
        }
      });
      userRole = response.data;
    } catch (error) {
      console.error('Error fetching user role:', error);
      next('/sign-in');
      sessionStorage.clear();
      return;
    }

    if (to.matched.some(record => record.meta.requiresAuth) && !isAuthenticated) {
      next('/sign-in');
      sessionStorage.clear();
    } else {
      const requiredRole = to.meta.requiredRole;
      if (requiredRole && userRole !== requiredRole) {
        next('/');
        return;
      }
      next();
    }
  } else {
    if (to.matched.some(record => record.meta.requiresAuth)) {
      next('/sign-in');
      sessionStorage.clear();
    } else {
      next();
    }
  }
  console.log('User Role:', userRole);
});




export default router;
