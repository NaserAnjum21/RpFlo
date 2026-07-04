import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Plus, ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import { StatusBadge, UrgencyBadge } from '@/components/StatusBadge';
import { RequestListSkeleton } from '@/components/Skeleton';
import { useAuth } from '@/hooks/useAuth';
import { procurementApi } from '@/api/procurement';
import { formatDate } from '@/lib/utils';
import type { ProcurementListItem } from '@/types';

const PAGE_SIZE = 10;

export function RequestList() {
  const { currentUser } = useAuth();
  const isRequester = currentUser?.role === 'Requester';
  const [tab, setTab] = useState('all');
  const [page, setPage] = useState(1);
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');

  const { data: rawRequests = [], isLoading } = useQuery({
    queryKey: ['procurement', isRequester ? 'my' : 'all'],
    queryFn: isRequester ? procurementApi.getMy : procurementApi.getAll,
  });

  const resetPage = () => setPage(1);

  const dateFiltered = rawRequests.filter(r => {
    if (dateFrom && new Date(r.createdAt) < new Date(dateFrom)) return false;
    if (dateTo) {
      const to = new Date(dateTo);
      to.setDate(to.getDate() + 1);
      if (new Date(r.createdAt) >= to) return false;
    }
    return true;
  });

  const tabFiltered = (() => {
    switch (tab) {
      case 'draft':
        return dateFiltered.filter(r => r.status === 'Draft');
      case 'pending':
        return dateFiltered.filter(r => r.status === 'Submitted' || r.status === 'ManagerApproved');
      case 'completed':
        return dateFiltered.filter(r => r.status === 'PurchaseOrderIssued');
      case 'rejected':
        return dateFiltered.filter(r => r.status === 'ManagerRejected' || r.status === 'FinanceRejected');
      default:
        return dateFiltered;
    }
  })();

  const totalPages = Math.max(1, Math.ceil(tabFiltered.length / PAGE_SIZE));
  const paginated = tabFiltered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">
            {isRequester ? 'My Requests' : 'Procurement Requests'}
          </h1>
          <p className="text-muted-foreground">{rawRequests.length} total requests</p>
        </div>
        {(isRequester || currentUser?.role === 'Admin') && (
          <Link to="/requests/new">
            <Button className="gap-2">
              <Plus className="h-4 w-4" />
              New Request
            </Button>
          </Link>
        )}
      </div>

      <div className="flex items-center gap-3">
        <div className="flex items-center gap-2">
          <span className="text-sm text-muted-foreground whitespace-nowrap">From</span>
          <Input
            type="date"
            value={dateFrom}
            onChange={e => { setDateFrom(e.target.value); resetPage(); }}
            className="w-40 h-8"
          />
        </div>
        <div className="flex items-center gap-2">
          <span className="text-sm text-muted-foreground whitespace-nowrap">To</span>
          <Input
            type="date"
            value={dateTo}
            onChange={e => { setDateTo(e.target.value); resetPage(); }}
            className="w-40 h-8"
          />
        </div>
        {(dateFrom || dateTo) && (
          <Button variant="ghost" size="sm" onClick={() => { setDateFrom(''); setDateTo(''); resetPage(); }}>
            Clear
          </Button>
        )}
      </div>

      <Tabs value={tab} onValueChange={v => { setTab(v); resetPage(); }}>
        <TabsList>
          <TabsTrigger value="all">All</TabsTrigger>
          <TabsTrigger value="draft">Drafts</TabsTrigger>
          <TabsTrigger value="pending">Pending</TabsTrigger>
          <TabsTrigger value="completed">Completed</TabsTrigger>
          <TabsTrigger value="rejected">Rejected</TabsTrigger>
        </TabsList>

        <TabsContent value={tab} className="mt-4">
          <RequestTable requests={paginated} isLoading={isLoading} showRequester={!isRequester} />
          {tabFiltered.length > PAGE_SIZE && (
            <Pagination page={page} totalPages={totalPages} onPageChange={setPage} total={tabFiltered.length} />
          )}
        </TabsContent>
      </Tabs>
    </div>
  );
}

function RequestTable({ requests, isLoading, showRequester }: {
  requests: ProcurementListItem[];
  isLoading: boolean;
  showRequester: boolean;
}) {
  if (isLoading) return <RequestListSkeleton />;

  if (requests.length === 0) {
    return (
      <Card className="p-8 text-center text-muted-foreground">
        No requests found.
      </Card>
    );
  }

  return (
    <Card>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Title</TableHead>
            <TableHead>Department</TableHead>
            <TableHead>Urgency</TableHead>
            <TableHead>Status</TableHead>
            <TableHead className="text-right">Amount</TableHead>
            {showRequester && <TableHead>Requester</TableHead>}
            <TableHead>Created</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {requests.map(request => (
            <TableRow key={request.id}>
              <TableCell>
                <Link to={`/requests/${request.id}`} className="font-medium text-primary hover:underline">
                  {request.title}
                </Link>
              </TableCell>
              <TableCell>{request.department}</TableCell>
              <TableCell><UrgencyBadge urgency={request.urgency} /></TableCell>
              <TableCell><StatusBadge status={request.status} /></TableCell>
              <TableCell className="text-right font-medium">
                ${request.totalAmount.toLocaleString('en-US', { minimumFractionDigits: 2 })}
              </TableCell>
              {showRequester && <TableCell>{request.requesterName}</TableCell>}
              <TableCell className="text-muted-foreground">
                {formatDate(request.createdAt)}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Card>
  );
}

function Pagination({ page, totalPages, onPageChange, total }: {
  page: number;
  totalPages: number;
  onPageChange: (p: number) => void;
  total: number;
}) {
  const from = (page - 1) * PAGE_SIZE + 1;
  const to = Math.min(page * PAGE_SIZE, total);

  return (
    <div className="flex items-center justify-between mt-4">
      <p className="text-sm text-muted-foreground">
        {from}–{to} of {total}
      </p>
      <div className="flex items-center gap-1">
        <Button variant="outline" size="icon" className="h-8 w-8" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <span className="text-sm px-2">
          {page} / {totalPages}
        </span>
        <Button variant="outline" size="icon" className="h-8 w-8" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}
