import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider, useAuth } from '@/hooks/useAuth';
import { Layout } from '@/components/Layout';
import { Dashboard } from '@/pages/Dashboard';
import { RequestList } from '@/pages/RequestList';
import { RequestDetail } from '@/pages/RequestDetail';
import { CreateRequest } from '@/pages/CreateRequest';
import { Approvals } from '@/pages/Approvals';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
    },
  },
});

function AppRoutes() {
  const { isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center text-muted-foreground">
        Loading...
      </div>
    );
  }

  return (
    <Layout>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/requests" element={<RequestList />} />
        <Route path="/requests/new" element={<CreateRequest />} />
        <Route path="/requests/:id" element={<RequestDetail />} />
        <Route path="/approvals" element={<Approvals role="Manager" />} />
        <Route path="/finance" element={<Approvals role="Finance" />} />
      </Routes>
    </Layout>
  );
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <AppRoutes />
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  );
}
