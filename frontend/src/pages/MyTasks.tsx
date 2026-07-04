import { useState } from 'react';
import { Link } from 'react-router-dom';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import { StatusBadge, UrgencyBadge } from '@/components/StatusBadge';
import { useAuth } from '@/hooks/useAuth';
import { procurementApi } from '@/api/procurement';
import { formatRelativeTime } from '@/lib/utils';
import type { ProcurementListItem } from '@/types';

const PAGE_SIZE = 10;

function getTaskDescription(role: string | undefined) {
  switch (role) {
    case 'Requester': return 'Drafts to complete and rejected requests to revise';
    case 'Manager': return 'Requests awaiting your manager approval';
    case 'Finance': return 'Requests awaiting finance review or PO issuance';
    case 'Admin': return 'All actionable items across workflows';
    default: return 'Items requiring your attention';
  }
}

export function MyTasks() {
  const { currentUser } = useAuth();
  const [page, setPage] = useState(1);
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');

  const queryParams = {
    page,
    pageSize: PAGE_SIZE,
    dateFrom: dateFrom || undefined,
    dateTo: dateTo || undefined,
  };

  const { data, isLoading } = useQuery({
    queryKey: ['procurement', 'pending', currentUser?.id, page, dateFrom, dateTo],
    queryFn: () => procurementApi.getPending(queryParams),
    enabled: !!currentUser,
    placeholderData: keepPreviousData,
  });

  const resetPage = () => setPage(1);
  const tasks = data?.items ?? [];
  const total = data?.totalItems ?? 0;
  const totalPages = data?.totalPages ?? 1;
  const currentPage = data?.page ?? page;

  if (isLoading) {
    return <div className="text-center py-12 text-muted-foreground">Loading...</div>;
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">My Tasks</h1>
        <p className="text-muted-foreground">
          {getTaskDescription(currentUser?.role)} · {total} item{total !== 1 ? 's' : ''}
        </p>
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

      {total === 0 ? (
        <Card className="p-8 text-center text-muted-foreground">
          Nothing requires your attention right now.
        </Card>
      ) : (
        <>
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
                  <TableHead>Waiting</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {tasks.map((task: ProcurementListItem) => (
                  <TableRow key={task.id}>
                    <TableCell>
                      <Link to={`/requests/${task.id}`} className="font-medium text-primary hover:underline">
                        {task.title}
                      </Link>
                    </TableCell>
                    <TableCell>{task.department}</TableCell>
                    <TableCell><UrgencyBadge urgency={task.urgency} /></TableCell>
                    <TableCell><StatusBadge status={task.status} /></TableCell>
                    <TableCell className="text-right font-medium">
                      ${task.totalAmount.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                    </TableCell>
                    <TableCell>{task.requesterName}</TableCell>
                    <TableCell className="text-muted-foreground">
                      {formatRelativeTime(task.updatedAt)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Card>
          {total > PAGE_SIZE && (
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                {(currentPage - 1) * PAGE_SIZE + 1}–{Math.min(currentPage * PAGE_SIZE, total)} of {total}
              </p>
              <div className="flex items-center gap-1">
                <Button variant="outline" size="icon" className="h-8 w-8" disabled={currentPage <= 1} onClick={() => setPage(currentPage - 1)}>
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <span className="text-sm px-2">{currentPage} / {totalPages}</span>
                <Button variant="outline" size="icon" className="h-8 w-8" disabled={currentPage >= totalPages} onClick={() => setPage(currentPage + 1)}>
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
