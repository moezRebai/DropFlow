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
    <div className="flex h-full">
      {/* Sub-navigation */}
      <aside className="hidden w-52 shrink-0 flex-col border-r bg-slate-50/60 md:flex">
        <div className="border-b px-4 py-4">
          <p className="text-xs font-semibold uppercase tracking-widest text-slate-400">Paramètres</p>
        </div>
        <nav className="flex-1 space-y-0.5 p-2">
          {NAV_ITEMS.map(item => {
            const isActive = pathname.startsWith(item.path)
            return (
              <Link
                key={item.path}
                to={item.path}
                className={cn(
                  'flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-white text-sky-600 shadow-sm ring-1 ring-sky-100'
                    : 'text-slate-600 hover:bg-white hover:text-slate-800',
                )}
              >
                <item.icon className={cn(
                  'h-4 w-4 shrink-0',
                  isActive ? 'text-sky-500' : 'text-slate-400',
                )} />
                {item.label}
              </Link>
            )
          })}
        </nav>
      </aside>

      {/* Page content */}
      <div className="flex-1 overflow-auto">
        <Outlet />
      </div>
    </div>
  )
}
