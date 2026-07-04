import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ArrowLeft,
  FileText,
  MessageSquare,
  History,
  Package,
  Plus,
  Trash2,
  AlertCircle,
  FileDown,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  TableFooter,
} from '@/components/ui/table';
import { StatusBadge, UrgencyBadge } from '@/components/StatusBadge';
import { ActionBar } from '@/components/ActionBar';
import { ApproveDialog, RejectDialog } from '@/components/ApprovalDialogs';
import { DetailSkeleton } from '@/components/Skeleton';
import { useAuth } from '@/hooks/useAuth';
import { procurementApi } from '@/api/procurement';
import { formatDate, formatDateTime } from '@/lib/utils';
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

function getErrorMessage(error: unknown): string {
  const fallback = 'Something went wrong. Please try again.';
  if (!error || typeof error !== 'object' || !('response' in error)) return fallback;

  const response = (error as { response?: { data?: unknown } }).response;
  const data = response?.data;
  if (!data || typeof data !== 'object') return fallback;

  if ('Message' in data && typeof data.Message === 'string') return data.Message;
  if ('message' in data && typeof data.message === 'string') return data.message;

  if ('errors' in data && data.errors && typeof data.errors === 'object') {
    const errors = data.errors as Record<string, string[]>;
    const firstError = Object.values(errors).flat()[0];
    if (firstError) return firstError;
  }

  return fallback;
}

function getResponseStatus(error: unknown): number | undefined {
  if (!error || typeof error !== 'object' || !('response' in error)) return undefined;
  return (error as { response?: { status?: number } }).response?.status;
}

