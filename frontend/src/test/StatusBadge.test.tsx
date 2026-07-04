import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { StatusBadge, UrgencyBadge } from '@/components/StatusBadge';
import type { ProcurementStatus } from '@/types';

describe('StatusBadge', () => {
  it.each<[ProcurementStatus, string]>([
    ['Draft', 'Draft'],
    ['ManagerApproved', 'Manager Approved'],
    ['FinanceRejected', 'Finance Rejected'],
    ['PurchaseOrderIssued', 'PO Issued'],
  ])('renders %s as %s', (status, label) => {
    render(<StatusBadge status={status} />);

    expect(screen.getByText(label)).toBeInTheDocument();
  });
});

describe('UrgencyBadge', () => {
  it.each(['Low', 'Medium', 'High', 'Critical', 'Unknown'])('renders %s urgency text', urgency => {
    render(<UrgencyBadge urgency={urgency} />);

    expect(screen.getByText(urgency)).toBeInTheDocument();
  });
});
