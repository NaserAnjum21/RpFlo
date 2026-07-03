import { type ReactNode, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  LayoutDashboard,
  FileText,
  CheckSquare,
  Bell,
  ChevronDown,
  Menu,
  Download,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuSeparator,
} from '@/components/ui/dropdown-menu';
import { Sheet, SheetContent, SheetTrigger, SheetTitle } from '@/components/ui/sheet';
import { ScrollArea } from '@/components/ui/scroll-area';
import { useAuth } from '@/hooks/useAuth';
import { notificationsApi } from '@/api/users';
import { procurementApi } from '@/api/procurement';
import type { NotificationResponse } from '@/types';
import { formatDateTime } from '@/lib/utils';

const roleColors: Record<string, string> = {
  Requester: 'bg-blue-100 text-blue-800',
  Manager: 'bg-purple-100 text-purple-800',
  Finance: 'bg-green-100 text-green-800',
  Admin: 'bg-orange-100 text-orange-800',
};

export function Layout({ children }: { children: ReactNode }) {
  const { currentUser, users, switchUser } = useAuth();
  const location = useLocation();
  const queryClient = useQueryClient();
  const [notifOpen, setNotifOpen] = useState(false);
  const [mobileNavOpen, setMobileNavOpen] = useState(false);

  const { data: unreadCount = 0 } = useQuery({
    queryKey: ['notifications', 'unread', currentUser?.id],
    queryFn: notificationsApi.getUnreadCount,
    refetchInterval: 10000,
    enabled: !!currentUser,
  });

  const { data: notifications = [] } = useQuery({
    queryKey: ['notifications', currentUser?.id],
    queryFn: notificationsApi.getAll,
    enabled: !!currentUser && notifOpen,
  });

  const navItems = [
    { path: '/', label: 'Dashboard', icon: LayoutDashboard },
    { path: '/requests', label: 'Requests', icon: FileText },
    ...(currentUser?.role === 'Manager' || currentUser?.role === 'Admin'
      ? [{ path: '/approvals', label: 'Approvals', icon: CheckSquare }]
      : []),
    ...(currentUser?.role === 'Finance' || currentUser?.role === 'Admin'
      ? [{ path: '/finance', label: 'Finance Review', icon: CheckSquare }]
      : []),
  ];

  const handleMarkAllRead = async () => {
    await notificationsApi.markAllAsRead();
    queryClient.invalidateQueries({ queryKey: ['notifications'] });
  };

  const handleMarkRead = async (id: string) => {
    await notificationsApi.markAsRead(id);
    queryClient.invalidateQueries({ queryKey: ['notifications'] });
  };

  if (!currentUser) return null;

  return (
    <div className="min-h-screen bg-background">
      <header className="sticky top-0 z-50 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="flex h-14 items-center px-4 md:px-6">
          <Link to="/" className="mr-6 flex items-center gap-2 font-semibold">
            <FileText className="h-5 w-5 text-primary" />
            <span className="hidden md:inline">ProcureFlow</span>
          </Link>

          <nav className="hidden md:flex items-center gap-1">
            {navItems.map(item => (
              <Link key={item.path} to={item.path}>
                <Button
                  variant={location.pathname === item.path ? 'secondary' : 'ghost'}
                  size="sm"
                  className="gap-2"
                >
                  <item.icon className="h-4 w-4" />
                  {item.label}
                </Button>
              </Link>
            ))}
          </nav>

          <div className="ml-auto flex items-center gap-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => procurementApi.exportCsv()}
              className="hidden md:flex gap-1"
            >
              <Download className="h-4 w-4" />
              Export
            </Button>

            {/* Notifications */}
            <Sheet open={notifOpen} onOpenChange={setNotifOpen}>
              <SheetTrigger className="relative inline-flex items-center justify-center rounded-md p-2 text-sm font-medium hover:bg-accent hover:text-accent-foreground">
                <Bell className="h-4 w-4" />
                {unreadCount > 0 && (
                  <Badge className="absolute -top-1 -right-1 h-5 w-5 p-0 flex items-center justify-center text-xs" variant="destructive">
                    {unreadCount}
                  </Badge>
                )}
              </SheetTrigger>
              <SheetContent>
                <SheetTitle className="flex items-center justify-between pr-4">
                  Notifications
                  {unreadCount > 0 && (
                    <Button variant="ghost" size="sm" onClick={handleMarkAllRead}>
                      Mark all read
                    </Button>
                  )}
                </SheetTitle>
                <ScrollArea className="h-[calc(100vh-8rem)] mt-4">
                  {notifications.length === 0 ? (
                    <p className="text-muted-foreground text-center py-8">No notifications</p>
                  ) : (
                    <div className="space-y-2 pr-4">
                      {notifications.map((n: NotificationResponse) => (
                        <div
                          key={n.id}
                          className={`p-3 rounded-lg border cursor-pointer transition-colors ${
                            n.isRead ? 'bg-background' : 'bg-muted'
                          }`}
                          onClick={() => !n.isRead && handleMarkRead(n.id)}
                        >
                          <p className="font-medium text-sm">{n.title}</p>
                          <p className="text-sm text-muted-foreground mt-1">{n.message}</p>
                          <p className="text-xs text-muted-foreground mt-1">
                            {formatDateTime(n.createdAt)}
                          </p>
                        </div>
                      ))}
                    </div>
                  )}
                </ScrollArea>
              </SheetContent>
            </Sheet>

            {/* User switcher */}
            <DropdownMenu>
              <DropdownMenuTrigger className="inline-flex items-center gap-2 rounded-md border px-3 py-1.5 text-sm font-medium hover:bg-accent">
                <div className="h-6 w-6 rounded-full bg-primary text-primary-foreground flex items-center justify-center text-xs font-medium">
                  {currentUser.name.charAt(0)}
                </div>
                <span className="hidden md:inline">{currentUser.name}</span>
                <Badge variant="secondary" className={roleColors[currentUser.role]}>
                  {currentUser.role}
                </Badge>
                <ChevronDown className="h-3 w-3" />
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-64">
                <div className="px-2 py-1.5 text-sm text-muted-foreground">Switch User</div>
                <DropdownMenuSeparator />
                {users.map(user => (
                  <DropdownMenuItem
                    key={user.id}
                    onClick={() => switchUser(user.id)}
                    className="flex items-center gap-2"
                  >
                    <div className="h-6 w-6 rounded-full bg-primary text-primary-foreground flex items-center justify-center text-xs font-medium">
                      {user.name.charAt(0)}
                    </div>
                    <div className="flex-1">
                      <p className="text-sm font-medium">{user.name}</p>
                      <p className="text-xs text-muted-foreground">{user.email}</p>
                    </div>
                    <Badge variant="secondary" className={`text-xs ${roleColors[user.role]}`}>
                      {user.role}
                    </Badge>
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>

            {/* Mobile nav */}
            <Sheet open={mobileNavOpen} onOpenChange={setMobileNavOpen}>
              <SheetTrigger className="md:hidden inline-flex items-center justify-center rounded-md p-2 hover:bg-accent">
                <Menu className="h-4 w-4" />
              </SheetTrigger>
              <SheetContent side="left">
                <SheetTitle>Navigation</SheetTitle>
                <nav className="flex flex-col gap-2 mt-4">
                  {navItems.map(item => (
                    <Link key={item.path} to={item.path} onClick={() => setMobileNavOpen(false)}>
                      <Button
                        variant={location.pathname === item.path ? 'secondary' : 'ghost'}
                        className="w-full justify-start gap-2"
                      >
                        <item.icon className="h-4 w-4" />
                        {item.label}
                      </Button>
                    </Link>
                  ))}
                </nav>
              </SheetContent>
            </Sheet>
          </div>
        </div>
      </header>

      <main className="p-4 md:p-6 max-w-7xl mx-auto">{children}</main>
    </div>
  );
}
