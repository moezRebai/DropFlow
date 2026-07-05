import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, Legend,
} from 'recharts'
import {
  Truck, AlertTriangle, BarChart3, Route, Users, Car,
  TrendingUp, TrendingDown, Minus, Calendar, RefreshCw, Plus,
  MapPin, Clock, ChevronRight, CheckCircle, Eye, Pencil,
  Zap, ShieldAlert, Euro, Package, ArrowRight,
} from 'lucide-react'
import { toast } from 'sonner'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import { useAuthStore } from '@/store/authStore'
import { dashboardApi, dashboardKeys, ChartPeriod } from '@/api/dashboard'
import type { RiskyDeliveryDto, TodayDeliveryDto, DashboardStatsDto } from '@/api/dashboard'

// ─── Helpers ──────────────────────────────────────────────────────────────────

function getInitials(name: string): string {
  const parts = name.trim().split(' ').filter(Boolean)
  if (parts.length >= 2) return `${parts[0][0]}${parts[1][0]}`.toUpperCase()
  return (parts[0]?.substring(0, 2) ?? '?').toUpperCase()
}

function formatRevenue(n: number): string {
  return n.toLocaleString('fr-FR', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 })
}

function formatTime(t?: string): string {
  if (!t) return 'Non défini'
  return t.substring(0, 5)
}

function getGreeting(): string {
  const h = new Date().getHours()
  if (h < 12) return 'Bonjour'
  if (h < 18) return 'Bon après-midi'
  return 'Bonsoir'
}

function formatDateFr(date: Date): string {
  return date.toLocaleDateString('fr-FR', {
    weekday: 'long', day: 'numeric', month: 'long', year: 'numeric',
  })
}

const RISK_COLORS: Record<string, { bg: string; border: string; icon: string; chip: string }> = {
  Warning: { bg: 'bg-amber-50', border: 'border-l-amber-400', icon: 'bg-amber-100 text-amber-600', chip: 'bg-amber-100 text-amber-700' },
  Error:   { bg: 'bg-red-50',   border: 'border-l-red-400',   icon: 'bg-red-100 text-red-600',     chip: 'bg-red-100 text-red-700'   },
  Info:    { bg: 'bg-blue-50',  border: 'border-l-blue-400',  icon: 'bg-blue-100 text-blue-600',   chip: 'bg-blue-100 text-blue-700' },
}

const STORE_COLORS = [
  '#0284c7', '#10b981', '#f59e0b', '#ef4444',
  '#8b5cf6', '#14b8a6', '#f97316', '#ec4899',
]

function getRankMedal(i: number): string {
  if (i === 0) return '🥇'
  if (i === 1) return '🥈'
  if (i === 2) return '🥉'
  return `#${i + 1}`
}

const PERIOD_LABELS: Record<ChartPeriod, string> = {
  [ChartPeriod.Week]: 'Semaine',
  [ChartPeriod.Month]: 'Mois',
  [ChartPeriod.Year]: 'Année',
}

// ─── Sparkline (SVG) ──────────────────────────────────────────────────────────

