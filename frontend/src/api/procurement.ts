import type {
  ProcurementListItem,
  ProcurementResponse,
  CreateProcurementRequest,
  DashboardMetrics,
  CommentResponse,
  PagedResult,
  ProcurementListPageParams,
  ProcurementTaskPageParams,
} from '@/types';
import api from './client';

export const procurementApi = {
  getAll: (params: ProcurementListPageParams) =>
    api.get<PagedResult<ProcurementListItem>>('/procurement', { params }).then(r => r.data),

  getById: (id: string) =>
    api.get<ProcurementResponse>(`/procurement/${id}`).then(r => r.data),

  getMy: (params: ProcurementListPageParams) =>
    api.get<PagedResult<ProcurementListItem>>('/procurement/my', { params }).then(r => r.data),

  getPending: (params: ProcurementTaskPageParams) =>
    api.get<PagedResult<ProcurementListItem>>('/procurement/pending', { params }).then(r => r.data),

  create: (data: CreateProcurementRequest) =>
    api.post<ProcurementResponse>('/procurement', data).then(r => r.data),

  update: (id: string, data: { title: string; description: string; department: string; urgency: string }) =>
    api.put<ProcurementResponse>(`/procurement/${id}`, data).then(r => r.data),

  addLineItems: (id: string, lineItems: { name: string; quantity: number; unitPrice: number }[]) =>
    api.post<ProcurementResponse>(`/procurement/${id}/line-items`, { lineItems }).then(r => r.data),

  removeLineItem: (id: string, lineItemId: string) =>
    api.delete<ProcurementResponse>(`/procurement/${id}/line-items/${lineItemId}`).then(r => r.data),

  submit: (id: string) =>
    api.post<ProcurementResponse>(`/procurement/${id}/submit`).then(r => r.data),

  approveByManager: (id: string, comment?: string) =>
    api.post<ProcurementResponse>(`/procurement/${id}/approve/manager`, { comment }).then(r => r.data),

  rejectByManager: (id: string, reason: string) =>
    api.post<ProcurementResponse>(`/procurement/${id}/reject/manager`, { reason }).then(r => r.data),

  approveByFinance: (id: string, comment?: string) =>
    api.post<ProcurementResponse>(`/procurement/${id}/approve/finance`, { comment }).then(r => r.data),

  rejectByFinance: (id: string, reason: string) =>
    api.post<ProcurementResponse>(`/procurement/${id}/reject/finance`, { reason }).then(r => r.data),

  issuePo: (id: string) =>
    api.post<ProcurementResponse>(`/procurement/${id}/issue-po`).then(r => r.data),

  reviseToDraft: (id: string) =>
    api.post<ProcurementResponse>(`/procurement/${id}/revise`).then(r => r.data),

  addComment: (id: string, text: string) =>
    api.post<CommentResponse>(`/procurement/${id}/comments`, { text }).then(r => r.data),

  getMetrics: () =>
    api.get<DashboardMetrics>('/procurement/metrics').then(r => r.data),

  exportCsv: () =>
    api.get('/export/csv', { responseType: 'blob' }).then(r => {
      const url = window.URL.createObjectURL(new Blob([r.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', 'procurement-requests.csv');
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    }),

  exportPdf: (id: string) =>
    api.get(`/procurement/${id}/export/pdf`, { responseType: 'blob' }).then(r => {
      const url = window.URL.createObjectURL(new Blob([r.data], { type: 'application/pdf' }));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `purchase-order-${id}.pdf`);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    }),
};
