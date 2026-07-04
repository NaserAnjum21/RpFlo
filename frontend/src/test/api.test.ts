import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const apiMock = vi.hoisted(() => ({
  defaults: { headers: { common: {} as Record<string, string> } },
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  delete: vi.fn(),
}));

vi.mock('@/api/client', () => ({
  default: apiMock,
  setCurrentUser: (userId: string) => {
    apiMock.defaults.headers.common['X-User-Id'] = userId;
  },
}));

describe('api wrappers', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    apiMock.defaults.headers.common = {};
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('setCurrentUser stores the simulated auth header', async () => {
    const { setCurrentUser } = await import('@/api/client');

    setCurrentUser('user-123');

    expect(apiMock.defaults.headers.common['X-User-Id']).toBe('user-123');
  });

  it('procurementApi forwards list params and unwraps paged data', async () => {
    const response = {
      items: [],
      page: 2,
      pageSize: 10,
      totalItems: 12,
      totalPages: 2,
    };
    apiMock.get.mockResolvedValueOnce({ data: response });
    const { procurementApi } = await import('@/api/procurement');

    const result = await procurementApi.getAll({
      page: 2,
      pageSize: 10,
      filter: 'pending',
      dateFrom: '2026-07-01',
      dateTo: '2026-07-04',
    });

    expect(result).toBe(response);
    expect(apiMock.get).toHaveBeenCalledWith('/procurement', {
      params: {
        page: 2,
        pageSize: 10,
        filter: 'pending',
        dateFrom: '2026-07-01',
        dateTo: '2026-07-04',
      },
    });
  });

  it('procurementApi posts workflow payloads to the expected endpoints', async () => {
    apiMock.post.mockResolvedValue({ data: { id: 'request-1' } });
    const { procurementApi } = await import('@/api/procurement');

    await procurementApi.approveByManager('request-1', 'Looks good');
    await procurementApi.rejectByFinance('request-1', 'Budget exceeded');
    await procurementApi.addComment('request-1', 'Please attach invoice');

    expect(apiMock.post).toHaveBeenNthCalledWith(
      1,
      '/procurement/request-1/approve/manager',
      { comment: 'Looks good' }
    );
    expect(apiMock.post).toHaveBeenNthCalledWith(
      2,
      '/procurement/request-1/reject/finance',
      { reason: 'Budget exceeded' }
    );
    expect(apiMock.post).toHaveBeenNthCalledWith(
      3,
      '/procurement/request-1/comments',
      { text: 'Please attach invoice' }
    );
  });

  it('procurementApi downloads exported PDFs with the request id in the filename', async () => {
    const objectUrl = 'blob:purchase-order';
    const createObjectURL = vi.spyOn(window.URL, 'createObjectURL').mockReturnValue(objectUrl);
    const revokeObjectURL = vi.spyOn(window.URL, 'revokeObjectURL').mockImplementation(() => {});
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});
    apiMock.get.mockResolvedValueOnce({ data: new Blob(['pdf']) });
    const { procurementApi } = await import('@/api/procurement');

    await procurementApi.exportPdf('request-42');

    expect(apiMock.get).toHaveBeenCalledWith('/procurement/request-42/export/pdf', {
      responseType: 'blob',
    });
    expect(createObjectURL.mock.calls[0]?.[0]).toBeInstanceOf(Blob);
    expect(click).toHaveBeenCalledTimes(1);
    expect(revokeObjectURL).toHaveBeenCalledWith(objectUrl);
  });

  it('users and notifications wrappers unwrap reads and keep read mutations fire-and-forget', async () => {
    apiMock.get
      .mockResolvedValueOnce({ data: [{ id: 'user-1' }] })
      .mockResolvedValueOnce({ data: 3 });
    apiMock.post.mockResolvedValue({ data: undefined });
    const { usersApi, notificationsApi } = await import('@/api/users');

    await expect(usersApi.getAll()).resolves.toEqual([{ id: 'user-1' }]);
    await expect(notificationsApi.getUnreadCount()).resolves.toBe(3);
    await notificationsApi.markAsRead('notification-1');
    await notificationsApi.markAllAsRead();

    expect(apiMock.get).toHaveBeenNthCalledWith(1, '/users');
    expect(apiMock.get).toHaveBeenNthCalledWith(2, '/notifications/unread-count');
    expect(apiMock.post).toHaveBeenNthCalledWith(1, '/notifications/notification-1/read');
    expect(apiMock.post).toHaveBeenNthCalledWith(2, '/notifications/read-all');
  });
});
