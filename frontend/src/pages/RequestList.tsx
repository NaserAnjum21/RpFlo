import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Plus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { StatusBadge, UrgencyBadge } from '@/components/StatusBadge';
import { RequestListSkeleton } from '@/components/Skeleton';
import { useAuth } from '@/hooks/useAuth';
import { procurementApi } from '@/api/procurement';
import { formatDate } from '@/lib/utils';
import type { ProcurementListItem } from '@/types';

export function RequestList() {
  const { currentUser } = useAuth();
  const [tab, setTab] = useState('all');

  const { data: allRequests = [], isLoading } = useQuery({
    queryKey: ['procurement', 'all'],
    queryFn: procurementApi.getAll,
  });

  const isRequester = currentUser?.role === 'Requester';

  const filteredRequests = (() => {
    switch (tab) {
      case 'my':
        return allRequests.filter(r => r.requesterName === currentUser?.name);
      case 'draft':
        return allRequests.filter(r => r.status === 'Draft');
      case 'pending':
        return allRequests.filter(r => r.status === 'Submitted' || r.status === 'ManagerApproved');
      case 'completed':
        return allRequests.filter(r => r.status === 'PurchaseOrderIssued');
      case 'rejected':
        return allRequests.filter(r => r.status === 'ManagerRejected' || r.status === 'FinanceRejected');
      default:
        return allRequests;
    }
  })();

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Procurement Requests</h1>
          <p className="text-muted-foreground">{allRequests.length} total requests</p>
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

      <Tabs value={tab} onValueChange={setTab}>
        <TabsList>
          <TabsTrigger value="all">All</TabsTrigger>
          <TabsTrigger value="my">My Requests</TabsTrigger>
          <TabsTrigger value="draft">Drafts</TabsTrigger>
          <TabsTrigger value="pending">Pending</TabsTrigger>
          <TabsTrigger value="completed">Completed</TabsTrigger>
          <TabsTrigger value="rejected">Rejected</TabsTrigger>
        </TabsList>

        <TabsContent value={tab} className="mt-4">
          <RequestTable requests={filteredRequests} isLoading={isLoading} />
        </TabsContent>
      </Tabs>
    </div>
  );
}

function RequestTable({ requests, isLoading }: { requests: ProcurementListItem[]; isLoading: boolean }) {
  if (isLoading) {
    return <RequestListSkeleton />;
  }

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
            <TableHead>Requester</TableHead>
            <TableHead>Created</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {requests.map(request => (
            <TableRow key={request.id}>
              <TableCell>
                <Link
                  to={`/requests/${request.id}`}
                  className="font-medium text-primary hover:underline"
                >
                  {request.title}
                </Link>
              </TableCell>
              <TableCell>{request.department}</TableCell>
              <TableCell><UrgencyBadge urgency={request.urgency} /></TableCell>
              <TableCell><StatusBadge status={request.status} /></TableCell>
              <TableCell className="text-right font-medium">
                ${request.totalAmount.toLocaleString('en-US', { minimumFractionDigits: 2 })}
              </TableCell>
              <TableCell>{request.requesterName}</TableCell>
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
