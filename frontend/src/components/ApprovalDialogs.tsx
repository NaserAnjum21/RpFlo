import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';

interface ApprovalDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: (comment?: string) => void;
  isPending: boolean;
}

export function ApproveDialog({ open, onClose, onConfirm, isPending }: ApprovalDialogProps) {
  const [comment, setComment] = useState('');

  const handleConfirm = () => {
    onConfirm(comment || undefined);
    setComment('');
  };

  return (
    <Dialog open={open} onOpenChange={() => { onClose(); setComment(''); }}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Approve Request</DialogTitle>
        </DialogHeader>
        <Textarea
          placeholder="Optional comment..."
          value={comment}
          onChange={e => setComment(e.target.value)}
          rows={3}
        />
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button
            className="bg-green-600 hover:bg-green-700"
            onClick={handleConfirm}
            disabled={isPending}
          >
            Approve
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

interface RejectDialogProps {
  open: boolean;
  onClose: () => void;
  onConfirm: (reason: string) => void;
  isPending: boolean;
}

export function RejectDialog({ open, onClose, onConfirm, isPending }: RejectDialogProps) {
  const [reason, setReason] = useState('');

  const handleConfirm = () => {
    onConfirm(reason);
    setReason('');
  };

  return (
    <Dialog open={open} onOpenChange={() => { onClose(); setReason(''); }}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Reject Request</DialogTitle>
        </DialogHeader>
        <Textarea
          placeholder="Reason for rejection (required)..."
          value={reason}
          onChange={e => setReason(e.target.value)}
          rows={3}
        />
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button
            variant="destructive"
            onClick={handleConfirm}
            disabled={!reason.trim() || isPending}
          >
            Reject
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
