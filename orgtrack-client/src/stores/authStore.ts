import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { User, AuthResponse } from '../types/auth';
import { api } from '../api/axios';

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null);
  const accessToken = ref<string | null>(localStorage.getItem('access_token'));
  const isAuthenticated = computed(() => !!accessToken.value && !!user.value);

  function setAuthData(data: AuthResponse) {
    accessToken.value = data.token;
    user.value = data.user;
    localStorage.setItem('access_token', data.token);
    localStorage.setItem('refresh_token', data.refreshToken);
    localStorage.setItem('user', JSON.stringify(data.user));
  }

  function clearAuthData() {
    accessToken.value = null;
    user.value = null;
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('user');
  }
  function loadUserFromStorage() {
    const storedUser = localStorage.getItem('user');
    if (storedUser && accessToken.value) {
      user.value = JSON.parse(storedUser);
    }
  }

  async function loginWithGoogle(idToken: string) {
    const response = await api.post<AuthResponse>('/auth/google', { idToken });
    setAuthData(response.data);
  }

  async function devLogin(email: string) {
    const response = await api.post<AuthResponse>('/auth/dev-login', { email });
    setAuthData(response.data);
  }

  function logout() {
    clearAuthData();
  }

  return {
    user,
    accessToken,
    isAuthenticated,
    setAuthData,
    clearAuthData,
    loadUserFromStorage,
    loginWithGoogle,
    devLogin,
    logout
  };
});
