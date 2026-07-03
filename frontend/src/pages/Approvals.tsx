import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Card } from '@/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { StatusBadge, UrgencyBadge } from '@/components/StatusBadge';
import { procurementApi } from '@/api/procurement';
import { formatDate } from '@/lib/utils';

export function Approvals({ role }: { role: 'Manager' | 'Finance' }) {
  const { data: pending = [], isLoading } = useQuery({
    queryKey: ['procurement', 'all'],
    queryFn: procurementApi.getAll,
  });

  const filteredPending = pending.filter(r =>
    role === 'Manager' ? r.status === 'Submitted' : r.status === 'ManagerApproved'
  );

  const title = role === 'Manager' ? 'Manager Approvals' : 'Finance Review';
  const subtitle = role === 'Manager'
    ? 'Requests awaiting your approval'
    : 'Requests awaiting finance approval';

  if (isLoading) {
    return <div className="text-center py-12 text-muted-foreground">Loading...</div>;
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">{title}</h1>
        <p className="text-muted-foreground">{subtitle} · {filteredPending.length} pending</p>
      </div>

      {filteredPending.length === 0 ? (
        <Card className="p-8 text-center text-muted-foreground">
          No requests pending your review.
        </Card>
      ) : (
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
              {filteredPending.map(request => (
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
                  <TableCell>{request.requesterName}</TableCell>
                  <TableCell className="text-muted-foreground">
                    {formatDate(request.createdAt)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>
      )}
    </div>
  );
}
