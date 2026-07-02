import { useQuery } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { procurementApi } from '@/api/procurement';
import { FileText, Clock, CheckCircle, XCircle, Package, DollarSign } from 'lucide-react';

export function Dashboard() {
  const { data: metrics, isLoading } = useQuery({
    queryKey: ['metrics'],
    queryFn: procurementApi.getMetrics,
  });

  if (isLoading || !metrics) {
    return <div className="flex items-center justify-center py-12 text-muted-foreground">Loading dashboard...</div>;
  }

  const statCards = [
    { title: 'Total Requests', value: metrics.totalRequests, icon: FileText, color: 'text-blue-600' },
    { title: 'Pending Approval', value: metrics.pendingApprovalCount, icon: Clock, color: 'text-yellow-600' },
    { title: 'Approved', value: metrics.approvedCount, icon: CheckCircle, color: 'text-green-600' },
    { title: 'Rejected', value: metrics.rejectedCount, icon: XCircle, color: 'text-red-600' },
    { title: 'POs Issued', value: metrics.purchaseOrderCount, icon: Package, color: 'text-purple-600' },
    {
      title: 'Approved Amount',
      value: `$${metrics.totalApprovedAmount.toLocaleString('en-US', { minimumFractionDigits: 2 })}`,
      icon: DollarSign,
      color: 'text-emerald-600',
    },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">Overview of procurement requests</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {statCards.map(card => (
          <Card key={card.title}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">{card.title}</CardTitle>
              <card.icon className={`h-4 w-4 ${card.color}`} />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{card.value}</div>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Status Breakdown</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {metrics.statusBreakdown.map(item => (
                <div key={item.status} className="flex items-center justify-between">
                  <span className="text-sm">{formatStatus(item.status)}</span>
                  <div className="flex items-center gap-2">
                    <div className="w-24 h-2 rounded-full bg-muted overflow-hidden">
                      <div
                        className="h-full rounded-full bg-primary"
                        style={{ width: `${(item.count / metrics.totalRequests) * 100}%` }}
                      />
                    </div>
                    <span className="text-sm font-medium w-6 text-right">{item.count}</span>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Department Summary</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {metrics.departmentBreakdown.map(item => (
                <div key={item.department} className="flex items-center justify-between">
                  <span className="text-sm">{item.department}</span>
                  <div className="flex items-center gap-3">
                    <span className="text-sm text-muted-foreground">{item.count} requests</span>
                    <span className="text-sm font-medium">
                      ${item.totalAmount.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>

      {metrics.averageProcessingTimeHours > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Processing Metrics</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              Average processing time: <span className="font-medium text-foreground">{metrics.averageProcessingTimeHours} hours</span>
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}

function formatStatus(status: string): string {
  return status.replace(/([A-Z])/g, ' $1').trim();
}
