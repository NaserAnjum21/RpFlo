import { Component, type ReactNode } from 'react';
import { Button } from '@/components/ui/button';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
}

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="min-h-screen flex items-center justify-center bg-background">
          <div className="text-center space-y-3">
            <p className="text-lg font-medium text-destructive">Something went wrong</p>
            <p className="text-sm text-muted-foreground">An unexpected error occurred in the application.</p>
            <Button variant="outline" onClick={() => { this.setState({ hasError: false }); window.location.reload(); }}>
              Reload
            </Button>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}
