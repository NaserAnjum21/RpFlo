import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { ActionBar } from '@/components/ActionBar';

const noop = () => {};

function renderActionBar(overrides: Partial<Parameters<typeof ActionBar>[0]> = {}) {
  const props = {
    canEdit: false,
    isEditing: false,
    isSaving: false,
    isDraft: false,
    isRejected: false,
    isOwner: false,
    isManager: false,
    isFinance: false,
    status: 'Submitted',
    onEdit: noop,
    onSave: noop,
    onCancelEdit: noop,
    onSubmit: noop,
    isSubmitting: false,
    onApproveManager: noop,
    onRejectManager: noop,
    onApproveFinance: noop,
    onRejectFinance: noop,
    onIssuePo: noop,
    isIssuingPo: false,
    onRevise: noop,
    isRevising: false,
    ...overrides,
  };

  render(<ActionBar {...props} />);
}

describe('ActionBar', () => {
  it('shows edit actions, then save and cancel controls while editing', () => {
    renderActionBar({ canEdit: true });
    expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument();

    renderActionBar({ canEdit: true, isEditing: true, isSaving: true });
    expect(screen.getByRole('button', { name: /saving/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /cancel/i })).toBeDisabled();
  });

  it('lets request owners submit drafts and revise rejected requests', () => {
    const onSubmit = vi.fn();
    const onRevise = vi.fn();

    renderActionBar({ isDraft: true, isOwner: true, onSubmit });
    fireEvent.click(screen.getByRole('button', { name: /submit for approval/i }));
    expect(onSubmit).toHaveBeenCalledTimes(1);

    renderActionBar({ isRejected: true, isOwner: true, onRevise });
    fireEvent.click(screen.getByRole('button', { name: /revise & resubmit/i }));
    expect(onRevise).toHaveBeenCalledTimes(1);
  });

  it('shows only manager workflow controls for submitted requests', () => {
    const onApproveManager = vi.fn();
    const onRejectManager = vi.fn();

    renderActionBar({
      status: 'Submitted',
      isManager: true,
      onApproveManager,
      onRejectManager,
    });

    fireEvent.click(screen.getByRole('button', { name: /^approve$/i }));
    fireEvent.click(screen.getByRole('button', { name: /^reject$/i }));

    expect(onApproveManager).toHaveBeenCalledTimes(1);
    expect(onRejectManager).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole('button', { name: /finance approve/i })).not.toBeInTheDocument();
  });

  it('shows finance approval and PO actions at the matching statuses', () => {
    const onApproveFinance = vi.fn();
    const onIssuePo = vi.fn();

    renderActionBar({
      status: 'ManagerApproved',
      isFinance: true,
      onApproveFinance,
    });
    fireEvent.click(screen.getByRole('button', { name: /finance approve/i }));
    expect(onApproveFinance).toHaveBeenCalledTimes(1);

    renderActionBar({
      status: 'FinanceApproved',
      isFinance: true,
      onIssuePo,
      isIssuingPo: true,
    });
    expect(screen.getByRole('button', { name: /issue purchase order/i })).toBeDisabled();
  });
});
