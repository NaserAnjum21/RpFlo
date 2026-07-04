import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { usersApi } from '@/api/users';
import { setCurrentUser } from '@/api/client';
import { AuthProvider, useAuth } from '@/hooks/useAuth';
import type { User } from '@/types';

vi.mock('@/api/users', () => ({
  usersApi: {
    getAll: vi.fn(),
  },
}));

vi.mock('@/api/client', () => ({
  setCurrentUser: vi.fn(),
}));

const users: User[] = [
  {
    id: 'requester-1',
    name: 'Alice Johnson',
    email: 'alice@company.com',
    role: 'Requester',
    department: 'Engineering',
  },
  {
    id: 'manager-1',
    name: 'Bob Smith',
    email: 'bob@company.com',
    role: 'Manager',
    department: 'Operations',
  },
];

function AuthProbe() {
  const { currentUser, users: loadedUsers, switchUser, isLoading, error } = useAuth();
  const location = useLocation();

  if (isLoading) return <div>Loading auth</div>;
  if (error) return <div>{error}</div>;

  return (
    <div>
      <p>Current: {currentUser?.name}</p>
      <p>Users loaded: {loadedUsers.length}</p>
      <p>Path: {location.pathname}</p>
      <button type="button" onClick={() => switchUser('manager-1')}>Switch to Bob</button>
    </div>
  );
}

function renderAuth(queryClient = new QueryClient()) {
  return {
    queryClient,
    ...render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/requests']}>
          <AuthProvider>
            <AuthProbe />
          </AuthProvider>
        </MemoryRouter>
      </QueryClientProvider>
    ),
  };
}

describe('AuthProvider', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('loads users, prefers the saved current user, and sets the API header', async () => {
    localStorage.setItem('currentUserId', 'manager-1');
    vi.mocked(usersApi.getAll).mockResolvedValue(users);

    renderAuth();

    expect(await screen.findByText('Current: Bob Smith')).toBeInTheDocument();
    expect(screen.getByText('Users loaded: 2')).toBeInTheDocument();
    expect(setCurrentUser).toHaveBeenCalledWith('manager-1');
  });

  it('switches user, persists the choice, clears cached queries, and navigates home', async () => {
    vi.mocked(usersApi.getAll).mockResolvedValue(users);
    const queryClient = new QueryClient();
    const clear = vi.spyOn(queryClient, 'clear');

    renderAuth(queryClient);
    expect(await screen.findByText('Current: Alice Johnson')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Switch to Bob' }));

    expect(await screen.findByText('Current: Bob Smith')).toBeInTheDocument();
    expect(localStorage.getItem('currentUserId')).toBe('manager-1');
    expect(setCurrentUser).toHaveBeenLastCalledWith('manager-1');
    expect(clear).toHaveBeenCalledTimes(1);
    expect(screen.getByText('Path: /')).toBeInTheDocument();
  });

  it('surfaces a friendly error when users cannot be loaded', async () => {
    vi.mocked(usersApi.getAll).mockRejectedValue(new Error('offline'));

    renderAuth();

    await waitFor(() => {
      expect(screen.getByText('Failed to load users. Is the API running?')).toBeInTheDocument();
    });
    expect(setCurrentUser).not.toHaveBeenCalled();
  });
});
