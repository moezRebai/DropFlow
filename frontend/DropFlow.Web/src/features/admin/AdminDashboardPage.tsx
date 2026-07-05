import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  Building2, Users, UserCheck, UserPlus, TrendingUp, Activity,
  ShieldCheck, RefreshCw, ChevronRight, ScrollText, LayoutGrid,
} from 'lucide-react'
import { toast } from 'sonner'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import { useAuthStore } from '@/store/authStore'
import {
  adminApi, adminKeys, PLAN_LABELS, PLAN_COLORS,
} from '@/api/admin'
import { ROLE_LABELS } from '@/api/team'

// Backend returns the driver role as "Livreur"; map it alongside the app's "Driver".
const ROLE_LABEL_MAP: Record<string, string> = { ...ROLE_LABELS, Livreur: 'Chauffeur' }

// ─── Stat card ──────────────────────────────────────────────────────────────

interface StatCardProps {
  label: string
  value: string | number
  sub?: string
  icon: React.ReactNode
  iconBg: string
  iconColor: string
  onClick?: () => void
}

function StatCard({ label, value, sub, icon, iconBg, iconColor, onClick }: StatCardProps) {
  return (
    <div
      className={cn(
        'group relative overflow-hidden rounded-2xl border bg-white p-5 shadow-sm transition-all duration-300 hover:-translate-y-1 hover:shadow-lg',
        onClick && 'cursor-pointer',
      )}
      onClick={onClick}
    >
      <div className="mb-4 flex items-start justify-between">
        <div className={cn('flex h-12 w-12 items-center justify-center rounded-xl transition-transform duration-300 group-hover:scale-110', iconBg, iconColor)}>
          {icon}
        </div>
      </div>
      <p className="mb-1 text-3xl font-extrabold tracking-tight text-slate-900">{value}</p>
      <p className="text-sm font-medium text-slate-500">{label}</p>
      {sub && <p className="mt-1 text-xs text-slate-400">{sub}</p>}
    </div>
  )
}

// ─── Distribution bars ──────────────────────────────────────────────────────

const BAR_COLORS = ['#0284c7', '#10b981', '#f59e0b', '#8b5cf6', '#ef4444', '#14b8a6']

