import { CheckCircle, XCircle, Send, RotateCcw, Package, Pencil, Save, X } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface ActionBarProps {
  canEdit: boolean;
  isEditing: boolean;
  isSaving: boolean;
  isDraft: boolean;
  isRejected: boolean;
  isOwner: boolean;
  isManager: boolean;
  isFinance: boolean;
  status: string;
  onEdit: () => void;
  onSave: () => void;
  onCancelEdit: () => void;
  onSubmit: () => void;
  isSubmitting: boolean;
  onApproveManager: () => void;
  onRejectManager: () => void;
  onApproveFinance: () => void;
  onRejectFinance: () => void;
  onIssuePo: () => void;
  isIssuingPo: boolean;
  onRevise: () => void;
  isRevising: boolean;
}

export function ActionBar({
  canEdit, isEditing, isSaving, isDraft, isRejected, isOwner,
  isManager, isFinance, status,
  onEdit, onSave, onCancelEdit, onSubmit, isSubmitting,
  onApproveManager, onRejectManager, onApproveFinance, onRejectFinance,
  onIssuePo, isIssuingPo, onRevise, isRevising,
}: ActionBarProps) {
  return (
    <div className="flex flex-wrap gap-2">
      {canEdit && !isEditing && (
        <Button onClick={onEdit} variant="outline" className="gap-2">
          <Pencil className="h-4 w-4" />
          Edit
        </Button>
      )}
      {isEditing && (
        <>
          <Button onClick={onSave} disabled={isSaving} className="gap-2">
            <Save className="h-4 w-4" />
            {isSaving ? 'Saving...' : 'Save Changes'}
          </Button>
          <Button onClick={onCancelEdit} variant="outline" disabled={isSaving} className="gap-2">
            <X className="h-4 w-4" />
            Cancel
          </Button>
        </>
      )}
      {isDraft && isOwner && !isEditing && (
        <Button onClick={onSubmit} disabled={isSubmitting} className="gap-2">
          <Send className="h-4 w-4" />
          Submit for Approval
        </Button>
      )}
      {status === 'Submitted' && isManager && (
        <>
          <Button onClick={onApproveManager} className="gap-2 bg-green-600 hover:bg-green-700">
            <CheckCircle className="h-4 w-4" />
            Approve
          </Button>
          <Button variant="destructive" onClick={onRejectManager} className="gap-2">
            <XCircle className="h-4 w-4" />
            Reject
          </Button>
        </>
      )}
      {status === 'ManagerApproved' && isFinance && (
        <>
          <Button onClick={onApproveFinance} className="gap-2 bg-green-600 hover:bg-green-700">
            <CheckCircle className="h-4 w-4" />
            Finance Approve
          </Button>
          <Button variant="destructive" onClick={onRejectFinance} className="gap-2">
            <XCircle className="h-4 w-4" />
            Reject
          </Button>
        </>
      )}
      {status === 'FinanceApproved' && isFinance && (
        <Button onClick={onIssuePo} disabled={isIssuingPo} className="gap-2">
          <Package className="h-4 w-4" />
          Issue Purchase Order
        </Button>
      )}
      {isRejected && isOwner && !isEditing && (
        <Button onClick={onRevise} disabled={isRevising} variant="outline" className="gap-2">
          <RotateCcw className="h-4 w-4" />
          Revise & Resubmit
        </Button>
      )}
    </div>
  );
}
