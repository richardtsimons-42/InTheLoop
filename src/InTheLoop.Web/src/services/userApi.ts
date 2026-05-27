import axios from 'axios';

const api = axios.create({
  baseURL: '/api',
});

api.interceptors.request.use(config => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export const userApi = {
  getProfile: async () => {
    const response = await api.get('/users/me');
    return response.data;
  },

  updateProfile: async (firstName: string, lastName: string, avatarUrl?: string) => {
    const response = await api.put('/users/me', { firstName, lastName, avatarUrl });
    return response.data;
  },

  getUserStatus: async (userId: string) => {
    const response = await api.get(`/users/${userId}/status`);
    return response.data;
  },

  updateAvatar: async (avatarUrl: string) => {
    const response = await api.put('/users/me/avatar', { avatarUrl });
    return response.data;
  },
};

export default api;
