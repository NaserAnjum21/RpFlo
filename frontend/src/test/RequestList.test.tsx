import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { procurementApi } from '@/api/procurement';
import { RequestList } from '@/pages/RequestList';
import type { PagedResult, ProcurementListItem, User } from '@/types';

const authState = vi.hoisted(() => ({
  currentUser: null as User | null,
}));

vi.mock('@/hooks/useAuth', () => ({
  useAuth: () => ({
    currentUser: authState.currentUser,
  }),
}));

vi.mock('@/api/procurement', () => ({
  procurementApi: {
    getAll: vi.fn(),
    getMy: vi.fn(),
  },
}));

const requester: User = {
  id: 'requester-1',
  name: 'Alice Johnson',
  email: 'alice@company.com',
  role: 'Requester',
  department: 'Engineering',
};

const manager: User = {
  id: 'manager-1',
  name: 'Bob Smith',
  email: 'bob@company.com',
  role: 'Manager',
  department: 'Operations',
};

const listItem: ProcurementListItem = {
  id: 'request-1',
  title: 'New workstations',
  department: 'Engineering',
  urgency: 'High',
  status: 'Submitted',
  totalAmount: 2450,
  currency: 'USD',
  requesterName: 'Alice Johnson',
  createdAt: '2026-07-01T08:00:00.000Z',
  updatedAt: '2026-07-02T08:00:00.000Z',
};

function paged(items: ProcurementListItem[]): PagedResult<ProcurementListItem> {
  return {
    items,
    page: 1,
    pageSize: 10,
    totalItems: items.length,
    totalPages: 1,
  };
}

function renderRequestList() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <RequestList />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('RequestList', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    authState.currentUser = requester;
    vi.mocked(procurementApi.getAll).mockResolvedValue(paged([listItem]));
    vi.mocked(procurementApi.getMy).mockResolvedValue(paged([listItem]));
  });

  it('uses the requester-scoped endpoint and hides requester-only organization columns', async () => {
    authState.currentUser = requester;

    renderRequestList();

    expect(await screen.findByRole('heading', { name: 'My Requests' })).toBeInTheDocument();
    await waitFor(() => {
      expect(procurementApi.getMy).toHaveBeenCalledWith(expect.objectContaining({
        page: 1,
        pageSize: 10,
        filter: 'all',
      }));
    });
    expect(await screen.findByRole('link', { name: 'New workstations' })).toHaveAttribute('href', '/requests/request-1');
    expect(screen.getByRole('link', { name: /new request/i })).toHaveAttribute('href', '/requests/new');
    expect(screen.queryByRole('columnheader', { name: 'Requester' })).not.toBeInTheDocument();
    expect(procurementApi.getAll).not.toHaveBeenCalled();
  });

  it('uses the organization endpoint for managers and sends tab/date filters', async () => {
    authState.currentUser = manager;
    const { container } = renderRequestList();

    expect(await screen.findByRole('heading', { name: 'Procurement Requests' })).toBeInTheDocument();
    await waitFor(() => {
      expect(procurementApi.getAll).toHaveBeenCalledWith(expect.objectContaining({
        filter: 'all',
      }));
    });
    expect(await screen.findByRole('columnheader', { name: 'Requester' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /new request/i })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('tab', { name: 'Pending' }));
    await waitFor(() => {
      expect(procurementApi.getAll).toHaveBeenLastCalledWith(expect.objectContaining({
        filter: 'pending',
      }));
    });

    const [dateFrom, dateTo] = Array.from(container.querySelectorAll('input[type="date"]'));
    fireEvent.change(dateFrom!, { target: { value: '2026-07-01' } });
    fireEvent.change(dateTo!, { target: { value: '2026-07-04' } });

    await waitFor(() => {
      expect(procurementApi.getAll).toHaveBeenLastCalledWith(expect.objectContaining({
        filter: 'pending',
        dateFrom: '2026-07-01',
        dateTo: '2026-07-04',
      }));
    });
    expect(procurementApi.getMy).not.toHaveBeenCalled();
  });
});
