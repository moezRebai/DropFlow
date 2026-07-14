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
      <div className="mx-auto max-w-md text-center motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-bottom-4 motion-safe:duration-500">
        <div className="mb-6 flex justify-center">
          <div className="flex h-20 w-20 items-center justify-center rounded-full bg-destructive/10">
            <Lock className="h-9 w-9 text-destructive" />
          </div>
        </div>

        <p className="text-7xl font-bold text-muted-foreground/30">403</p>
        <h1 className="mt-3 text-2xl font-semibold">Oups ! Vous n&apos;êtes pas autorisé ici</h1>
        <p className="mt-3 text-base text-muted-foreground">
          Cette page nécessite des permissions spéciales que vous ne possédez pas.
        </p>

        <div className="mt-8 flex flex-col gap-3">
          <Button
            asChild
            className="h-12 bg-gradient-to-r from-sky-500 to-blue-600 text-base shadow-md shadow-blue-600/20 transition-all duration-200 hover:shadow-lg hover:shadow-blue-600/30 active:scale-[0.98]"
          >
            <Link to={dashboardPath}>Retour au tableau de bord</Link>
          </Button>
          <Button asChild variant="ghost" className="h-12 text-base text-muted-foreground">
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
