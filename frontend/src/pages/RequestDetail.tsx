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
  Pencil,
  Plus,
  Trash2,
  Save,
  X,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
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
import type { LineItemResponse } from '@/types';

const departments = ['Engineering', 'Marketing', 'Sales', 'Operations', 'HumanResources', 'Finance'];
const urgencies = ['Low', 'Medium', 'High', 'Critical'];

interface EditLineItem {
  originalId: string | null;
  name: string;
  quantity: string;
  unitPrice: string;
}

function toEditLineItems(items: LineItemResponse[]): EditLineItem[] {
  return items.map(item => ({
    originalId: item.id,
    name: item.name,
    quantity: String(item.quantity),
    unitPrice: String(item.unitPrice),
  }));
}

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
  const [isEditing, setIsEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [editTitle, setEditTitle] = useState('');
  const [editDescription, setEditDescription] = useState('');
  const [editDepartment, setEditDepartment] = useState('');
  const [editUrgency, setEditUrgency] = useState('');
  const [editLineItems, setEditLineItems] = useState<EditLineItem[]>([]);

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
  const isDraft = request.status === 'Draft';
  const isRejected = request.status === 'ManagerRejected' || request.status === 'FinanceRejected';
  const canEdit = (isDraft || isRejected) && isOwner;

  const startEditing = async () => {
    if (isRejected) {
      await procurementApi.reviseToDraft(id!);
      invalidate();
    }
    setEditTitle(request.title);
    setEditDescription(request.description);
    setEditDepartment(request.department);
    setEditUrgency(request.urgency);
    setEditLineItems(toEditLineItems(request.lineItems));
    setIsEditing(true);
  };

  const cancelEditing = () => {
    setIsEditing(false);
    setIsSaving(false);
  };

  const saveEdits = async () => {
    if (!editTitle.trim() || !editDescription.trim() || !editDepartment || !editUrgency) return;

    const validLineItems = editLineItems.filter(li =>
      li.name.trim() && parseInt(li.quantity) > 0 && parseFloat(li.unitPrice) > 0
    );
    if (validLineItems.length === 0) return;

    setIsSaving(true);
    try {
      await procurementApi.update(id!, {
        title: editTitle.trim(),
        description: editDescription.trim(),
        department: editDepartment,
        urgency: editUrgency,
      });

      const unchanged = validLineItems.filter(li => {
        if (!li.originalId) return false;
        const orig = request.lineItems.find(o => o.id === li.originalId);
        if (!orig) return false;
        return orig.name === li.name.trim()
          && orig.quantity === parseInt(li.quantity)
          && orig.unitPrice === parseFloat(li.unitPrice);
      });
      const unchangedIds = new Set(unchanged.map(li => li.originalId));

      for (const li of request.lineItems) {
        if (!unchangedIds.has(li.id)) {
          await procurementApi.removeLineItem(id!, li.id);
        }
      }

      const toAdd = validLineItems.filter(li => !unchangedIds.has(li.originalId));
      if (toAdd.length > 0) {
        await procurementApi.addLineItems(id!, toAdd.map(li => ({
          name: li.name.trim(),
          quantity: parseInt(li.quantity),
          unitPrice: parseFloat(li.unitPrice),
        })));
      }

      invalidate();
      setIsEditing(false);
    } finally {
      setIsSaving(false);
    }
  };

  const updateEditLineItem = (index: number, field: keyof Omit<EditLineItem, 'originalId'>, value: string) => {
    setEditLineItems(prev => prev.map((item, i) => (i === index ? { ...item, [field]: value } : item)));
  };

  const removeEditLineItem = (index: number) => {
    setEditLineItems(prev => prev.filter((_, i) => i !== index));
  };

  const addEditLineItem = () => {
    setEditLineItems(prev => [...prev, { originalId: null, name: '', quantity: '', unitPrice: '' }]);
  };

  const editTotal = editLineItems.reduce((sum, item) => {
    const qty = parseInt(item.quantity) || 0;
    const price = parseFloat(item.unitPrice) || 0;
    return sum + qty * price;
  }, 0);

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" onClick={() => navigate(-1)}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <div className="flex-1">
          <div className="flex items-center gap-3">
            {isEditing ? (
              <Input
                value={editTitle}
                onChange={e => setEditTitle(e.target.value)}
                className="text-2xl font-bold h-auto py-1"
              />
            ) : (
              <h1 className="text-2xl font-bold">{request.title}</h1>
            )}
            <StatusBadge status={request.status} />
            {!isEditing && <UrgencyBadge urgency={request.urgency} />}
          </div>
          <p className="text-muted-foreground mt-1">
            {request.department} · Created {new Date(request.createdAt).toLocaleDateString()} · By {request.requester.name}
          </p>
        </div>
      </div>

      {/* Action buttons */}
      <div className="flex flex-wrap gap-2">
        {canEdit && !isEditing && (
          <Button onClick={startEditing} variant="outline" className="gap-2">
            <Pencil className="h-4 w-4" />
            Edit
          </Button>
        )}
        {isEditing && (
          <>
            <Button onClick={saveEdits} disabled={isSaving} className="gap-2">
              <Save className="h-4 w-4" />
              {isSaving ? 'Saving...' : 'Save Changes'}
            </Button>
            <Button onClick={cancelEditing} variant="outline" disabled={isSaving} className="gap-2">
              <X className="h-4 w-4" />
              Cancel
            </Button>
          </>
        )}
        {isDraft && isOwner && !isEditing && (
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
        {isRejected && isOwner && !isEditing && (
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
              {isEditing ? (
                <Textarea
                  value={editDescription}
                  onChange={e => setEditDescription(e.target.value)}
                  rows={4}
                />
              ) : (
                <p className="text-sm whitespace-pre-wrap">{request.description}</p>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="text-base">Line Items</CardTitle>
              {isEditing && (
                <Button type="button" variant="outline" size="sm" onClick={addEditLineItem} className="gap-1">
                  <Plus className="h-3 w-3" />
                  Add Item
                </Button>
              )}
            </CardHeader>
            <CardContent>
              {isEditing ? (
                <div className="space-y-3">
                  {editLineItems.map((item, index) => (
                    <div key={item.originalId ?? `new-${index}`} className="flex gap-3 items-start">
                      <div className="flex-1">
                        {index === 0 && <Label className="text-xs text-muted-foreground">Item Name</Label>}
                        <Input
                          placeholder="Item name"
                          value={item.name}
                          onChange={e => updateEditLineItem(index, 'name', e.target.value)}
                        />
                      </div>
                      <div className="w-24">
                        {index === 0 && <Label className="text-xs text-muted-foreground">Qty</Label>}
                        <Input
                          type="number"
                          placeholder="Qty"
                          value={item.quantity}
                          onChange={e => updateEditLineItem(index, 'quantity', e.target.value)}
                          min="1"
                        />
                      </div>
                      <div className="w-32">
                        {index === 0 && <Label className="text-xs text-muted-foreground">Unit Price</Label>}
                        <Input
                          type="number"
                          placeholder="Price"
                          value={item.unitPrice}
                          onChange={e => updateEditLineItem(index, 'unitPrice', e.target.value)}
                          min="0.01"
                          step="0.01"
                        />
                      </div>
                      <div className="w-24 text-right pt-2 text-sm font-medium">
                        {index === 0 && <Label className="text-xs text-muted-foreground block">&nbsp;</Label>}
                        ${((parseInt(item.quantity) || 0) * (parseFloat(item.unitPrice) || 0)).toFixed(2)}
                      </div>
                      <div>
                        {index === 0 && <Label className="text-xs text-muted-foreground block">&nbsp;</Label>}
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          onClick={() => removeEditLineItem(index)}
                          disabled={editLineItems.length <= 1}
                        >
                          <Trash2 className="h-4 w-4 text-muted-foreground" />
                        </Button>
                      </div>
                    </div>
                  ))}
                  <div className="flex justify-end pt-2 border-t">
                    <p className="text-lg font-bold">
                      Total: ${editTotal.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                    </p>
                  </div>
                </div>
              ) : (
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
              )}
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
              <div className="flex justify-between items-center">
                <span className="text-muted-foreground">Department</span>
                {isEditing ? (
                  <Select value={editDepartment} onValueChange={v => v && setEditDepartment(v)}>
                    <SelectTrigger className="w-40 h-8">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {departments.map(d => (
                        <SelectItem key={d} value={d}>{d}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                ) : (
                  <span>{request.department}</span>
                )}
              </div>
              <div className="flex justify-between items-center">
                <span className="text-muted-foreground">Urgency</span>
                {isEditing ? (
                  <Select value={editUrgency} onValueChange={v => v && setEditUrgency(v)}>
                    <SelectTrigger className="w-40 h-8">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {urgencies.map(u => (
                        <SelectItem key={u} value={u}>{u}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                ) : (
                  <UrgencyBadge urgency={request.urgency} />
                )}
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Status</span>
                <StatusBadge status={request.status} />
              </div>
              <Separator />
              <div className="flex justify-between">
                <span className="text-muted-foreground">Total Amount</span>
                <span className="font-bold">
                  {isEditing
                    ? `$${editTotal.toLocaleString('en-US', { minimumFractionDigits: 2 })}`
                    : `$${request.totalAmount.toLocaleString('en-US', { minimumFractionDigits: 2 })}`
                  }
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Items</span>
                <span>{isEditing ? editLineItems.length : request.lineItems.length}</span>
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
