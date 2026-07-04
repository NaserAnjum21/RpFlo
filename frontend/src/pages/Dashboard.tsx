import { useQuery } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { DashboardSkeleton } from '@/components/Skeleton';
import { useAuth } from '@/hooks/useAuth';
import { procurementApi } from '@/api/procurement';
import {
  FileText, Clock, CheckCircle, XCircle, Package, DollarSign,
  User, Send, ClipboardList, TrendingUp, Banknote,
} from 'lucide-react';
import type { UserRole } from '@/types';

const fmt = (n: number) => n.toLocaleString('en-US', { minimumFractionDigits: 2 });

export function Dashboard() {
  const { currentUser } = useAuth();

  const { data: metrics, isLoading } = useQuery({
    queryKey: ['metrics', currentUser?.id],
    queryFn: procurementApi.getMetrics,
  });

  if (isLoading || !metrics) {
    return <DashboardSkeleton />;
  }

  const role = currentUser?.role;
  const rm = metrics.roleMetrics;

  const globalCards = [
    { title: 'Total Requests', value: metrics.totalRequests, icon: FileText, color: 'text-blue-600' },
    { title: 'Pending Approval', value: metrics.pendingApprovalCount, icon: Clock, color: 'text-yellow-600' },
    { title: 'Approved', value: metrics.approvedCount, icon: CheckCircle, color: 'text-green-600' },
    { title: 'Rejected', value: metrics.rejectedCount, icon: XCircle, color: 'text-red-600' },
    { title: 'POs Issued', value: metrics.purchaseOrderCount, icon: Package, color: 'text-purple-600' },
    {
      title: 'Approved Amount',
      value: `$${fmt(metrics.totalApprovedAmount)}`,
      icon: DollarSign,
      color: 'text-emerald-600',
    },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">
          {role === 'Requester'
            ? 'Your procurement overview'
            : 'Overview of procurement requests'}
        </p>
      </div>

      {rm && <RoleCards role={role!} rm={rm} />}

      {role !== 'Requester' && (
        <>
          <h2 className="text-lg font-semibold text-muted-foreground">Organization Overview</h2>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {globalCards.map(card => (
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
        </>
      )}

      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Status Breakdown</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {(role === 'Requester' && rm?.myStatusBreakdown ? rm.myStatusBreakdown : metrics.statusBreakdown).map(item => {
                const total = role === 'Requester' && rm?.myStatusBreakdown
                  ? rm.myStatusBreakdown.reduce((s, i) => s + i.count, 0)
                  : metrics.totalRequests;
                return (
                  <div key={item.status} className="flex items-center justify-between">
                    <span className="text-sm">{formatStatus(item.status)}</span>
                    <div className="flex items-center gap-2">
                      <div className="w-24 h-2 rounded-full bg-muted overflow-hidden">
                        <div
                          className="h-full rounded-full bg-primary"
                          style={{ width: `${total > 0 ? (item.count / total) * 100 : 0}%` }}
                        />
                      </div>
                      <span className="text-sm font-medium w-6 text-right">{item.count}</span>
                    </div>
                  </div>
                );
              })}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Department Summary</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {(role === 'Requester' && rm?.myDepartmentBreakdown ? rm.myDepartmentBreakdown : metrics.departmentBreakdown).map(item => (
                <div key={item.department} className="flex items-center justify-between">
                  <span className="text-sm">{item.department}</span>
                  <div className="flex items-center gap-3">
                    <span className="text-sm text-muted-foreground">{item.count} requests</span>
                    <span className="text-sm font-medium">
                      ${fmt(item.totalAmount)}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function RoleCards({ role, rm }: { role: UserRole; rm: NonNullable<import('@/types').RoleMetrics> }) {
  const cards = getRoleCards(role, rm);

  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
      {cards.map(card => (
        <Card key={card.title}>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">{card.title}</CardTitle>
            <card.icon className={`h-4 w-4 ${card.color}`} />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{card.value}</div>
            {card.subtitle && (
              <p className="text-xs text-muted-foreground mt-1">{card.subtitle}</p>
            )}
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

function getRoleCards(role: UserRole, rm: NonNullable<import('@/types').RoleMetrics>) {
  switch (role) {
    case 'Requester':
      return [
        { title: 'My Active Requests', value: rm.myActiveRequests, icon: FileText, color: 'text-blue-600' },
        { title: 'Pending Approval', value: rm.myPendingApproval, icon: Send, color: 'text-yellow-600', subtitle: 'Awaiting review' },
        { title: 'My Approved Amount', value: `$${fmt(rm.myApprovedAmount)}`, icon: DollarSign, color: 'text-green-600' },
      ];
    case 'Manager':
      return [
        { title: 'Pending My Review', value: rm.pendingMyReview, icon: ClipboardList, color: 'text-yellow-600', subtitle: 'Awaiting your approval' },
        { title: 'Approved This Month', value: rm.approvedThisMonth, icon: TrendingUp, color: 'text-green-600' },
        { title: 'My Active Requests', value: rm.myActiveRequests, icon: FileText, color: 'text-blue-600' },
      ];
    case 'Finance':
      return [
        { title: 'Pending Finance Review', value: rm.pendingMyReview, icon: ClipboardList, color: 'text-yellow-600', subtitle: 'Awaiting your review' },
        { title: 'Ready for PO', value: rm.readyForPo, icon: Package, color: 'text-blue-600', subtitle: 'Finance approved' },
        { title: 'Value Pending', value: `$${fmt(rm.totalValuePending)}`, icon: Banknote, color: 'text-orange-600' },
        { title: 'Monthly Spend', value: `$${fmt(rm.monthlySpendApproved)}`, icon: DollarSign, color: 'text-green-600', subtitle: 'POs issued this month' },
      ];
    case 'Admin':
      return [
        { title: 'Pending Manager Review', value: rm.pendingMyReview, icon: ClipboardList, color: 'text-yellow-600' },
        { title: 'Pending Finance Review', value: rm.readyForPo + (rm.pendingMyReview > 0 ? 0 : 0), icon: User, color: 'text-orange-600' },
        { title: 'Ready for PO', value: rm.readyForPo, icon: Package, color: 'text-blue-600' },
        { title: 'Monthly Spend', value: `$${fmt(rm.monthlySpendApproved)}`, icon: DollarSign, color: 'text-green-600' },
      ];
  }
}

function formatStatus(status: string): string {
  return status.replace(/([A-Z])/g, ' $1').trim();
}
