import { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import type { User } from '@/types';
import { usersApi } from '@/api/users';
import { setCurrentUser } from '@/api/client';

interface AuthContextType {
  currentUser: User | null;
  users: User[];
  switchUser: (userId: string) => void;
  isLoading: boolean;
}

const AuthContext = createContext<AuthContextType>({
  currentUser: null,
  users: [],
  switchUser: () => {},
  isLoading: true,
});

export function AuthProvider({ children }: { children: ReactNode }) {
  const [users, setUsers] = useState<User[]>([]);
  const [currentUser, setCurrentUserState] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

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
    });
  }, []);

  const switchUser = (userId: string) => {
    const user = users.find(u => u.id === userId);
    if (user) {
      setCurrentUserState(user);
      setCurrentUser(user.id);
      localStorage.setItem('currentUserId', user.id);
    }
  };

  return (
    <AuthContext.Provider value={{ currentUser, users, switchUser, isLoading }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);