export function RequestDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { currentUser } = useAuth();
  const queryClient = useQueryClient();
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
  const [actionError, setActionError] = useState('');
  const [needsReviseToDraft, setNeedsReviseToDraft] = useState(false);

  const { data: request, error, isError, isLoading } = useQuery({
    queryKey: ['procurement', id],
    queryFn: () => procurementApi.getById(id!),
    enabled: !!id,
    retry: (failureCount, queryError) => {
      const status = getResponseStatus(queryError);
      return status !== 403 && status !== 404 && failureCount < 1;
    },
  });

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['procurement'] });
    queryClient.invalidateQueries({ queryKey: ['metrics'] });
    queryClient.invalidateQueries({ queryKey: ['notifications'] });
  };

  const handleMutationError = (error: unknown) => {
    setActionError(getErrorMessage(error));
  };

  const submitMutation = useMutation({
    mutationFn: () => procurementApi.submit(id!),
    onSuccess: () => { setActionError(''); invalidate(); },
    onError: handleMutationError,
  });

  const approveManagerMutation = useMutation({
    mutationFn: (comment?: string) => procurementApi.approveByManager(id!, comment),
    onSuccess: () => { setActionError(''); invalidate(); setApproveDialog(null); },
    onError: handleMutationError,
  });

  const rejectManagerMutation = useMutation({
    mutationFn: (reason: string) => procurementApi.rejectByManager(id!, reason),
    onSuccess: () => { setActionError(''); invalidate(); setRejectDialog(null); },
    onError: handleMutationError,
  });

  const approveFinanceMutation = useMutation({
    mutationFn: (comment?: string) => procurementApi.approveByFinance(id!, comment),
    onSuccess: () => { setActionError(''); invalidate(); setApproveDialog(null); },
    onError: handleMutationError,
  });

  const rejectFinanceMutation = useMutation({
    mutationFn: (reason: string) => procurementApi.rejectByFinance(id!, reason),
    onSuccess: () => { setActionError(''); invalidate(); setRejectDialog(null); },
    onError: handleMutationError,
  });

  const issuePoMutation = useMutation({
    mutationFn: () => procurementApi.issuePo(id!),
    onSuccess: () => { setActionError(''); invalidate(); },
    onError: handleMutationError,
  });

  const reviseMutation = useMutation({
    mutationFn: () => procurementApi.reviseToDraft(id!),
    onSuccess: () => { setActionError(''); invalidate(); },
    onError: handleMutationError,
  });

  const commentMutation = useMutation({
    mutationFn: (text: string) => procurementApi.addComment(id!, text),
    onSuccess: () => { setActionError(''); invalidate(); setCommentText(''); },
    onError: handleMutationError,
  });

  const handleExportPdf = () => {
    // Use the API client so the simulated auth header is sent with the download request.
    procurementApi.exportPdf(id!);
  };

  if (isLoading) {
    return <DetailSkeleton />;
  }

  if (isError || !request) {
    const status = getResponseStatus(error);
    const isForbidden = status === 403;

    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" onClick={() => navigate('/requests')}>
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div>
            <h1 className="text-2xl font-bold">
              {isForbidden ? 'Access denied' : 'Request unavailable'}
            </h1>
            <p className="text-muted-foreground mt-1">
              {isForbidden
                ? 'You do not have permission to view this procurement request.'
                : getErrorMessage(error)}
            </p>
          </div>
        </div>

        <Card className="border-red-200 bg-red-50">
          <CardContent className="py-4 flex items-start gap-3 text-red-800">
            <AlertCircle className="h-5 w-5 mt-0.5 shrink-0" />
            <div className="space-y-3">
              <p className="text-sm">
                {isForbidden
                  ? 'This request may belong to another requester or sit outside your workflow scope.'
                  : 'The request could not be loaded.'}
              </p>
              <Button variant="outline" size="sm" onClick={() => navigate('/requests')}>
                Back to requests
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  const isOwner = currentUser?.id === request.requester.id;
  const isManager = currentUser?.role === 'Manager' || currentUser?.role === 'Admin';
  const isFinance = currentUser?.role === 'Finance' || currentUser?.role === 'Admin';
  const isDraft = request.status === 'Draft';
  const isRejected = request.status === 'ManagerRejected' || request.status === 'FinanceRejected';
  const canEdit = (isDraft || isRejected) && isOwner;

  const startEditing = () => {
    setNeedsReviseToDraft(isRejected);
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
    setNeedsReviseToDraft(false);
  };

  const saveEdits = async () => {
    if (!editTitle.trim() || !editDescription.trim() || !editDepartment || !editUrgency) {
      setActionError('Title, description, department, and urgency are required.');
      return;
    }

    const validLineItems = editLineItems.filter(li =>
      li.name.trim() && parseInt(li.quantity) > 0 && parseFloat(li.unitPrice) > 0
    );
    if (validLineItems.length === 0) {
      setActionError('At least one valid line item is required.');
      return;
    }

    setIsSaving(true);
    try {
      if (needsReviseToDraft) {
        await procurementApi.reviseToDraft(id!);
        setNeedsReviseToDraft(false);
      }

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
      setActionError('');
      setIsEditing(false);
    } catch (error) {
      handleMutationError(error);
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
                className="text-2xl font-bold h-10 flex-1 max-w-md"
              />
            ) : (
              <h1 className="text-2xl font-bold">{request.title}</h1>
            )}
            <StatusBadge status={request.status} />
            {!isEditing && <UrgencyBadge urgency={request.urgency} />}
          </div>
          <p className="text-muted-foreground mt-1">
            {request.department} · Created {formatDate(request.createdAt)} · By {request.requester.name}
          </p>
        </div>
      </div>

      <ActionBar
        canEdit={canEdit}
        isEditing={isEditing}
        isSaving={isSaving}
        isDraft={isDraft}
        isRejected={isRejected}
        isOwner={isOwner}
        isManager={isManager}
        isFinance={isFinance}
        status={request.status}
        onEdit={startEditing}
        onSave={saveEdits}
        onCancelEdit={cancelEditing}
        onSubmit={() => submitMutation.mutate()}
        isSubmitting={submitMutation.isPending}
        onApproveManager={() => setApproveDialog('manager')}
        onRejectManager={() => setRejectDialog('manager')}
        onApproveFinance={() => setApproveDialog('finance')}
        onRejectFinance={() => setRejectDialog('finance')}
        onIssuePo={() => issuePoMutation.mutate()}
        isIssuingPo={issuePoMutation.isPending}
        onRevise={() => reviseMutation.mutate()}
        isRevising={reviseMutation.isPending}
      />

      {actionError && (
        <Card className="border-red-200 bg-red-50">
          <CardContent className="py-3 flex items-start gap-2 text-sm text-red-800">
            <AlertCircle className="h-4 w-4 mt-0.5 shrink-0" />
            <span>{actionError}</span>
          </CardContent>
        </Card>
      )}

      {request.poNumber && (
        <Card className="bg-green-50 border-green-200">
          <CardContent className="py-4 flex items-center gap-3">
            <Package className="h-5 w-5 text-green-600" />
            <div className="flex-1">
              <p className="font-medium text-green-800">Purchase Order Issued</p>
              <p className="text-sm text-green-700">PO Number: {request.poNumber}</p>
            </div>
            <Button
              variant="outline"
              size="sm"
              className="gap-2 border-green-300 text-green-700 hover:bg-green-100"
              onClick={handleExportPdf}
            >
              <FileDown className="h-4 w-4" />
              Export PDF
            </Button>
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
                  <div className="flex gap-3 items-center text-xs text-muted-foreground">
                    <div className="flex-1">Item Name</div>
                    <div className="w-24">Qty</div>
                    <div className="w-32">Unit Price</div>
                    <div className="w-24 text-right">Total</div>
                    <div className="w-9" />
                  </div>
                  {editLineItems.map((item, index) => (
                    <div key={item.originalId ?? `new-${index}`} className="flex gap-3 items-center">
                      <div className="flex-1">
                        <Input
                          placeholder="Item name"
                          value={item.name}
                          onChange={e => updateEditLineItem(index, 'name', e.target.value)}
                        />
                      </div>
                      <div className="w-24">
                        <Input
                          type="number"
                          placeholder="Qty"
                          value={item.quantity}
                          onChange={e => updateEditLineItem(index, 'quantity', e.target.value)}
                          min="1"
                        />
                      </div>
                      <div className="w-32">
                        <Input
                          type="number"
                          placeholder="Price"
                          value={item.unitPrice}
                          onChange={e => updateEditLineItem(index, 'unitPrice', e.target.value)}
                          min="0.01"
                          step="0.01"
                        />
                      </div>
                      <div className="w-24 text-right text-sm font-medium">
                        ${((parseInt(item.quantity) || 0) * (parseFloat(item.unitPrice) || 0)).toFixed(2)}
                      </div>
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => removeEditLineItem(index)}
                        disabled={editLineItems.length <= 1}
                        className="shrink-0"
                      >
                        <Trash2 className="h-4 w-4 text-muted-foreground" />
                      </Button>
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
                      {[...request.auditTrail].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()).map(entry => (
                        <div key={entry.id} className="flex gap-3">
                          <div className="w-2 h-2 rounded-full bg-primary mt-2 shrink-0" />
                          <div>
                            <p className="text-sm font-medium">{entry.action}</p>
                            <p className="text-xs text-muted-foreground">
                              {entry.fromStatus} → {entry.toStatus} · {formatDateTime(entry.createdAt)}
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
                        <span className="text-sm font-medium">{comment.userName}</span>
                        <span className="text-xs text-muted-foreground">
                          {formatDateTime(comment.createdAt)}
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
                <span>{formatDate(request.createdAt)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Updated</span>
                <span>{formatDate(request.updatedAt)}</span>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      <ApproveDialog
        open={!!approveDialog}
        onClose={() => setApproveDialog(null)}
        onConfirm={(comment) => {
          if (approveDialog === 'manager') approveManagerMutation.mutate(comment);
          else approveFinanceMutation.mutate(comment);
        }}
        isPending={approveManagerMutation.isPending || approveFinanceMutation.isPending}
      />

      <RejectDialog
        open={!!rejectDialog}
        onClose={() => setRejectDialog(null)}
        onConfirm={(reason) => {
          if (rejectDialog === 'manager') rejectManagerMutation.mutate(reason);
          else rejectFinanceMutation.mutate(reason);
        }}
        isPending={rejectManagerMutation.isPending || rejectFinanceMutation.isPending}
      />
    </div>
  );
}
