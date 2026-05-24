import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '../stores/authStore';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('../views/LoginView.vue')
    },
    {
      path: '/invite/:token',
      name: 'invite',
      component: () => import('../views/InviteView.vue')
    },
    {
      path: '/',
      component: () => import('../components/layout/AppLayout.vue'),
      meta: { requiresAuth: true },
      children: [
        {
          path: '',
          name: 'dashboard',
          component: () => import('../views/HomeView.vue'),
        },
        {
          path: 'organization',
          name: 'organization',
          component: () => import('../views/OrganizationView.vue'),
        },
        {
          path: 'units/:id',
          name: 'unit-dashboard',
          component: () => import('../views/UnitDashboardView.vue'),
        },
        {
          path: 'my-tasks',
          name: 'my-tasks',
          component: () => import('../views/MyTasksView.vue'),
        },
        {
          path: 'events',
          name: 'events',
          component: () => import('../views/MyEventsView.vue'),
        },
        {
          path: 'profile',
          name: 'profile',
          component: () => import('../views/ProfileView.vue'),
        }
      ]
    },
    {
      path: '/403',
      name: 'forbidden',
      component: () => import('../views/ForbiddenView.vue')
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: () => import('../views/NotFoundView.vue')
    }
  ]
});
router.beforeEach((to) => {
  const authStore = useAuthStore();

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return '/login'; // Dacă nu e logat, îl trimitem la Login
  }
  if (to.path === '/login' && authStore.isAuthenticated) {
    return '/'; // Dacă e deja logat, nu are ce căuta pe pagina de Login
  }
});

export default router;
