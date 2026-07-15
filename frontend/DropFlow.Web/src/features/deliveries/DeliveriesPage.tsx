import { useState, useCallback, useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import {
  LayoutGrid, List, Plus, Search, X, ChevronLeft, ChevronRight,
  Trash2, RefreshCw, Package, Truck, CalendarDays, Eye, Pencil,
  MapPinOff, Clock, AlertTriangle, Locate, CalendarX,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Checkbox } from '@/components/ui/checkbox'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table'
import {
  deliveriesApi, deliveryKeys, DeliveryStatus, DeliveryType,
  STATUS_LABELS, STATUS_COLORS, URGENT_BADGE_CLASS,
  type DeliveryViewDto, type DeliveryFilterDto,
} from '@/api/deliveries'
import { DeliveryStatusBadge } from './DeliveryStatusBadge'
import { deliveryEventBus } from '@/lib/deliveryEventBus'
import { cn } from '@/lib/utils'

const DEFAULT_STATUSES = [DeliveryStatus.ToBePlanned, DeliveryStatus.Confirmed, DeliveryStatus.InProgress]
const PAGE_SIZE_OPTIONS = [20, 40, 60]

type DeliveryStatusFilter = DeliveryStatus | 'all' | 'active' | 'issues'

const STATUS_TABS: Array<{ label: string; value: DeliveryStatusFilter }> = [
  { label: 'Actives', value: 'active' },
  { label: 'Toutes', value: 'all' },
  { label: 'À planifier', value: DeliveryStatus.ToBePlanned },
  { label: 'Confirmée', value: DeliveryStatus.Confirmed },
  { label: 'En cours', value: DeliveryStatus.InProgress },
  { label: 'Livrée', value: DeliveryStatus.Delivered },
  { label: 'Annulée', value: DeliveryStatus.Canceled },
  { label: 'Problèmes', value: 'issues' },
]

// France bounding box
const isInFrance = (lat: number, lng: number) =>
  lat >= 41.3 && lat <= 51.2 && lng >= -5.2 && lng <= 9.6

function hasGeoIssue(d: DeliveryViewDto) {
  return d.latitude == null || d.longitude == null || !isInFrance(d.latitude, d.longitude)
}
function hasDurationIssue(d: DeliveryViewDto) {
  return !d.estimatedDurationMinutes
}
function hasStatusDateIssue(d: DeliveryViewDto) {
  return (d.status !== DeliveryStatus.ToBePlanned && !d.scheduledDate) ||
         (!!d.scheduledDate && d.status === DeliveryStatus.ToBePlanned)
}
function hasIssue(d: DeliveryViewDto) {
  return hasGeoIssue(d) || hasDurationIssue(d) || hasStatusDateIssue(d)
}

function formatDate(iso?: string) {
  if (!iso) return null
  return new Date(iso).toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function formatPrice(n: number) {
  return n.toLocaleString('fr-FR', { style: 'currency', currency: 'EUR' })
}

// ─── Warning icons ─────────────────────────────────────────────────────────────

function WarningIcons({ delivery, onGeocode }: { delivery: DeliveryViewDto; onGeocode: (id: number) => void }) {
  const geoIssue = hasGeoIssue(delivery)
  const durIssue = hasDurationIssue(delivery)
  const statusDateIssue = hasStatusDateIssue(delivery)
  if (!geoIssue && !durIssue && !statusDateIssue) return null
  return (
    <div className="flex items-center gap-1">
      {geoIssue && (
        <button
          title="Adresse non géolocalisée — cliquer pour géocoder"
          aria-label="Géocoder l'adresse"
          onClick={e => { e.stopPropagation(); onGeocode(delivery.id) }}
          className="flex items-center justify-center rounded p-0.5 text-amber-500 hover:bg-amber-50 hover:text-amber-600 dark:hover:bg-amber-500/10 dark:hover:text-amber-400 transition-colors"
        >
          <MapPinOff className="h-3.5 w-3.5" />
        </button>
      )}
      {durIssue && (
        <span title="Durée de service non renseignée" className="flex items-center justify-center rounded p-0.5 text-muted-foreground">
          <Clock className="h-3.5 w-3.5" />
        </span>
      )}
      {statusDateIssue && (
        <span
          title={
            delivery.status !== DeliveryStatus.ToBePlanned && !delivery.scheduledDate
              ? 'Statut confirmé/en cours sans date de livraison'
              : 'Date planifiée mais statut "À planifier"'
          }
          className="flex items-center justify-center rounded p-0.5 text-orange-500 dark:text-orange-400"
        >
          <CalendarX className="h-3.5 w-3.5" />
        </span>
      )}
    </div>
  )
}

// ─── Stat chip ─────────────────────────────────────────────────────────────────

function StatChip({ icon, label, className }: { icon: React.ReactNode; label: string; className?: string }) {
  return (
    <span className={cn('inline-flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white', className)}>
      {icon}{label}
    </span>
  )
}

// ─── Delivery Card ─────────────────────────────────────────────────────────────

function DeliveryCard({
  delivery, selected, onSelect, onGeocode,
}: {
  delivery: DeliveryViewDto
  selected: boolean
  onSelect: (id: number) => void
  onGeocode: (id: number) => void
}) {
  const navigate = useNavigate()
  const issue = hasIssue(delivery)

  return (
    <div
      className={cn(
        'group relative cursor-pointer rounded-xl border bg-card p-4 shadow-sm transition-all duration-300 hover:-translate-y-1 hover:shadow-lg',
        selected && 'ring-2 ring-primary',
        issue && 'border-amber-200 bg-amber-50/40 dark:border-amber-500/30 dark:bg-amber-500/5',
      )}
      onClick={() => navigate(`/deliveries/${delivery.id}`)}
    >
      <div className="absolute left-3 top-3" onClick={e => { e.stopPropagation(); onSelect(delivery.id) }}>
        <Checkbox checked={selected} />
      </div>

      <div className="mb-3 flex items-start justify-between pl-7">
        <div>
          <p className="text-xs text-muted-foreground">#{delivery.sequentialNumber}</p>
          <p className="font-medium">{delivery.reference}</p>
        </div>
        <div className="flex items-center gap-1.5">
          <WarningIcons delivery={delivery} onGeocode={onGeocode} />
          <DeliveryStatusBadge status={delivery.status} />
        </div>
      </div>

      <p className="mb-0.5 font-semibold">{delivery.clientName}</p>
      <p className="mb-3 text-sm text-muted-foreground">{delivery.city}</p>

      <div className="mb-3 flex flex-wrap gap-1.5">
        <Badge variant="secondary" className="text-xs">
          <Truck className="mr-1 h-3 w-3" />{delivery.storeName}
        </Badge>
        {delivery.type === DeliveryType.Urgent && (
          <Badge variant="outline" className={cn('text-xs', URGENT_BADGE_CLASS)}>Urgente</Badge>
        )}
        {delivery.withAssembly && (
          <Badge variant="outline" className="text-xs">Montage</Badge>
        )}
      </div>

      <div className="flex items-center justify-between text-sm">
        <div className="flex items-center gap-2 text-muted-foreground">
          {delivery.scheduledDate && (
            <span className="flex items-center gap-1">
              <CalendarDays className="h-3.5 w-3.5" />{formatDate(delivery.scheduledDate)}
            </span>
          )}
          {delivery.totalPackages > 0 && (
            <span className="flex items-center gap-1">
              <Package className="h-3.5 w-3.5" />{delivery.totalPackages}
            </span>
          )}
        </div>
        <span className="font-semibold text-green-600 dark:text-green-400">{formatPrice(delivery.price)}</span>
      </div>
    </div>
  )
}

// ─── Selection banner ──────────────────────────────────────────────────────────

function SelectionBanner({
  count, onClear, onBulkStatus, onBulkDelete, onBulkGeocode,
}: {
  count: number
  onClear: () => void
  onBulkStatus: (status: DeliveryStatus) => void
  onBulkDelete: () => void
  onBulkGeocode: () => void
}) {
  const statusOptions = [
    DeliveryStatus.ToBePlanned, DeliveryStatus.Confirmed,
    DeliveryStatus.InProgress, DeliveryStatus.Delivered, DeliveryStatus.Canceled,
  ]
  return (
    <div className="flex items-center gap-3 rounded-xl border bg-primary/5 px-4 py-2.5">
      <span className="text-sm font-medium">{count} sélectionnée{count > 1 ? 's' : ''}</span>
      <div className="flex flex-1 items-center gap-2">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" size="sm">
              <RefreshCw className="mr-1.5 h-3.5 w-3.5" />Changer le statut
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start">
            {statusOptions.map(s => (
              <DropdownMenuItem key={s} onClick={() => onBulkStatus(s)}>
                <span className={cn('mr-2 h-2 w-2 rounded-full', STATUS_COLORS[s].split(' ')[0])} />
                {STATUS_LABELS[s]}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
        <Button variant="outline" size="sm" onClick={onBulkGeocode}>
          <Locate className="mr-1.5 h-3.5 w-3.5" />Géocoder
        </Button>
        <Button variant="outline" size="sm" className="text-destructive hover:text-destructive" onClick={onBulkDelete}>
          <Trash2 className="mr-1.5 h-3.5 w-3.5" />Supprimer
        </Button>
      </div>
      <Button variant="ghost" size="sm" onClick={onClear}><X className="h-4 w-4" /></Button>
    </div>
  )
}

// ─── Main page ─────────────────────────────────────────────────────────────────

export default function DeliveriesPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [view, setView] = useState<'cards' | 'list'>('list')
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<DeliveryStatusFilter>('active')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)

  useEffect(() => {
    const t = setTimeout(() => setDebouncedSearch(search), 400)
    return () => clearTimeout(t)
  }, [search])

  useEffect(() => { setPage(1) }, [debouncedSearch, statusFilter, pageSize])

  // Real-time: subscribe to cross-tab events
  useEffect(() => {
    return deliveryEventBus.subscribe(() => {
      queryClient.invalidateQueries({ queryKey: deliveryKeys.lists() })
    })
  }, [queryClient])

  const filters: DeliveryFilterDto = {
    page,
    pageSize,
    globalSearch: debouncedSearch || undefined,
    statuses:
      statusFilter === 'active' || statusFilter === 'issues' ? DEFAULT_STATUSES
      : statusFilter === 'all' ? undefined
      : [statusFilter as DeliveryStatus],
    withIssues: statusFilter === 'issues' ? true : undefined,
    sortBy: 'SequentialNumber',
    sortDescending: true,
  }

  const { data, isLoading } = useQuery({
    queryKey: deliveryKeys.list(filters),
    queryFn: () => deliveriesApi.getList(filters),
    staleTime: 30_000,
    refetchInterval: 60_000,
  })

  const allDeliveries = data?.items ?? []
  const deliveries = allDeliveries
  const totalPages = data?.totalPages ?? 1

  // ─── Selection ────────────────────────────────────────────────────────────────

  const toggleSelect = useCallback((id: number) => {
    setSelected(prev => {
      const next = new Set(prev)
      if (next.has(id)) { next.delete(id) } else { next.add(id) }
      return next
    })
  }, [])

  const toggleSelectAll = useCallback(() => {
    setSelected(selected.size === deliveries.length ? new Set() : new Set(deliveries.map(d => d.id)))
  }, [selected.size, deliveries])

  // ─── Mutations ────────────────────────────────────────────────────────────────

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: deliveryKeys.lists() })
    deliveryEventBus.publish({ type: 'bulk' })
  }

  const bulkStatusMutation = useMutation({
    mutationFn: (status: DeliveryStatus) =>
      deliveriesApi.bulkUpdateStatus({ deliveryIds: [...selected], status }),
    onSuccess: () => { toast.success('Statut mis à jour'); setSelected(new Set()); invalidate() },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur') : 'Erreur'),
  })

  const bulkDeleteMutation = useMutation({
    mutationFn: () => deliveriesApi.bulkDelete({ deliveryIds: [...selected] }),
    onSuccess: () => { toast.success('Livraisons supprimées'); setSelected(new Set()); invalidate() },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur') : 'Erreur'),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deliveriesApi.delete(id),
    onSuccess: (_, id) => {
      toast.success('Livraison supprimée')
      deliveryEventBus.publish({ type: 'deleted', id })
      invalidate()
    },
    onError: () => toast.error('Impossible de supprimer cette livraison'),
  })

  const geocodeMutation = useMutation({
    mutationFn: (id: number) => deliveriesApi.geocode(id),
    onSuccess: (_, id) => {
      toast.success('Adresse géolocalisée')
      deliveryEventBus.publish({ type: 'updated', id })
      queryClient.invalidateQueries({ queryKey: deliveryKeys.lists() })
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Échec de la géolocalisation') : 'Erreur'),
  })

  function handleBulkGeocode() {
    const ids = [...selected]
    ids.forEach(id => geocodeMutation.mutate(id))
    setSelected(new Set())
  }

  const issueCount = allDeliveries.filter(hasIssue).length

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Hero */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 p-6 shadow-lg">
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }} />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="mb-1 flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
                <Package className="h-5 w-5 text-white" />
              </div>
              <h1 className="text-2xl font-bold tracking-tight text-white">Livraisons</h1>
            </div>
            <p className="text-sm text-sky-200">Suivez et gérez vos livraisons</p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            {!isLoading && (
              <StatChip icon={<Package className="h-3.5 w-3.5" />} label={`${data?.totalCount ?? 0} livraison${(data?.totalCount ?? 0) > 1 ? 's' : ''}`} />
            )}
            {!isLoading && issueCount > 0 && (
              <button
                onClick={() => setStatusFilter('issues')}
                className="inline-flex items-center gap-1.5 rounded-xl bg-amber-400/25 px-3 py-1.5 text-xs font-semibold text-amber-100 ring-1 ring-amber-300/40 transition-colors hover:bg-amber-400/35"
              >
                <AlertTriangle className="h-3.5 w-3.5" />{issueCount} problème{issueCount > 1 ? 's' : ''}
              </button>
            )}
            <Link
              to="/deliveries/new"
              className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white backdrop-blur-sm transition-colors hover:bg-white/25"
            >
              <Plus className="h-3.5 w-3.5" />Nouvelle livraison
            </Link>
          </div>
        </div>
      </div>

      {/* Status tabs */}
      <div className="flex items-center gap-1 overflow-x-auto rounded-xl border bg-muted p-1">
        {STATUS_TABS.map(tab => (
          <button
            key={String(tab.value)}
            onClick={() => setStatusFilter(tab.value)}
            aria-pressed={statusFilter === tab.value}
            className={cn(
              'shrink-0 rounded-lg px-3 py-1.5 text-sm font-medium transition-colors',
              statusFilter === tab.value ? 'bg-card text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-48">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Référence, client, ville…"
            value={search}
            onChange={e => setSearch(e.target.value)}
            className="pl-9 pr-9"
          />
          {search && (
            <button onClick={() => setSearch('')} aria-label="Effacer la recherche" className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground">
              <X className="h-4 w-4" />
            </button>
          )}
        </div>

        <div className="flex rounded-md border" role="group" aria-label="Mode d'affichage">
          <button
            onClick={() => setView('cards')}
            aria-label="Vue en cartes"
            aria-pressed={view === 'cards'}
            className={cn('flex items-center gap-1.5 px-3 py-1.5 text-sm transition-colors', view === 'cards' ? 'bg-primary text-primary-foreground' : 'hover:bg-muted')}
          >
            <LayoutGrid className="h-4 w-4" />
          </button>
          <button
            onClick={() => setView('list')}
            aria-label="Vue en liste"
            aria-pressed={view === 'list'}
            className={cn('flex items-center gap-1.5 px-3 py-1.5 text-sm transition-colors', view === 'list' ? 'bg-primary text-primary-foreground' : 'hover:bg-muted')}
          >
            <List className="h-4 w-4" />
          </button>
        </div>
      </div>

      {/* Selection banner */}
      {selected.size > 0 && (
        <SelectionBanner
          count={selected.size}
          onClear={() => setSelected(new Set())}
          onBulkStatus={s => bulkStatusMutation.mutate(s)}
          onBulkDelete={() => bulkDeleteMutation.mutate()}
          onBulkGeocode={handleBulkGeocode}
        />
      )}

      {/* Content */}
      {isLoading ? (
        <div className={cn(view === 'cards' ? 'grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4' : 'space-y-2')}>
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton key={i} className={view === 'cards' ? 'h-44 rounded-xl' : 'h-12'} />
          ))}
        </div>
      ) : deliveries.length === 0 ? (
        <div className="flex h-64 flex-col items-center justify-center gap-2 text-muted-foreground">
          <Package className="h-10 w-10 opacity-30" />
          <p className="font-medium">
            {statusFilter === 'issues' ? 'Aucun problème détecté ✓' : 'Aucune livraison trouvée'}
          </p>
          {search && <Button variant="ghost" size="sm" onClick={() => setSearch('')}>Effacer la recherche</Button>}
        </div>
      ) : view === 'cards' ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {deliveries.map(d => (
            <DeliveryCard
              key={d.id}
              delivery={d}
              selected={selected.has(d.id)}
              onSelect={toggleSelect}
              onGeocode={id => geocodeMutation.mutate(id)}
            />
          ))}
        </div>
      ) : (
        <div className="overflow-hidden rounded-2xl border bg-card shadow-sm">
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent">
                <TableHead className="w-8 py-2 pl-3 pr-0">
                  <Checkbox
                    checked={selected.size === deliveries.length && deliveries.length > 0}
                    onCheckedChange={toggleSelectAll}
                  />
                </TableHead>
                <TableHead className="w-6 py-2 px-1" />
                <TableHead className="py-2 text-xs">#</TableHead>
                <TableHead className="py-2 text-xs">Référence / Client</TableHead>
                <TableHead className="py-2 text-xs">Ville</TableHead>
                <TableHead className="py-2 text-xs">Dépôt</TableHead>
                <TableHead className="py-2 text-xs">Colis</TableHead>
                <TableHead className="py-2 text-xs">Dates</TableHead>
                <TableHead className="py-2 text-xs">Prix</TableHead>
                <TableHead className="py-2 text-xs">Statut</TableHead>
                <TableHead className="w-20 py-2" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {deliveries.map(d => (
                <TableRow
                  key={d.id}
                  className={cn(
                    'cursor-pointer',
                    hasIssue(d)
                      ? 'bg-amber-50/50 hover:bg-amber-50 dark:bg-amber-500/5 dark:hover:bg-amber-500/10'
                      : 'hover:bg-sky-50/40 dark:hover:bg-sky-500/5',
                  )}
                  onClick={() => navigate(`/deliveries/${d.id}`)}
                >
                  <TableCell className="py-1.5 pl-3 pr-0" onClick={e => e.stopPropagation()}>
                    <Checkbox checked={selected.has(d.id)} onCheckedChange={() => toggleSelect(d.id)} />
                  </TableCell>
                  <TableCell className="py-1.5 px-1" onClick={e => e.stopPropagation()}>
                    <WarningIcons delivery={d} onGeocode={id => geocodeMutation.mutate(id)} />
                  </TableCell>
                  <TableCell className="py-1.5 text-xs text-muted-foreground">{d.sequentialNumber}</TableCell>
                  <TableCell className="py-1.5">
                    <p className="text-sm font-medium leading-tight">{d.reference}</p>
                    <p className="text-xs text-muted-foreground">{d.clientName}</p>
                  </TableCell>
                  <TableCell className="py-1.5 text-sm">{d.city}</TableCell>
                  <TableCell className="py-1.5 text-sm">{d.storeName}</TableCell>
                  <TableCell className="py-1.5 text-sm">{d.totalPackages || '—'}</TableCell>
                  <TableCell className="py-1.5">
                    {d.scheduledDate
                      ? <p className="text-xs font-medium">{formatDate(d.scheduledDate)}</p>
                      : <p className="text-xs text-muted-foreground">—</p>}
                    <p className="text-xs text-muted-foreground">{formatDate(d.createdDate)}</p>
                  </TableCell>
                  <TableCell className="py-1.5 text-sm font-medium text-green-600 dark:text-green-400">{formatPrice(d.price)}</TableCell>
                  <TableCell className="py-1.5"><DeliveryStatusBadge status={d.status} /></TableCell>
                  <TableCell className="py-1.5" onClick={e => e.stopPropagation()}>
                    <div className="flex items-center gap-0.5">
                      <Button variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground hover:text-foreground"
                        onClick={() => navigate(`/deliveries/${d.id}`)}>
                        <Eye className="h-3.5 w-3.5" />
                      </Button>
                      {d.status !== DeliveryStatus.Delivered && d.status !== DeliveryStatus.Canceled && (
                        <>
                          <Button variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground hover:text-foreground"
                            onClick={() => navigate(`/deliveries/${d.id}/edit`)}>
                            <Pencil className="h-3.5 w-3.5" />
                          </Button>
                          <Button variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground hover:text-destructive"
                            onClick={() => deleteMutation.mutate(d.id)}>
                            <Trash2 className="h-3.5 w-3.5" />
                          </Button>
                        </>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Pagination */}
      {!isLoading && totalPages > 1 && (
        <div className="flex items-center justify-between border-t pt-4">
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">Lignes par page :</span>
            <Select value={String(pageSize)} onValueChange={v => setPageSize(Number(v))}>
              <SelectTrigger className="h-8 w-16"><SelectValue /></SelectTrigger>
              <SelectContent>
                {PAGE_SIZE_OPTIONS.map(n => <SelectItem key={n} value={String(n)}>{n}</SelectItem>)}
              </SelectContent>
            </Select>
          </div>
          <div className="flex items-center gap-1">
            <span className="mr-2 text-sm text-muted-foreground">Page {page} / {totalPages}</span>
            <Button variant="outline" size="icon" className="h-8 w-8" disabled={page === 1} onClick={() => setPage(p => p - 1)}>
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button variant="outline" size="icon" className="h-8 w-8" disabled={page === totalPages} onClick={() => setPage(p => p + 1)}>
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
