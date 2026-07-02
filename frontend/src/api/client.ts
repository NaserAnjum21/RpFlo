import axios from 'axios';

const api = axios.create({
  baseURL: '/api',
});

export function setCurrentUser(userId: string) {
  api.defaults.headers.common['X-User-Id'] = userId;
}

export default api;
