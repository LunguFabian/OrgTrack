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
          path: 'national-dashboard',
          name: 'national-dashboard',
          component: () => import('../views/NationalDashboardView.vue'),
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
        },
        {
          path: 'profile/:userId',
          name: 'user-profile',
          component: () => import('../views/UserProfileView.vue'),
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
    return '/login';
  }
  if (to.path === '/login' && authStore.isAuthenticated) {
    return '/';
  }
});

export default router;