function DistributionCard({
  title, icon, data, colorMap, labelMap,
}: {
  title: string
  icon: React.ReactNode
  data: Record<string, number>
  colorMap?: Record<string, string>
  labelMap?: Record<string, string>
}) {
  const entries = Object.entries(data).sort((a, b) => b[1] - a[1])
  const total = entries.reduce((sum, [, v]) => sum + v, 0)
  const max = entries.length ? Math.max(...entries.map(([, v]) => v)) : 0

  return (
    <div className="rounded-2xl border bg-white p-6 shadow-sm">
      <div className="mb-5 flex items-center gap-2">
        <span className="text-slate-500">{icon}</span>
        <h2 className="font-semibold text-slate-800">{title}</h2>
      </div>
      {entries.length === 0 ? (
        <p className="py-6 text-center text-sm text-slate-400">Aucune donnée</p>
      ) : (
        <div className="flex flex-col gap-4">
          {entries.map(([key, value], i) => {
            const pct = total > 0 ? Math.round((value / total) * 100) : 0
            const barWidth = max > 0 ? Math.round((value / max) * 100) : 0
            const color = BAR_COLORS[i % BAR_COLORS.length]
            return (
              <div key={key} className="grid items-center gap-3" style={{ gridTemplateColumns: 'minmax(0,120px) 1fr 56px 44px' }}>
                <span className={cn('inline-flex w-fit items-center rounded-full px-2.5 py-0.5 text-xs font-medium', colorMap?.[key] ?? 'bg-slate-100 text-slate-600')}>
                  {labelMap?.[key] ?? key}
                </span>
                <div className="h-2.5 overflow-hidden rounded-full bg-slate-100">
                  <div className="h-full rounded-full transition-all duration-700" style={{ width: `${barWidth}%`, background: color }} />
                </div>
                <span className="text-right text-sm font-bold text-slate-800">{value}</span>
                <span className="text-right text-xs text-slate-400">{pct}%</span>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

// ─── Page ───────────────────────────────────────────────────────────────────

export default function AdminDashboardPage() {
  const navigate = useNavigate()
  const user = useAuthStore(s => s.user)

  const { data: stats, isLoading: statsLoading, refetch: refetchStats } = useQuery({
    queryKey: adminKeys.stats(),
    queryFn: adminApi.getStats,
  })
  const { data: userStats, isLoading: userStatsLoading, refetch: refetchUserStats } = useQuery({
    queryKey: adminKeys.userStats(),
    queryFn: adminApi.getUserStats,
  })

  async function handleRefresh() {
    await Promise.all([refetchStats(), refetchUserStats()])
    toast.success('Données actualisées')
  }

  const isLoading = statsLoading || userStatsLoading

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Hero */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-violet-600 to-indigo-700 p-6 shadow-lg">
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }} />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="mb-1 flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
                <ShieldCheck className="h-5 w-5 text-white" />
              </div>
              <h1 className="text-2xl font-bold tracking-tight text-white">
                Console Super Admin
              </h1>
            </div>
            <p className="text-sm text-violet-200">
              Bonjour {user?.firstName ?? 'Admin'} — vue d'ensemble de la plateforme DropFlow
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <button
              onClick={() => navigate('/admin/tenants')}
              className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-white/25"
            >
              <Building2 className="h-3.5 w-3.5" />Entreprises
            </button>
            <button
              onClick={() => navigate('/admin/audit-logs')}
              className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-white/25"
            >
              <ScrollText className="h-3.5 w-3.5" />Logs
            </button>
            <button
              onClick={handleRefresh}
              className="flex h-9 w-9 items-center justify-center rounded-xl bg-white/15 text-white transition-colors hover:bg-white/25"
              title="Actualiser"
            >
              <RefreshCw className="h-4 w-4" />
            </button>
          </div>
        </div>
      </div>

      {/* KPI cards */}
      {isLoading ? (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-36 rounded-2xl" />)}
        </div>
      ) : (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          <StatCard
            label="Entreprises"
            value={stats?.totalTenants ?? 0}
            sub={`${stats?.activeTenants ?? 0} actives · ${stats?.inactiveTenants ?? 0} inactives`}
            icon={<Building2 className="h-5 w-5" />}
            iconBg="bg-violet-100" iconColor="text-violet-600"
            onClick={() => navigate('/admin/tenants')}
          />
          <StatCard
            label="Utilisateurs"
            value={stats?.totalUsers ?? 0}
            sub={`${stats?.activeUsers ?? 0} actifs`}
            icon={<Users className="h-5 w-5" />}
            iconBg="bg-sky-100" iconColor="text-sky-600"
            onClick={() => navigate('/admin/users')}
          />
          <StatCard
            label="Nouvelles entreprises"
            value={stats?.tenantsCreatedThisMonth ?? 0}
            sub={`${stats?.tenantsCreatedThisWeek ?? 0} cette semaine`}
            icon={<TrendingUp className="h-5 w-5" />}
            iconBg="bg-emerald-100" iconColor="text-emerald-600"
          />
          <StatCard
            label="Nouveaux utilisateurs"
            value={stats?.usersCreatedThisMonth ?? 0}
            sub={`${userStats?.usersCreatedThisWeek ?? 0} cette semaine`}
            icon={<UserPlus className="h-5 w-5" />}
            iconBg="bg-amber-100" iconColor="text-amber-600"
          />
        </div>
      )}

      {/* Distributions */}
      {isLoading ? (
        <div className="grid gap-6 lg:grid-cols-2">
          <Skeleton className="h-64 rounded-2xl" />
          <Skeleton className="h-64 rounded-2xl" />
        </div>
      ) : (
        <div className="grid gap-6 lg:grid-cols-2">
          <DistributionCard
            title="Répartition par plan"
            icon={<LayoutGrid className="h-4 w-4" />}
            data={stats?.tenantsByPlan ?? {}}
            colorMap={PLAN_COLORS}
            labelMap={PLAN_LABELS}
          />
          <DistributionCard
            title="Répartition par rôle"
            icon={<UserCheck className="h-4 w-4" />}
            data={userStats?.usersByRole ?? {}}
            labelMap={ROLE_LABEL_MAP}
          />
        </div>
      )}

      {/* Secondary stats */}
      {!isLoading && userStats && (
        <div className="rounded-2xl border bg-white p-6 shadow-sm">
          <div className="mb-5 flex items-center gap-2">
            <Activity className="h-4 w-4 text-slate-500" />
            <h2 className="font-semibold text-slate-800">Activité utilisateurs</h2>
          </div>
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            {[
              { label: 'Total', value: userStats.totalUsers, color: 'text-slate-800' },
              { label: 'Actifs', value: userStats.activeUsers, color: 'text-emerald-600' },
              { label: 'Inactifs', value: userStats.inactiveUsers, color: 'text-red-500' },
              { label: 'Entreprises servies', value: Object.keys(userStats.usersByTenant ?? {}).length, color: 'text-violet-600' },
            ].map((s, i) => (
              <div key={i} className="flex flex-col items-center gap-1 rounded-xl border bg-slate-50 p-4 text-center">
                <p className={cn('text-2xl font-extrabold', s.color)}>{s.value}</p>
                <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{s.label}</p>
              </div>
            ))}
          </div>
          <button
            onClick={() => navigate('/admin/users')}
            className="mt-4 flex w-full items-center justify-center gap-1 rounded-xl border border-dashed border-slate-200 py-2.5 text-sm font-medium text-sky-600 transition-colors hover:bg-sky-50"
          >
            Gérer les utilisateurs <ChevronRight className="h-4 w-4" />
          </button>
        </div>
      )}
    </div>
  )
}
