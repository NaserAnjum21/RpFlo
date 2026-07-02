import { Badge } from '@/components/ui/badge';
import type { ProcurementStatus } from '@/types';

const statusConfig: Record<ProcurementStatus, { label: string; className: string }> = {
  Draft: { label: 'Draft', className: 'bg-gray-100 text-gray-800 border-gray-200' },
  Submitted: { label: 'Submitted', className: 'bg-blue-100 text-blue-800 border-blue-200' },
  ManagerApproved: { label: 'Manager Approved', className: 'bg-indigo-100 text-indigo-800 border-indigo-200' },
  ManagerRejected: { label: 'Manager Rejected', className: 'bg-red-100 text-red-800 border-red-200' },
  FinanceApproved: { label: 'Finance Approved', className: 'bg-emerald-100 text-emerald-800 border-emerald-200' },
  FinanceRejected: { label: 'Finance Rejected', className: 'bg-red-100 text-red-800 border-red-200' },
  PurchaseOrderIssued: { label: 'PO Issued', className: 'bg-green-100 text-green-800 border-green-200' },
};

const urgencyConfig: Record<string, string> = {
  Low: 'bg-gray-100 text-gray-700',
  Medium: 'bg-yellow-100 text-yellow-800',
  High: 'bg-orange-100 text-orange-800',
  Critical: 'bg-red-100 text-red-800',
};

export function StatusBadge({ status }: { status: ProcurementStatus }) {
  const config = statusConfig[status] ?? { label: status, className: '' };
  return <Badge variant="outline" className={config.className}>{config.label}</Badge>;
}

export function UrgencyBadge({ urgency }: { urgency: string }) {
  return <Badge variant="secondary" className={urgencyConfig[urgency] ?? ''}>{urgency}</Badge>;
}
