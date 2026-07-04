import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
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

  const { data: tasks = [], isLoading } = useQuery({
    queryKey: ['procurement', 'pending', currentUser?.id],
    queryFn: procurementApi.getPending,
    enabled: !!currentUser,
  });

  const resetPage = () => setPage(1);

  const dateFiltered = tasks.filter(r => {
    if (dateFrom && new Date(r.createdAt) < new Date(dateFrom)) return false;
    if (dateTo) {
      const to = new Date(dateTo);
      to.setDate(to.getDate() + 1);
      if (new Date(r.createdAt) >= to) return false;
    }
    return true;
  });

  const sorted = [...dateFiltered].sort((a, b) => {
    const urgencyOrder: Record<string, number> = { Critical: 0, High: 1, Medium: 2, Low: 3 };
    const ua = urgencyOrder[a.urgency] ?? 4;
    const ub = urgencyOrder[b.urgency] ?? 4;
    if (ua !== ub) return ua - ub;
    return new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
  });

  const totalPages = Math.max(1, Math.ceil(sorted.length / PAGE_SIZE));
  const paginated = sorted.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  if (isLoading) {
    return <div className="text-center py-12 text-muted-foreground">Loading...</div>;
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">My Tasks</h1>
        <p className="text-muted-foreground">
          {getTaskDescription(currentUser?.role)} · {sorted.length} item{sorted.length !== 1 ? 's' : ''}
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

      {sorted.length === 0 ? (
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
                {paginated.map((task: ProcurementListItem) => (
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
          {sorted.length > PAGE_SIZE && (
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                {(page - 1) * PAGE_SIZE + 1}–{Math.min(page * PAGE_SIZE, sorted.length)} of {sorted.length}
              </p>
              <div className="flex items-center gap-1">
                <Button variant="outline" size="icon" className="h-8 w-8" disabled={page <= 1} onClick={() => setPage(page - 1)}>
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <span className="text-sm px-2">{page} / {totalPages}</span>
                <Button variant="outline" size="icon" className="h-8 w-8" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>
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
