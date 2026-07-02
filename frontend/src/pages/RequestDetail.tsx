import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ArrowLeft,
  CheckCircle,
  XCircle,
  Send,
  RotateCcw,
  FileText,
  MessageSquare,
  History,
  Package,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Textarea } from '@/components/ui/textarea';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  TableFooter,
} from '@/components/ui/table';
import { StatusBadge, UrgencyBadge } from '@/components/StatusBadge';
import { useAuth } from '@/hooks/useAuth';
import { procurementApi } from '@/api/procurement';

export function RequestDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { currentUser } = useAuth();
  const queryClient = useQueryClient();
  const [rejectReason, setRejectReason] = useState('');
  const [approvalComment, setApprovalComment] = useState('');
  const [commentText, setCommentText] = useState('');
  const [rejectDialog, setRejectDialog] = useState<'manager' | 'finance' | null>(null);
  const [approveDialog, setApproveDialog] = useState<'manager' | 'finance' | null>(null);

  const { data: request, isLoading } = useQuery({
    queryKey: ['procurement', id],
    queryFn: () => procurementApi.getById(id!),
    enabled: !!id,
  });

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['procurement'] });
    queryClient.invalidateQueries({ queryKey: ['metrics'] });
    queryClient.invalidateQueries({ queryKey: ['notifications'] });
  };

  const submitMutation = useMutation({
    mutationFn: () => procurementApi.submit(id!),
    onSuccess: invalidate,
  });

  const approveManagerMutation = useMutation({
    mutationFn: (comment?: string) => procurementApi.approveByManager(id!, comment),
    onSuccess: () => { invalidate(); setApproveDialog(null); },
  });

  const rejectManagerMutation = useMutation({
    mutationFn: (reason: string) => procurementApi.rejectByManager(id!, reason),
    onSuccess: () => { invalidate(); setRejectDialog(null); setRejectReason(''); },
  });

  const approveFinanceMutation = useMutation({
    mutationFn: (comment?: string) => procurementApi.approveByFinance(id!, comment),
    onSuccess: () => { invalidate(); setApproveDialog(null); },
  });

  const rejectFinanceMutation = useMutation({
    mutationFn: (reason: string) => procurementApi.rejectByFinance(id!, reason),
    onSuccess: () => { invalidate(); setRejectDialog(null); setRejectReason(''); },
  });

  const issuePoMutation = useMutation({
    mutationFn: () => procurementApi.issuePo(id!),
    onSuccess: invalidate,
  });

  const reviseMutation = useMutation({
    mutationFn: () => procurementApi.reviseToDraft(id!),
    onSuccess: invalidate,
  });

  const commentMutation = useMutation({
    mutationFn: (text: string) => procurementApi.addComment(id!, text),
    onSuccess: () => { invalidate(); setCommentText(''); },
  });

  if (isLoading || !request) {
    return <div className="text-center py-12 text-muted-foreground">Loading...</div>;
  }

  const isOwner = currentUser?.id === request.requester.id;
  const isManager = currentUser?.role === 'Manager' || currentUser?.role === 'Admin';
  const isFinance = currentUser?.role === 'Finance' || currentUser?.role === 'Admin';

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" onClick={() => navigate(-1)}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold">{request.title}</h1>
            <StatusBadge status={request.status} />
            <UrgencyBadge urgency={request.urgency} />
          </div>
          <p className="text-muted-foreground mt-1">
            {request.department} · Created {new Date(request.createdAt).toLocaleDateString()} · By {request.requester.name}
          </p>
        </div>
      </div>

      {/* Action buttons */}
      <div className="flex flex-wrap gap-2">
        {request.status === 'Draft' && isOwner && (
          <Button onClick={() => submitMutation.mutate()} disabled={submitMutation.isPending} className="gap-2">
            <Send className="h-4 w-4" />
            Submit for Approval
          </Button>
        )}
        {request.status === 'Submitted' && isManager && (
          <>
            <Button onClick={() => setApproveDialog('manager')} className="gap-2 bg-green-600 hover:bg-green-700">
              <CheckCircle className="h-4 w-4" />
              Approve
            </Button>
            <Button variant="destructive" onClick={() => setRejectDialog('manager')} className="gap-2">
              <XCircle className="h-4 w-4" />
              Reject
            </Button>
          </>
        )}
        {request.status === 'ManagerApproved' && isFinance && (
          <>
            <Button onClick={() => setApproveDialog('finance')} className="gap-2 bg-green-600 hover:bg-green-700">
              <CheckCircle className="h-4 w-4" />
              Finance Approve
            </Button>
            <Button variant="destructive" onClick={() => setRejectDialog('finance')} className="gap-2">
              <XCircle className="h-4 w-4" />
              Reject
            </Button>
          </>
        )}
        {request.status === 'FinanceApproved' && isFinance && (
          <Button onClick={() => issuePoMutation.mutate()} disabled={issuePoMutation.isPending} className="gap-2">
            <Package className="h-4 w-4" />
            Issue Purchase Order
          </Button>
        )}
        {(request.status === 'ManagerRejected' || request.status === 'FinanceRejected') && isOwner && (
          <Button onClick={() => reviseMutation.mutate()} disabled={reviseMutation.isPending} variant="outline" className="gap-2">
            <RotateCcw className="h-4 w-4" />
            Revise & Resubmit
          </Button>
        )}
      </div>

      {request.poNumber && (
        <Card className="bg-green-50 border-green-200">
          <CardContent className="py-4 flex items-center gap-3">
            <Package className="h-5 w-5 text-green-600" />
            <div>
              <p className="font-medium text-green-800">Purchase Order Issued</p>
              <p className="text-sm text-green-700">PO Number: {request.poNumber}</p>
            </div>
          </CardContent>
        </Card>
      )}

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2 space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <FileText className="h-4 w-4" />
                Description
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm whitespace-pre-wrap">{request.description}</p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Line Items</CardTitle>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Item</TableHead>
                    <TableHead className="text-right">Qty</TableHead>
                    <TableHead className="text-right">Unit Price</TableHead>
                    <TableHead className="text-right">Total</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {request.lineItems.map(item => (
                    <TableRow key={item.id}>
                      <TableCell>{item.name}</TableCell>
                      <TableCell className="text-right">{item.quantity}</TableCell>
                      <TableCell className="text-right">${item.unitPrice.toFixed(2)}</TableCell>
                      <TableCell className="text-right font-medium">${item.totalPrice.toFixed(2)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
                <TableFooter>
                  <TableRow>
                    <TableCell colSpan={3} className="font-medium">Total</TableCell>
                    <TableCell className="text-right font-bold">
                      ${request.totalAmount.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                    </TableCell>
                  </TableRow>
                </TableFooter>
              </Table>
            </CardContent>
          </Card>

          <Tabs defaultValue="audit">
            <TabsList>
              <TabsTrigger value="audit" className="gap-1">
                <History className="h-3 w-3" />
                Audit Trail ({request.auditTrail.length})
              </TabsTrigger>
              <TabsTrigger value="comments" className="gap-1">
                <MessageSquare className="h-3 w-3" />
                Comments ({request.comments.length})
              </TabsTrigger>
            </TabsList>
            <TabsContent value="audit" className="mt-4">
              <Card>
                <CardContent className="pt-4">
                  {request.auditTrail.length === 0 ? (
                    <p className="text-muted-foreground text-sm text-center py-4">No audit entries yet</p>
                  ) : (
                    <div className="space-y-4">
                      {request.auditTrail.map(entry => (
                        <div key={entry.id} className="flex gap-3">
                          <div className="w-2 h-2 rounded-full bg-primary mt-2 shrink-0" />
                          <div>
                            <p className="text-sm font-medium">{entry.action}</p>
                            <p className="text-xs text-muted-foreground">
                              {entry.fromStatus} → {entry.toStatus} · {new Date(entry.createdAt).toLocaleString()}
                            </p>
                            {entry.comment && (
                              <p className="text-sm mt-1 text-muted-foreground italic">"{entry.comment}"</p>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </CardContent>
              </Card>
            </TabsContent>
            <TabsContent value="comments" className="mt-4">
              <Card>
                <CardContent className="pt-4 space-y-4">
                  {request.comments.map(comment => (
                    <div key={comment.id} className="border-b pb-3 last:border-0">
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-medium">{comment.userName || 'User'}</span>
                        <span className="text-xs text-muted-foreground">
                          {new Date(comment.createdAt).toLocaleString()}
                        </span>
                      </div>
                      <p className="text-sm mt-1">{comment.text}</p>
                    </div>
                  ))}
                  <Separator />
                  <div className="flex gap-2">
                    <Textarea
                      placeholder="Add a comment..."
                      value={commentText}
                      onChange={e => setCommentText(e.target.value)}
                      rows={2}
                    />
                    <Button
                      onClick={() => commentText.trim() && commentMutation.mutate(commentText.trim())}
                      disabled={!commentText.trim() || commentMutation.isPending}
                      size="sm"
                    >
                      Post
                    </Button>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>
          </Tabs>
        </div>

        <div className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Requester</span>
                <span className="font-medium">{request.requester.name}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Department</span>
                <span>{request.department}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Urgency</span>
                <UrgencyBadge urgency={request.urgency} />
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Status</span>
                <StatusBadge status={request.status} />
              </div>
              <Separator />
              <div className="flex justify-between">
                <span className="text-muted-foreground">Total Amount</span>
                <span className="font-bold">
                  ${request.totalAmount.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Items</span>
                <span>{request.lineItems.length}</span>
              </div>
              <Separator />
              <div className="flex justify-between">
                <span className="text-muted-foreground">Created</span>
                <span>{new Date(request.createdAt).toLocaleDateString()}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Updated</span>
                <span>{new Date(request.updatedAt).toLocaleDateString()}</span>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Approve dialog */}
      <Dialog open={!!approveDialog} onOpenChange={() => setApproveDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Approve Request</DialogTitle>
          </DialogHeader>
          <Textarea
            placeholder="Optional comment..."
            value={approvalComment}
            onChange={e => setApprovalComment(e.target.value)}
            rows={3}
          />
          <DialogFooter>
            <Button variant="outline" onClick={() => setApproveDialog(null)}>Cancel</Button>
            <Button
              className="bg-green-600 hover:bg-green-700"
              onClick={() => {
                if (approveDialog === 'manager') {
                  approveManagerMutation.mutate(approvalComment || undefined);
                } else {
                  approveFinanceMutation.mutate(approvalComment || undefined);
                }
              }}
              disabled={approveManagerMutation.isPending || approveFinanceMutation.isPending}
            >
              Approve
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Reject dialog */}
      <Dialog open={!!rejectDialog} onOpenChange={() => setRejectDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reject Request</DialogTitle>
          </DialogHeader>
          <Textarea
            placeholder="Reason for rejection (required)..."
            value={rejectReason}
            onChange={e => setRejectReason(e.target.value)}
            rows={3}
          />
          <DialogFooter>
            <Button variant="outline" onClick={() => setRejectDialog(null)}>Cancel</Button>
            <Button
              variant="destructive"
              onClick={() => {
                if (rejectDialog === 'manager') {
                  rejectManagerMutation.mutate(rejectReason);
                } else {
                  rejectFinanceMutation.mutate(rejectReason);
                }
              }}
              disabled={!rejectReason.trim() || rejectManagerMutation.isPending || rejectFinanceMutation.isPending}
            >
              Reject
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
