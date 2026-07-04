import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import type { User } from '@/types';
import { usersApi } from '@/api/users';
import { setCurrentUser } from '@/api/client';

interface AuthContextType {
  currentUser: User | null;
  users: User[];
  switchUser: (userId: string) => void;
  isLoading: boolean;
  error: string | null;
}

const AuthContext = createContext<AuthContextType>({
  currentUser: null,
  users: [],
  switchUser: () => {},
  isLoading: true,
  error: null,
});

export function AuthProvider({ children }: { children: ReactNode }) {
  const [users, setUsers] = useState<User[]>([]);
  const [currentUser, setCurrentUserState] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  useEffect(() => {
    usersApi.getAll().then(data => {
      setUsers(data);
      const savedId = localStorage.getItem('currentUserId');
      const user = data.find(u => u.id === savedId) ?? data[0];
      if (user) {
        setCurrentUserState(user);
        setCurrentUser(user.id);
      }
      setIsLoading(false);
    }).catch(() => {
      setError('Failed to load users. Is the API running?');
      setIsLoading(false);
    });
  }, []);

  const switchUser = (userId: string) => {
    const user = users.find(u => u.id === userId);
    if (user) {
      setCurrentUserState(user);
      setCurrentUser(user.id);
      localStorage.setItem('currentUserId', user.id);
      queryClient.clear();
      navigate('/');
    }
  };

  return (
    <AuthContext.Provider value={{ currentUser, users, switchUser, isLoading, error }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);
