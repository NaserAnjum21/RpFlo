import type { User, NotificationResponse } from '@/types';
import api from './client';

export const usersApi = {
  getAll: () => api.get<User[]>('/users').then(r => r.data),
  getById: (id: string) => api.get<User>(`/users/${id}`).then(r => r.data),
};

export const notificationsApi = {
  getAll: () => api.get<NotificationResponse[]>('/notifications').then(r => r.data),
  getUnreadCount: () => api.get<number>('/notifications/unread-count').then(r => r.data),
  markAsRead: (id: string) => api.post(`/notifications/${id}/read`),
  markAllAsRead: () => api.post('/notifications/read-all'),
};
