import { Link } from 'react-router-dom'
import { HelpCircle, Lock } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/store/authStore'
import { getRedirectPath } from '@/api/auth'

export default function AccessDeniedPage() {
  const user = useAuthStore(s => s.user)
  const dashboardPath = user ? getRedirectPath(user.role, user.tenantId) : '/login'

  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-muted/30 px-4">
      <div className="mx-auto max-w-sm text-center">
        <div className="mb-4 flex justify-center">
          <div className="flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10">
            <Lock className="h-8 w-8 text-destructive" />
          </div>
        </div>

        <p className="text-6xl font-bold text-muted-foreground/30">403</p>
        <h1 className="mt-2 text-xl font-semibold">Oups ! Vous n&apos;êtes pas autorisé ici</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Cette page nécessite des permissions spéciales que vous ne possédez pas.
        </p>

        <div className="mt-6 flex flex-col gap-3">
          <Button asChild>
            <Link to={dashboardPath}>Retour au tableau de bord</Link>
          </Button>
          <Button asChild variant="ghost" className="text-muted-foreground">
            <Link to="/help">
              <HelpCircle className="mr-2 h-4 w-4" />
              Contacter le support
            </Link>
          </Button>
        </div>
      </div>
    </div>
  )
}
