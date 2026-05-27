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

export const familiesApi = {
  getMyFamilies: () => api.get('/families'),
  createFamily: (name: string, description?: string) => api.post('/families', { name, description }),
  joinFamily: (familyId: number) => api.post(`/families/${familyId}/join`),
  leaveFamily: (familyId: number) => api.post(`/families/${familyId}/leave`),
  updateFamily: (familyId: number, name?: string, description?: string, coverPhotoUrl?: string) =>
    api.put(`/families/${familyId}`, { name, description, coverPhotoUrl }),
  inviteMember: (familyId: number, email: string) => api.post(`/families/${familyId}/invite`, { email }),
};

export const postsApi = {
  getFeed: (familyId: number, skip = 0, take = 20) => api.get(`/posts/${familyId}/feed`, { params: { skip, take } }),
  createPost: (familyId: number, content: string) => api.post('/posts', { familyId, content }),
  uploadPhoto: (postId: number, file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    return api.post(`/posts/${postId}/photos`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
  getComments: (postId: number) => api.get(`/comments/posts/${postId}`),
  addComment: (postId: number, content: string) => api.post(`/comments/posts/${postId}`, { content }),
  addReply: (commentId: number, postId: number, content: string) => api.post(`/comments/${commentId}/reply`, { postId, content }),
};

export const messagesApi = {
  send: (content: string, recipientId?: string, familyId?: number) =>
    api.post('/messages', { recipientId, familyId, content }),
  getConversation: (recipientId?: string, familyId?: number) =>
    api.get('/messages/conversation', { params: { recipientId, familyId } }),
};

export const authApi = {
  logout: () => api.post('/auth/logout'),
};
