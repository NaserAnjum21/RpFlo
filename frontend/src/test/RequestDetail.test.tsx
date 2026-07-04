import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { RequestDetail } from '@/pages/RequestDetail';
import { procurementApi } from '@/api/procurement';

vi.mock('@/api/procurement', () => ({
  procurementApi: {
    getById: vi.fn(),
    submit: vi.fn(),
    approveByManager: vi.fn(),
    rejectByManager: vi.fn(),
    approveByFinance: vi.fn(),
    rejectByFinance: vi.fn(),
    issuePo: vi.fn(),
    reviseToDraft: vi.fn(),
    addComment: vi.fn(),
    update: vi.fn(),
    removeLineItem: vi.fn(),
    addLineItems: vi.fn(),
    exportPdf: vi.fn(),
  },
}));

vi.mock('@/hooks/useAuth', () => ({
  useAuth: () => ({
    currentUser: {
      id: '11111111-1111-1111-1111-111111111111',
      name: 'Alice Johnson',
      email: 'alice@company.com',
      role: 'Requester',
      department: 'Engineering',
    },
  }),
}));

describe('RequestDetail', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders access denied when the request detail API returns 403', async () => {
    vi.mocked(procurementApi.getById).mockRejectedValue({
      response: {
        status: 403,
        data: {
          code: 'Unauthorized.AccessDenied',
          message: 'You do not have access to this procurement request.',
        },
      },
    });

    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/requests/bob-request-id']}>
          <Routes>
            <Route path="/requests/:id" element={<RequestDetail />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(await screen.findByRole('heading', { name: 'Access denied' })).toBeInTheDocument();
    expect(screen.getByText('You do not have permission to view this procurement request.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Back to requests' })).toBeInTheDocument();
    expect(screen.queryByText('Description')).not.toBeInTheDocument();
  });
});
