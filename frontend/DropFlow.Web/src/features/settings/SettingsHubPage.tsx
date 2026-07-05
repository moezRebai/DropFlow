import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  Settings, Users2, Building2, Clock, Truck, UserCog, Store,
  Receipt, ChevronRight,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { teamApi, teamKeys } from '@/api/team'
import { driversApi, driverKeys } from '@/api/drivers'
import { vehiclesApi, vehicleKeys } from '@/api/vehicles'
import { storesApi, storeKeys } from '@/api/stores'
import { timeslotsApi, timeslotKeys } from '@/api/timeslots'

interface HubCard {
  to: string
  title: string
  description: string
  icon: React.ElementType
  color: string
  count?: number
  disabled?: boolean
  badge?: string
}

export default function SettingsHubPage() {
  const countQuery = { page: 1, pageSize: 1 }

  const { data: team } = useQuery({ queryKey: teamKeys.users(false), queryFn: () => teamApi.getUsers(false) })
  const { data: drivers } = useQuery({ queryKey: driverKeys.list(countQuery), queryFn: () => driversApi.getList(countQuery) })
  const { data: vehicles } = useQuery({ queryKey: vehicleKeys.list(countQuery), queryFn: () => vehiclesApi.getList(countQuery) })
  const { data: stores } = useQuery({ queryKey: storeKeys.list(countQuery), queryFn: () => storesApi.getList(countQuery) })
  const { data: timeslots } = useQuery({ queryKey: timeslotKeys.lists(), queryFn: () => timeslotsApi.getAll() })

  const cards: HubCard[] = [
    {
      to: '/settings/team',
      title: 'Mon équipe',
      description: 'Inviter des membres, gérer les rôles et les accès',
      icon: Users2,
      color: 'bg-sky-100 text-sky-600',
      count: team?.length,
    },
    {
      to: '/settings/company',
      title: 'Entreprise',
      description: 'Informations légales, coordonnées et dépôts',
      icon: Building2,
      color: 'bg-violet-100 text-violet-600',
    },
    {
      to: '/settings/timeslots',
      title: 'Créneaux horaires',
      description: 'Plages de livraison réutilisables',
      icon: Clock,
      color: 'bg-amber-100 text-amber-600',
      count: timeslots?.length,
    },
    {
      to: '/settings/vehicles',
      title: 'Véhicules',
      description: 'Gérer la flotte et les capacités',
      icon: Truck,
      color: 'bg-emerald-100 text-emerald-600',
      count: vehicles?.totalCount,
    },
    {
      to: '/settings/drivers',
      title: 'Chauffeurs',
      description: 'Lier les chauffeurs aux comptes utilisateurs',
      icon: UserCog,
      color: 'bg-blue-100 text-blue-600',
      count: drivers?.totalCount,
    },
    {
      to: '/settings/stores',
      title: 'Enseignes',
      description: 'Points de vente pour lesquels vous livrez',
      icon: Store,
      color: 'bg-rose-100 text-rose-600',
      count: stores?.totalCount,
    },
    {
      to: '#',
      title: 'Facturation',
      description: 'Abonnement et paiements',
      icon: Receipt,
      color: 'bg-slate-100 text-slate-400',
      disabled: true,
      badge: 'Bientôt',
    },
  ]

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Hero */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-slate-700 to-slate-900 p-6 shadow-lg">
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }} />
        <div className="relative flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
            <Settings className="h-5 w-5 text-white" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-white">Paramètres</h1>
            <p className="text-sm text-slate-300">Configurez votre espace de travail</p>
          </div>
        </div>
      </div>

      {/* Cards */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {cards.map(card => {
          const inner = (
            <>
              <div className="flex items-start justify-between">
                <div className={cn('flex h-12 w-12 items-center justify-center rounded-xl transition-transform duration-300 group-hover:scale-110', card.color)}>
                  <card.icon className="h-6 w-6" />
                </div>
                {card.badge ? (
                  <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-400">{card.badge}</span>
                ) : card.count !== undefined ? (
                  <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-semibold text-slate-600">{card.count}</span>
                ) : null}
              </div>
              <div className="mt-4">
                <div className="flex items-center gap-1.5">
                  <h2 className="font-semibold text-slate-800">{card.title}</h2>
                  {!card.disabled && <ChevronRight className="h-4 w-4 text-slate-300 transition-transform group-hover:translate-x-0.5" />}
                </div>
                <p className="mt-1 text-sm text-slate-500">{card.description}</p>
              </div>
            </>
          )

          if (card.disabled) {
            return (
              <div key={card.title} className="cursor-not-allowed rounded-2xl border bg-white p-5 opacity-60 shadow-sm">
                {inner}
              </div>
            )
          }

          return (
            <Link
              key={card.title}
              to={card.to}
              className="group rounded-2xl border bg-white p-5 shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-transparent hover:shadow-lg"
            >
              {inner}
            </Link>
          )
        })}
      </div>
    </div>
  )
}
