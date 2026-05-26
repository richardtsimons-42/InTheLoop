import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5000',
});

export const userApi = {
  getProfile: async () => {
    const response = await api.get('/api/users/me');
    return response.data;
  },

  updateProfile: async (firstName: string, lastName: string) => {
    const response = await api.put('/api/users/me', { firstName, lastName });
    return response.data;
  },

  updateAvatar: async (avatarUrl: string) => {
    const response = await api.put('/api/users/me/avatar', { avatarUrl });
    return response.data;
  },
};

export default api;