function Sparkline({ data, color }: { data: number[]; color: string }) {
  if (data.length < 2) return null
  const max = Math.max(...data, 1)
  const min = Math.min(...data)
  const range = max - min || 1
  const W = 80, H = 28, pad = 2
  const points = data
    .map((v, i) => {
      const x = pad + (i / (data.length - 1)) * (W - pad * 2)
      const y = H - pad - ((v - min) / range) * (H - pad * 2)
      return `${x.toFixed(1)},${y.toFixed(1)}`
    })
    .join(' ')
  return (
    <svg width={W} height={H} viewBox={`0 0 ${W} ${H}`} className="overflow-visible opacity-60">
      <polyline
        points={points}
        fill="none"
        stroke={color}
        strokeWidth={2}
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

// ─── KPI Card ─────────────────────────────────────────────────────────────────

interface KpiCardProps {
  label: string
  value: string | number
  icon: React.ReactNode
  iconBg: string
  iconColor: string
  gradientFrom: string
  gradientTo: string
  trend?: { value: string | number; up: boolean; neutral?: boolean }
  progress?: { filled: number; max: number; label: string }
  sparklineData?: number[]
  sparklineColor?: string
  onClick?: () => void
}

function KpiCard({
  label, value, icon, iconBg, iconColor, gradientFrom, gradientTo,
  trend, progress, sparklineData, sparklineColor, onClick,
}: KpiCardProps) {
  const pct = progress ? Math.round((progress.filled / Math.max(progress.max, 1)) * 100) : 0

  return (
    <div
      className={cn(
        'group relative overflow-hidden rounded-2xl border bg-white p-5 shadow-sm',
        'transition-all duration-300 hover:-translate-y-1 hover:shadow-lg hover:border-transparent',
        onClick && 'cursor-pointer',
      )}
      onClick={onClick}
    >
      <div
        className="absolute inset-x-0 top-0 h-0.5 opacity-0 transition-opacity duration-300 group-hover:opacity-100"
        style={{ background: `linear-gradient(to right, ${gradientFrom}, ${gradientTo})` }}
      />
      <div className="mb-4 flex items-start justify-between">
        <div className={cn('flex h-12 w-12 items-center justify-center rounded-xl transition-transform duration-300 group-hover:scale-110', iconBg, iconColor)}>
          {icon}
        </div>
        <div className="flex flex-col items-end gap-2">
          {trend && (
            <div className={cn(
              'flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-semibold',
              trend.neutral
                ? 'bg-slate-100 text-slate-500'
                : trend.up
                  ? 'bg-emerald-100 text-emerald-600'
                  : 'bg-red-100 text-red-600',
            )}>
              {trend.neutral
                ? <Minus className="h-3 w-3" />
                : trend.up
                  ? <TrendingUp className="h-3 w-3" />
                  : <TrendingDown className="h-3 w-3" />}
              {trend.value}
            </div>
          )}
          {sparklineData && sparklineData.length >= 2 && (
            <Sparkline data={sparklineData} color={sparklineColor ?? gradientFrom} />
          )}
        </div>
      </div>
      <p className="mb-1 text-3xl font-extrabold tracking-tight text-slate-900">{value}</p>
      <p className="text-sm font-medium text-slate-500">{label}</p>
      {progress && (
        <div className="mt-3">
          <div className="h-1.5 w-full overflow-hidden rounded-full bg-slate-100">
            <div
              className="h-full rounded-full transition-all duration-700"
              style={{ width: `${pct}%`, background: `linear-gradient(to right, ${gradientFrom}, ${gradientTo})` }}
            />
          </div>
          <div className="mt-1.5 flex justify-between text-xs text-slate-400">
            <span>{progress.label}</span>
            <span>{pct}%</span>
          </div>
        </div>
      )}
    </div>
  )
}

// ─── Pulse Bar ────────────────────────────────────────────────────────────────

interface PulseMetric {
  icon: React.ReactNode
  label: string
  value: string | number
  valueClass: string
  bg: string
  iconClass: string
  alert?: boolean
  alertRingClass?: string
}

function PulseBar({
  stats,
  todayDeliveries,
  isLoading,
}: {
  stats?: DashboardStatsDto
  todayDeliveries: TodayDeliveryDto[]
  isLoading: boolean
}) {
  const delivered = stats?.deliveredToday ?? 0
  const total = stats?.todayDeliveries ?? 0
  const pct = total > 0 ? delivered / total : 0
  const lateCount = todayDeliveries.filter(d => d.isLate || d.status === 'Late').length
  const unplanned = stats?.unplannedDeliveries ?? 0

  const SIZE = 84, STROKE = 7
  const r = (SIZE - STROKE) / 2
  const circ = 2 * Math.PI * r
  const offset = circ * (1 - pct)
  const ringColor = pct >= 0.8 ? '#10b981' : pct >= 0.5 ? '#0284c7' : '#f59e0b'

  const metrics: PulseMetric[] = [
    {
      icon: <Route className="h-5 w-5" />,
      label: 'Tournées actives',
      value: stats ? `${stats.activeRoutes}/${stats.totalRoutesToday}` : '—',
      valueClass: 'text-blue-700',
      bg: 'bg-blue-50',
      iconClass: 'text-blue-500',
    },
    {
      icon: <Users className="h-5 w-5" />,
      label: 'Chauffeurs en route',
      value: stats?.driversOnRoad ?? '—',
      valueClass: 'text-emerald-700',
      bg: 'bg-emerald-50',
      iconClass: 'text-emerald-500',
    },
    {
      icon: <Car className="h-5 w-5" />,
      label: 'Véhicules inactifs',
      value: stats?.idleVehicles ?? '—',
      valueClass: 'text-slate-600',
      bg: 'bg-slate-100',
      iconClass: 'text-slate-400',
    },
    {
      icon: <AlertTriangle className="h-5 w-5" />,
      label: 'En retard',
      value: lateCount,
      valueClass: lateCount > 0 ? 'text-red-700' : 'text-slate-600',
      bg: lateCount > 0 ? 'bg-red-50' : 'bg-slate-50',
      iconClass: lateCount > 0 ? 'text-red-500' : 'text-slate-400',
      alert: lateCount > 0,
      alertRingClass: 'ring-1 ring-red-200',
    },
    {
      icon: <Package className="h-5 w-5" />,
      label: 'Non planifiées',
      value: unplanned,
      valueClass: unplanned > 0 ? 'text-amber-700' : 'text-slate-600',
      bg: unplanned > 0 ? 'bg-amber-50' : 'bg-slate-50',
      iconClass: unplanned > 0 ? 'text-amber-500' : 'text-slate-400',
      alert: unplanned > 0,
      alertRingClass: 'ring-1 ring-amber-200',
    },
  ]

  return (
    <div className="rounded-2xl border bg-white p-5 shadow-sm transition-shadow hover:shadow-md">
      <div className="mb-4 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Zap className="h-4 w-4 text-slate-500" />
          <h2 className="font-semibold text-slate-800">Pulse opérationnel</h2>
        </div>
        <span className="flex items-center gap-1.5 rounded-full bg-emerald-100 px-2.5 py-0.5 text-xs font-semibold text-emerald-600">
          <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-emerald-500" />
          Temps réel
        </span>
      </div>

      {isLoading ? (
        <div className="flex gap-4">
          <Skeleton className="h-24 w-24 shrink-0 rounded-full" />
          <div className="flex flex-1 gap-3">
            {[...Array(5)].map((_, i) => <Skeleton key={i} className="h-24 flex-1 rounded-xl" />)}
          </div>
        </div>
      ) : (
        <div className="flex flex-wrap items-center gap-5">
          {/* Ring gauge */}
          <div className="flex shrink-0 flex-col items-center gap-1">
            <svg width={SIZE} height={SIZE} viewBox={`0 0 ${SIZE} ${SIZE}`}>
              <circle cx={SIZE / 2} cy={SIZE / 2} r={r} fill="none" stroke="#f1f5f9" strokeWidth={STROKE} />
              <circle
                cx={SIZE / 2} cy={SIZE / 2} r={r} fill="none"
                stroke={ringColor}
                strokeWidth={STROKE}
                strokeDasharray={circ}
                strokeDashoffset={offset}
                strokeLinecap="round"
                transform={`rotate(-90 ${SIZE / 2} ${SIZE / 2})`}
                style={{ transition: 'stroke-dashoffset 0.7s ease' }}
              />
              <text x={SIZE / 2} y={SIZE / 2 - 5} textAnchor="middle" dominantBaseline="middle" fontSize="15" fontWeight="800" fill="#0f172a">
                {Math.round(pct * 100)}%
              </text>
              <text x={SIZE / 2} y={SIZE / 2 + 11} textAnchor="middle" dominantBaseline="middle" fontSize="9" fill="#94a3b8">
                {delivered}/{total}
              </text>
            </svg>
            <p className="text-xs font-semibold text-slate-500">Livrées</p>
          </div>

          <div className="h-16 w-px shrink-0 bg-slate-100" />

          {/* Metric tiles */}
          <div className="grid flex-1 grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
            {metrics.map((m, i) => (
              <div
                key={i}
                className={cn(
                  'flex flex-col items-center gap-1.5 rounded-xl p-3 text-center transition-colors',
                  m.bg,
                  m.alert && m.alertRingClass,
                )}
              >
                <span className={cn(m.iconClass)}>{m.icon}</span>
                <p className={cn('text-2xl font-extrabold leading-none', m.valueClass)}>{m.value}</p>
                <p className="text-xs font-medium leading-tight text-slate-500">{m.label}</p>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

// ─── Delivery Swim Lanes ──────────────────────────────────────────────────────

interface LaneConfig {
  id: string
  label: string
  filter: (d: TodayDeliveryDto) => boolean
  headerBg: string
  headerBorder: string
  dotColor: string
  textColor: string
  countBg: string
  cardBorder: string
  ctaLabel?: string
  ctaClass?: string
  showCta: boolean
}

const SWIM_LANES: LaneConfig[] = [
  {
    id: 'planned',
    label: 'À planifier',
    filter: d => ['ToBePlanned', 'Confirmed'].includes(d.status) && !d.isLate,
    headerBg: 'bg-amber-50',
    headerBorder: 'border-amber-200',
    dotColor: 'bg-amber-400',
    textColor: 'text-amber-700',
    countBg: 'bg-amber-100 text-amber-700',
    cardBorder: 'border-amber-100 hover:border-amber-300',
    ctaLabel: 'Planifier',
    ctaClass: 'bg-amber-100 text-amber-700 hover:bg-amber-200',
    showCta: true,
  },
  {
    id: 'inprogress',
    label: 'En cours',
    filter: d => d.status === 'InProgress' && !d.isLate,
    headerBg: 'bg-purple-50',
    headerBorder: 'border-purple-200',
    dotColor: 'bg-purple-400',
    textColor: 'text-purple-700',
    countBg: 'bg-purple-100 text-purple-700',
    cardBorder: 'border-purple-100 hover:border-purple-300',
    showCta: false,
  },
  {
    id: 'delivered',
    label: 'Livrée',
    filter: d => d.status === 'Delivered',
    headerBg: 'bg-emerald-50',
    headerBorder: 'border-emerald-200',
    dotColor: 'bg-emerald-400',
    textColor: 'text-emerald-700',
    countBg: 'bg-emerald-100 text-emerald-700',
    cardBorder: 'border-emerald-100 hover:border-emerald-300',
    showCta: false,
  },
  {
    id: 'late',
    label: 'En retard',
    filter: d => d.status === 'Late' || (d.isLate && d.status !== 'Delivered'),
    headerBg: 'bg-red-50',
    headerBorder: 'border-red-200',
    dotColor: 'bg-red-400',
    textColor: 'text-red-700',
    countBg: 'bg-red-100 text-red-700',
    cardBorder: 'border-red-100 hover:border-red-300',
    ctaLabel: 'Gérer',
    ctaClass: 'bg-red-100 text-red-700 hover:bg-red-200',
    showCta: true,
  },
]

function DeliverySwimLanes({ items, isLoading }: { items: TodayDeliveryDto[]; isLoading: boolean }) {
  const navigate = useNavigate()

  return (
    <div className="rounded-2xl border bg-white shadow-sm transition-shadow hover:shadow-md">
      <div className="flex items-center justify-between border-b px-6 py-4">
        <div className="flex items-center gap-2">
          <Truck className="h-4 w-4 text-slate-500" />
          <h2 className="font-semibold text-slate-800">Livraisons du jour</h2>
          {items.length > 0 && (
            <span className="rounded-full bg-sky-100 px-2 py-0.5 text-xs font-semibold text-sky-600">
              {items.length}
            </span>
          )}
        </div>
        <button
          onClick={() => navigate('/deliveries')}
          className="flex items-center gap-1 text-xs font-medium text-sky-600 transition-colors hover:text-sky-700"
        >
          Voir tout <ChevronRight className="h-3.5 w-3.5" />
        </button>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-2 gap-4 p-4 lg:grid-cols-4">
          {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-48 rounded-xl" />)}
        </div>
      ) : items.length === 0 ? (
        <div className="flex flex-col items-center gap-3 py-12 text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
            <Truck className="h-6 w-6" />
          </div>
          <p className="text-sm text-slate-500">Aucune livraison planifiée aujourd'hui</p>
        </div>
      ) : (
        <div className="grid grid-cols-2 gap-3 p-4 lg:grid-cols-4">
          {SWIM_LANES.map(lane => {
            const laneItems = items.filter(lane.filter)
            return (
              <div key={lane.id} className="flex flex-col gap-2">
                {/* Lane header */}
                <div className={cn(
                  'flex items-center gap-2 rounded-xl border px-3 py-2',
                  lane.headerBg, lane.headerBorder,
                )}>
                  <span className={cn('h-2 w-2 shrink-0 rounded-full', lane.dotColor)} />
                  <span className={cn('flex-1 text-sm font-semibold', lane.textColor)}>{lane.label}</span>
                  <span className={cn('rounded-full px-1.5 py-0.5 text-xs font-bold', lane.countBg)}>
                    {laneItems.length}
                  </span>
                </div>

                {/* Lane cards */}
                <div className="flex max-h-80 flex-col gap-2 overflow-y-auto">
                  {laneItems.length === 0 ? (
                    <div className="flex flex-col items-center gap-1 rounded-xl border border-dashed border-slate-200 py-6 text-center">
                      <CheckCircle className="h-4 w-4 text-slate-300" />
                      <p className="text-xs text-slate-400">Aucune</p>
                    </div>
                  ) : (
                    laneItems.map(d => (
                      <div
                        key={d.id}
                        className={cn(
                          'group rounded-xl border bg-white p-3 shadow-sm transition-all hover:-translate-y-0.5 hover:shadow-md',
                          lane.cardBorder,
                        )}
                      >
                        <div className="mb-2 flex items-start gap-2">
                          <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-sky-500 to-blue-600 text-xs font-bold text-white">
                            {getInitials(d.clientName)}
                          </div>
                          <div className="min-w-0 flex-1">
                            <p className="truncate text-sm font-semibold text-slate-800">{d.clientName}</p>
                            <p className="text-xs text-slate-400">{d.reference}</p>
                          </div>
                        </div>
                        <div className="flex flex-col gap-0.5 mb-2">
                          <p className="flex items-center gap-1 text-xs text-slate-500">
                            <MapPin className="h-3 w-3 shrink-0 text-slate-400" />
                            <span className="truncate">{d.deliveryCity}</span>
                          </p>
                          <p className="flex items-center gap-1 text-xs text-slate-500">
                            <Clock className="h-3 w-3 shrink-0 text-slate-400" />
                            {formatTime(d.scheduledTime)}
                          </p>
                        </div>
                        <div className="flex items-center justify-between gap-1">
                          <div className="flex items-center gap-1">
                            <button
                              onClick={() => navigate(`/deliveries/${d.id}`)}
                              className="rounded p-1 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
                              title="Voir"
                            >
                              <Eye className="h-3.5 w-3.5" />
                            </button>
                            <button
                              onClick={() => navigate(`/deliveries/${d.id}/edit`)}
                              className="rounded p-1 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
                              title="Modifier"
                            >
                              <Pencil className="h-3.5 w-3.5" />
                            </button>
                          </div>
                          {lane.showCta && lane.ctaLabel && (
                            <button
                              onClick={() => navigate(`/deliveries/${d.id}/edit`)}
                              className={cn(
                                'flex items-center gap-1 rounded-lg px-2 py-1 text-xs font-semibold transition-colors',
                                lane.ctaClass,
                              )}
                            >
                              {lane.ctaLabel} <ArrowRight className="h-3 w-3" />
                            </button>
                          )}
                        </div>
                      </div>
                    ))
                  )}
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

// ─── Risk Section (Action Cards) ──────────────────────────────────────────────

function RiskSection({ items }: { items: RiskyDeliveryDto[] }) {
  const navigate = useNavigate()
  return (
    <div className="rounded-2xl border bg-white shadow-sm transition-shadow hover:shadow-md">
      <div className="flex items-center justify-between border-b px-5 py-4">
        <div className="flex items-center gap-2">
          <ShieldAlert className="h-4 w-4 text-slate-500" />
          <h2 className="font-semibold text-slate-800">À planifier en urgence</h2>
          {items.length > 0 && (
            <span className="rounded-full bg-red-100 px-2 py-0.5 text-xs font-semibold text-red-600">
              {items.length}
            </span>
          )}
        </div>
        <button
          onClick={() => navigate('/deliveries')}
          className="flex items-center gap-1 text-xs font-medium text-sky-600 transition-colors hover:text-sky-700"
        >
          Voir tout <ChevronRight className="h-3.5 w-3.5" />
        </button>
      </div>
      <div className="p-4">
        {items.length === 0 ? (
          <div className="flex flex-col items-center gap-2 py-6 text-center">
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-emerald-100 text-emerald-600">
              <CheckCircle className="h-5 w-5" />
            </div>
            <p className="text-sm text-slate-500">Aucun risque détecté</p>
          </div>
        ) : (
          <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
            {items.map(r => {
              const colors = RISK_COLORS[r.riskLevel] ?? RISK_COLORS.Info
              const ctaBg =
                r.riskLevel === 'Error'
                  ? 'bg-red-600 text-white hover:bg-red-700'
                  : r.riskLevel === 'Warning'
                    ? 'bg-amber-500 text-white hover:bg-amber-600'
                    : 'bg-sky-600 text-white hover:bg-sky-700'
              return (
                <div
                  key={r.id}
                  className={cn(
                    'flex items-center gap-3 rounded-xl border-l-4 p-3 transition-transform hover:translate-x-0.5',
                    colors.bg, colors.border,
                  )}
                >
                  <div className={cn('flex h-9 w-9 shrink-0 items-center justify-center rounded-full', colors.icon)}>
                    <Clock className="h-4 w-4" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-semibold text-slate-800">{r.reference}</p>
                    <div className="mt-0.5 flex flex-wrap items-center gap-2">
                      <p className="flex items-center gap-1 text-xs text-slate-400">
                        <MapPin className="h-3 w-3" /> {r.deliveryCity}
                      </p>
                      <span className={cn('rounded-full px-2 py-0.5 text-xs font-semibold', colors.chip)}>
                        {r.riskReason}
                      </span>
                    </div>
                  </div>
                  <button
                    onClick={() => navigate(`/deliveries/${r.id}/edit`)}
                    className={cn(
                      'flex shrink-0 items-center gap-1 rounded-lg px-2.5 py-1.5 text-xs font-semibold transition-colors',
                      ctaBg,
                    )}
                  >
                    Planifier <ArrowRight className="h-3 w-3" />
                  </button>
                </div>
              )
            })}
          </div>
        )}
      </div>
    </div>
  )
}

// ─── DashboardPage ────────────────────────────────────────────────────────────

export default function DashboardPage() {
  const navigate = useNavigate()
  const user = useAuthStore(s => s.user)
  const [period, setPeriod] = useState<ChartPeriod>(ChartPeriod.Month)

  const { data: stats, isLoading: statsLoading, refetch: refetchStats } = useQuery({
    queryKey: dashboardKeys.stats(),
    queryFn: dashboardApi.getStats,
  })
  const { data: todayDeliveries = [], isLoading: deliveriesLoading, refetch: refetchDeliveries } = useQuery({
    queryKey: dashboardKeys.todayDeliveries(),
    queryFn: dashboardApi.getTodayDeliveries,
  })
  const { data: riskyDeliveries = [], refetch: refetchRisky } = useQuery({
    queryKey: dashboardKeys.riskyDeliveries(),
    queryFn: dashboardApi.getRiskyDeliveries,
  })
  const { data: revenueChart, isLoading: chartLoading } = useQuery({
    queryKey: dashboardKeys.revenueChart(period),
    queryFn: () => dashboardApi.getRevenueChart(period),
  })
  const { data: statusChart } = useQuery({
    queryKey: dashboardKeys.statusChart(period),
    queryFn: () => dashboardApi.getStatusChart(period),
  })
  const { data: storeChart } = useQuery({
    queryKey: dashboardKeys.storeChart(period),
    queryFn: () => dashboardApi.getStoreChart(period),
  })
  // Sparkline source — always weekly regardless of the performance chart period
  const { data: weekChart } = useQuery({
    queryKey: dashboardKeys.revenueChart(ChartPeriod.Week),
    queryFn: () => dashboardApi.getRevenueChart(ChartPeriod.Week),
  })

  async function handleRefresh() {
    await Promise.all([refetchStats(), refetchDeliveries(), refetchRisky()])
    toast.success('Données actualisées')
  }

  const chartData = revenueChart
    ? revenueChart.labels.map((label, i) => ({
        label,
        livraisons: revenueChart.deliveryCount[i] ?? 0,
        revenus: revenueChart.revenues[i] ?? 0,
      }))
    : []

  const totalDeliveries = revenueChart?.deliveryCount.reduce((a, b) => a + b, 0) ?? 0
  const totalRevenue = revenueChart?.revenues.reduce((a, b) => a + b, 0) ?? 0
  const successRate =
    statusChart && statusChart.totalCount > 0
      ? Math.round((statusChart.deliveredCount / statusChart.totalCount) * 100)
      : 0

  const storeTotal = storeChart?.revenues.reduce((a, b) => a + b, 0) ?? 0
  const storeMax = storeChart?.revenues.length ? Math.max(...storeChart.revenues) : 0

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* ── Hero Banner ──────────────────────────────────────────────────── */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 p-6 shadow-lg">
        <div
          className="absolute inset-0 opacity-10"
          style={{
            backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)',
            backgroundSize: '24px 24px',
          }}
        />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h1 className="mb-1 text-2xl font-bold tracking-tight text-white">
              {getGreeting()}, {user?.firstName ?? 'Utilisateur'} 👋
            </h1>
            <div className="flex flex-wrap items-center gap-3">
              <p className="text-sm text-sky-200">Vue d'ensemble de vos opérations</p>
              <span className="inline-flex items-center gap-1.5 rounded-full bg-white/15 px-3 py-1 text-xs font-medium text-white backdrop-blur-sm">
                <Calendar className="h-3.5 w-3.5" />
                {formatDateFr(new Date())}
              </span>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            <span className="mr-1 text-xs font-semibold uppercase tracking-wider text-sky-200">
              Actions rapides
            </span>
            <div className="flex items-center gap-1.5 rounded-2xl border border-white/20 bg-white/10 p-1.5 backdrop-blur-sm">
              <button
                onClick={() => navigate('/deliveries/new')}
                className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-white/25"
              >
                <Plus className="h-3.5 w-3.5" />
                Livraison
              </button>
              <button
                onClick={() => navigate('/routes/new')}
                className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-white/25"
              >
                <Route className="h-3.5 w-3.5" />
                Tournée
              </button>
              <button
                onClick={() => navigate('/deliveries?status=Late')}
                className="flex items-center gap-1.5 rounded-xl bg-amber-400/40 px-3 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-amber-400/60"
              >
                <AlertTriangle className="h-3.5 w-3.5" />
                En retard
              </button>
            </div>
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

      {/* ── KPI Cards ────────────────────────────────────────────────────── */}
      {statsLoading ? (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-40 rounded-2xl" />)}
        </div>
      ) : (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          <KpiCard
            label="Livraisons du jour"
            value={stats?.todayDeliveries ?? 0}
            icon={<Truck className="h-5 w-5" />}
            iconBg="bg-sky-100"
            iconColor="text-sky-600"
            gradientFrom="#0ea5e9"
            gradientTo="#0284c7"
            trend={{ value: `${stats?.deliveredToday ?? 0}/${stats?.todayDeliveries ?? 0}`, up: true }}
            progress={{
              filled: stats?.deliveredToday ?? 0,
              max: stats?.todayDeliveries ?? 1,
              label: `${stats?.deliveredToday ?? 0} livrées`,
            }}
            sparklineData={weekChart?.deliveryCount}
            sparklineColor="#0284c7"
            onClick={() => navigate('/deliveries')}
          />
          <KpiCard
            label="Non planifiées"
            value={stats?.unplannedDeliveries ?? 0}
            icon={<Package className="h-5 w-5" />}
            iconBg="bg-amber-100"
            iconColor="text-amber-600"
            gradientFrom="#f59e0b"
            gradientTo="#d97706"
            trend={
              stats
                ? {
                    value: stats.unplannedTrend > 0 ? `+${stats.unplannedTrend}` : stats.unplannedTrend.toString(),
                    up: stats.unplannedTrend <= 0,
                    neutral: stats.unplannedTrend === 0,
                  }
                : undefined
            }
            progress={{
              filled: stats?.unplannedDeliveries ?? 0,
              max: stats?.todayDeliveries ?? 1,
              label: `Sur ${stats?.todayDeliveries ?? 0} total`,
            }}
            onClick={() => navigate('/deliveries')}
          />
          <KpiCard
            label="Factures en attente"
            value={0}
            icon={<Euro className="h-5 w-5" />}
            iconBg="bg-red-100"
            iconColor="text-red-600"
            gradientFrom="#ef4444"
            gradientTo="#dc2626"
            trend={{ value: '0', up: true, neutral: true }}
            onClick={() => toast.info('Module Factures bientôt disponible')}
          />
          <KpiCard
            label="Revenus du mois"
            value={formatRevenue(stats?.monthlyRevenue ?? 0)}
            icon={<BarChart3 className="h-5 w-5" />}
            iconBg="bg-emerald-100"
            iconColor="text-emerald-600"
            gradientFrom="#10b981"
            gradientTo="#059669"
            trend={
              stats
                ? {
                    value: `${stats.revenueTrend >= 0 ? '+' : ''}${stats.revenueTrend.toFixed(1)}%`,
                    up: stats.revenueTrend >= 0,
                  }
                : undefined
            }
            sparklineData={weekChart?.revenues}
            sparklineColor="#10b981"
            onClick={() => toast.info('Module Rapports bientôt disponible')}
          />
        </div>
      )}

      {/* ── Pulse Bar ────────────────────────────────────────────────────── */}
      <PulseBar stats={stats} todayDeliveries={todayDeliveries} isLoading={statsLoading} />

      {/* ── Swim Lanes ───────────────────────────────────────────────────── */}
      <DeliverySwimLanes items={todayDeliveries} isLoading={deliveriesLoading} />

      {/* ── Risk Action Cards ─────────────────────────────────────────────── */}
      <RiskSection items={riskyDeliveries} />

      {/* ── Performance Chart ─────────────────────────────────────────────── */}
      <div className="rounded-2xl border bg-white p-6 shadow-sm transition-shadow hover:shadow-md">
        <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-2">
            <BarChart3 className="h-4 w-4 text-slate-500" />
            <h2 className="font-semibold text-slate-800">Performance</h2>
          </div>
          <div className="flex gap-1 rounded-xl bg-slate-100 p-1">
            {([ChartPeriod.Week, ChartPeriod.Month, ChartPeriod.Year] as ChartPeriod[]).map(p => (
              <button
                key={p}
                onClick={() => setPeriod(p)}
                className={cn(
                  'rounded-lg px-3 py-1.5 text-sm font-medium transition-all',
                  period === p
                    ? 'bg-white text-slate-900 shadow-sm'
                    : 'text-slate-500 hover:text-slate-700',
                )}
              >
                {PERIOD_LABELS[p]}
              </button>
            ))}
          </div>
        </div>

        {/* Mini KPI tiles */}
        <div className="mb-6 grid grid-cols-3 gap-4">
          {[
            {
              icon: <Truck className="h-4 w-4" />,
              bg: 'bg-sky-100', color: 'text-sky-600', hoverBg: 'hover:bg-sky-50',
              value: totalDeliveries.toLocaleString('fr-FR'),
              label: 'Livraisons',
            },
            {
              icon: <Euro className="h-4 w-4" />,
              bg: 'bg-emerald-100', color: 'text-emerald-600', hoverBg: 'hover:bg-emerald-50',
              value: `${totalRevenue.toFixed(1)} k€`,
              label: 'Revenus',
            },
            {
              icon: <CheckCircle className="h-4 w-4" />,
              bg: 'bg-blue-100', color: 'text-blue-600', hoverBg: 'hover:bg-blue-50',
              value: statusChart?.totalCount === 0 ? '—' : `${successRate}%`,
              label: 'Taux de succès',
            },
          ].map((item, i) => (
            <div
              key={i}
              className={cn(
                'flex flex-col items-center gap-1 rounded-xl border bg-slate-50 p-4 text-center transition-colors',
                item.hoverBg,
              )}
            >
              <div className={cn('flex h-9 w-9 items-center justify-center rounded-full', item.bg, item.color)}>
                {item.icon}
              </div>
              <p className="text-2xl font-extrabold text-slate-900">{item.value}</p>
              <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{item.label}</p>
            </div>
          ))}
        </div>

        {chartLoading ? (
          <Skeleton className="h-64 rounded-xl" />
        ) : (
          <ResponsiveContainer width="100%" height={260}>
            <AreaChart data={chartData} margin={{ top: 5, right: 10, bottom: 0, left: -20 }}>
              <defs>
                <linearGradient id="gradDeliveries" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#0284c7" stopOpacity={0.15} />
                  <stop offset="95%" stopColor="#0284c7" stopOpacity={0.01} />
                </linearGradient>
                <linearGradient id="gradRevenues" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#10b981" stopOpacity={0.15} />
                  <stop offset="95%" stopColor="#10b981" stopOpacity={0.01} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" vertical={false} />
              <XAxis
                dataKey="label"
                tick={{ fontSize: 11, fill: '#94a3b8' }}
                axisLine={false}
                tickLine={false}
              />
              <YAxis
                tick={{ fontSize: 11, fill: '#94a3b8' }}
                axisLine={false}
                tickLine={false}
              />
              <Tooltip
                contentStyle={{
                  borderRadius: '12px',
                  border: 'none',
                  boxShadow: '0 4px 20px rgba(0,0,0,0.1)',
                  fontSize: '12px',
                }}
                labelStyle={{ fontWeight: 600, color: '#0f172a' }}
              />
              <Legend
                iconType="circle"
                iconSize={8}
                wrapperStyle={{ fontSize: '12px', paddingTop: '16px' }}
              />
              <Area
                type="monotone"
                dataKey="livraisons"
                name="Livraisons"
                stroke="#0284c7"
                strokeWidth={2.5}
                fill="url(#gradDeliveries)"
                dot={false}
                activeDot={{ r: 5, fill: '#0284c7' }}
              />
              <Area
                type="monotone"
                dataKey="revenus"
                name="Revenus (k€)"
                stroke="#10b981"
                strokeWidth={2.5}
                fill="url(#gradRevenues)"
                dot={false}
                activeDot={{ r: 5, fill: '#10b981' }}
              />
            </AreaChart>
          </ResponsiveContainer>
        )}
      </div>

      {/* ── Store Leaderboard ─────────────────────────────────────────────── */}
      <div className="rounded-2xl border bg-white p-6 shadow-sm transition-shadow hover:shadow-md">
        <div className="mb-6 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <BarChart3 className="h-4 w-4 text-slate-500" />
            <h2 className="font-semibold text-slate-800">Top enseignes</h2>
          </div>
          <p className="text-xs text-slate-400">Revenus pour la période sélectionnée</p>
        </div>

        {!storeChart || storeChart.storeNames.length === 0 ? (
          <div className="flex flex-col items-center gap-3 py-8 text-center">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
              <BarChart3 className="h-6 w-6" />
            </div>
            <p className="text-sm text-slate-500">Aucune donnée disponible</p>
          </div>
        ) : (
          <>
            <div className="flex flex-col gap-4">
              {storeChart.storeNames.map((name, i) => {
                const rev = storeChart.revenues[i] ?? 0
                const pct = storeTotal > 0 ? Math.round((rev / storeTotal) * 100) : 0
                const barWidth = storeMax > 0 ? Math.round((rev / storeMax) * 100) : 0
                const color = STORE_COLORS[i % STORE_COLORS.length]
                return (
                  <div
                    key={i}
                    className="grid items-center gap-3"
                    style={{ gridTemplateColumns: '40px minmax(0,160px) 1fr 90px 40px' }}
                  >
                    <span className="text-center text-base leading-none">{getRankMedal(i)}</span>
                    <span className="overflow-hidden text-ellipsis whitespace-nowrap text-sm font-semibold text-slate-700">
                      {name}
                    </span>
                    <div className="h-2.5 overflow-hidden rounded-full bg-slate-100">
                      <div
                        className="h-full rounded-full transition-all duration-700"
                        style={{ width: `${barWidth}%`, background: color }}
                      />
                    </div>
                    <span className="text-right text-sm font-bold text-slate-800">{rev.toFixed(1)} k€</span>
                    <span className="text-right text-xs text-slate-400">{pct}%</span>
                  </div>
                )
              })}
            </div>
            <div className="mt-4 border-t pt-4 text-right text-sm text-slate-500">
              Total période :{' '}
              <strong className="text-slate-800">{storeTotal.toFixed(1)} k€</strong>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
