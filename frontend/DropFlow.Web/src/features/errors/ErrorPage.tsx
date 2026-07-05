import { Component, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { AlertTriangle, Home, RotateCcw, Compass } from 'lucide-react'
import { Button } from '@/components/ui/button'

// ─── Presentational error view ──────────────────────────────────────────────

interface ErrorViewProps {
  code: string
  title: string
  message: string
  icon?: ReactNode
  primary?: ReactNode
  secondary?: ReactNode
}

function ErrorView({ code, title, message, icon, primary, secondary }: ErrorViewProps) {
  return (
    <div className="flex min-h-[70vh] flex-col items-center justify-center gap-6 p-6 text-center">
      <div className="relative">
        <p className="text-[7rem] font-extrabold leading-none tracking-tight text-slate-100 sm:text-[9rem]">{code}</p>
        <div className="absolute inset-0 flex items-center justify-center">
          <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 text-white shadow-lg">
            {icon ?? <AlertTriangle className="h-8 w-8" />}
          </div>
        </div>
      </div>
      <div className="max-w-md">
        <h1 className="text-2xl font-bold tracking-tight text-slate-800">{title}</h1>
        <p className="mt-2 text-sm text-slate-500">{message}</p>
      </div>
      <div className="flex flex-wrap items-center justify-center gap-3">
        {primary}
        {secondary}
      </div>
    </div>
  )
}

// ─── 404 Not Found ──────────────────────────────────────────────────────────

export function NotFoundPage() {
  return (
    <ErrorView
      code="404"
      icon={<Compass className="h-8 w-8" />}
      title="Page introuvable"
      message="La page que vous recherchez n'existe pas ou a été déplacée."
      primary={
        <Button asChild>
          <Link to="/"><Home className="mr-1.5 h-4 w-4" />Retour à l'accueil</Link>
        </Button>
      }
      secondary={
        <Button variant="outline" onClick={() => window.history.back()}>
          Page précédente
        </Button>
      }
    />
  )
}

// ─── Error boundary (runtime render errors) ─────────────────────────────────

interface BoundaryState {
  hasError: boolean
  error?: Error
}

export class ErrorBoundary extends Component<{ children: ReactNode }, BoundaryState> {
  state: BoundaryState = { hasError: false }

  static getDerivedStateFromError(error: Error): BoundaryState {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, info: unknown) {
    // Surface the error for diagnostics; a real logger could be plugged in here.
    console.error('Unhandled UI error:', error, info)
  }

  handleReset = () => {
    this.setState({ hasError: false, error: undefined })
  }

  render() {
    if (this.state.hasError) {
      return (
        <ErrorView
          code="500"
          title="Une erreur est survenue"
          message="Un problème inattendu a interrompu cette page. Vous pouvez réessayer ou revenir à l'accueil."
          primary={
            <Button onClick={() => window.location.reload()}>
              <RotateCcw className="mr-1.5 h-4 w-4" />Recharger la page
            </Button>
          }
          secondary={
            <Button asChild variant="outline" onClick={this.handleReset}>
              <Link to="/"><Home className="mr-1.5 h-4 w-4" />Accueil</Link>
            </Button>
          }
        />
      )
    }
    return this.props.children
  }
}
