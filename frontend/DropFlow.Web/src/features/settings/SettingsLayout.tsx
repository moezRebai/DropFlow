import { Link, Outlet, useLocation } from 'react-router-dom'
import { Building2, Clock, ShoppingBag, Truck, UserCheck, Users } from 'lucide-react'
import { cn } from '@/lib/utils'

const NAV_ITEMS = [
  { path: '/settings/company',   label: 'Entreprise',  icon: Building2   },
  { path: '/settings/drivers',   label: 'Chauffeurs',  icon: UserCheck   },
  { path: '/settings/vehicles',  label: 'Véhicules',   icon: Truck       },
  { path: '/settings/stores',    label: 'Enseignes',   icon: ShoppingBag },
  { path: '/settings/timeslots', label: 'Créneaux',    icon: Clock       },
  { path: '/settings/team',      label: 'Équipe',      icon: Users       },
]

export function SettingsLayout() {
  const { pathname } = useLocation()

  return (
    <div className="flex h-full flex-col md:flex-row">
      {/* Sub-navigation (desktop) */}
      <aside className="hidden w-52 shrink-0 flex-col border-r bg-muted/60 md:flex">
        <div className="border-b px-4 py-4">
          <p className="text-xs font-semibold uppercase tracking-widest text-muted-foreground">Paramètres</p>
        </div>
        <nav className="flex-1 space-y-0.5 p-2">
          {NAV_ITEMS.map(item => {
            const isActive = pathname.startsWith(item.path)
            return (
              <Link
                key={item.path}
                to={item.path}
                aria-current={isActive ? 'page' : undefined}
                className={cn(
                  'flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-card text-sky-600 shadow-sm ring-1 ring-sky-100 dark:text-sky-400 dark:ring-sky-500/20'
                    : 'text-muted-foreground hover:bg-card hover:text-foreground',
                )}
              >
                <item.icon className={cn(
                  'h-4 w-4 shrink-0',
                  isActive ? 'text-sky-500 dark:text-sky-400' : 'text-muted-foreground',
                )} />
                {item.label}
              </Link>
            )
          })}
        </nav>
      </aside>

      {/* Sub-navigation (mobile) */}
      <nav className="flex shrink-0 gap-1.5 overflow-x-auto border-b bg-muted/60 px-3 py-2 md:hidden">
        {NAV_ITEMS.map(item => {
          const isActive = pathname.startsWith(item.path)
          return (
            <Link
              key={item.path}
              to={item.path}
              aria-current={isActive ? 'page' : undefined}
              className={cn(
                'flex shrink-0 items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-card text-sky-600 shadow-sm ring-1 ring-sky-100 dark:text-sky-400 dark:ring-sky-500/20'
                  : 'text-muted-foreground hover:bg-card hover:text-foreground',
              )}
            >
              <item.icon className={cn(
                'h-4 w-4 shrink-0',
                isActive ? 'text-sky-500 dark:text-sky-400' : 'text-muted-foreground',
              )} />
              {item.label}
            </Link>
          )
        })}
      </nav>

      {/* Page content */}
      <div className="flex-1 overflow-auto">
        <Outlet />
      </div>
    </div>
  )
}
