import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import {
  Route, Plus, Search, X, RefreshCw, Eye, Trash2,
  ChevronLeft, ChevronRight, AlertTriangle, Truck,
  Users, Navigation, Clock, Calendar, Download, Pencil, CheckCircle, UserCircle,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { cn } from '@/lib/utils'
import {
  routesApi, routeKeys, RouteStatus,
  ROUTE_STATUS_LABELS, ROUTE_STATUS_COLORS,
} from '@/api/routes'
import type { RouteFilterDto, RouteViewDto } from '@/api/routes'
import { useWizardStore } from '@/store/wizardStore'

const PAGE_SIZE = 20

const STATUS_TABS: Array<{ label: string; value: RouteStatus | undefined }> = [
  { label: 'Toutes', value: undefined },
  { label: 'Brouillon', value: RouteStatus.Draft },
  { label: 'Confirmée', value: RouteStatus.Confirmed },
  { label: 'En cours', value: RouteStatus.InProgress },
  { label: 'Terminée', value: RouteStatus.Completed },
  { label: 'Annulée', value: RouteStatus.Cancelled },
]

// ─── StatChip ─────────────────────────────────────────────────────────────────

function StatChip({ icon, label }: { icon: React.ReactNode; label: string }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white">
      {icon}{label}
    </span>
  )
}

// ─── StatusBadge ──────────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: RouteStatus }) {
  return (
    <span className={cn('inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium', ROUTE_STATUS_COLORS[status])}>
      {ROUTE_STATUS_LABELS[status]}
    </span>
  )
}

// ─── DeleteConfirmModal ───────────────────────────────────────────────────────

function DeleteConfirmModal({ route, onConfirm, onCancel, isPending }: {
  route: RouteViewDto; onConfirm: () => void; onCancel: () => void; isPending: boolean
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-white shadow-2xl">
        <div className="p-6">
          <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100">
            <Trash2 className="h-6 w-6 text-red-600" />
          </div>
          <h3 className="mb-1 text-base font-semibold text-slate-800">Supprimer cette tournée ?</h3>
          <p className="mb-3 text-sm text-slate-500">
            La tournée <strong>{route.reference}</strong> sera définitivement supprimée.
          </p>
          <ul className="space-y-1 rounded-xl bg-red-50 px-4 py-3 text-xs text-red-700">
            <li>• Les livraisons assignées seront libérées</li>
            <li>• Les chauffeurs et aide-livreurs seront détachés</li>
            <li>• Le véhicule sera libéré</li>
            <li className="font-semibold">• Cette action est irréversible</li>
          </ul>
        </div>
        <div className="flex gap-3 border-t px-6 py-4">
          <Button variant="outline" className="flex-1" onClick={onCancel}>Annuler</Button>
          <Button variant="destructive" className="flex-1" onClick={onConfirm} disabled={isPending}>
            {isPending
              ? <span className="flex items-center gap-2"><span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />Suppression…</span>
              : 'Supprimer définitivement'}
          </Button>
        </div>
      </div>
    </div>
  )
}

// ─── ConfirmRouteModal ────────────────────────────────────────────────────────

function ConfirmRouteModal({ route, onConfirm, onCancel, isPending }: {
  route: RouteViewDto; onConfirm: () => void; onCancel: () => void; isPending: boolean
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-white shadow-2xl">
        <div className="p-6">
          <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-blue-100">
            <CheckCircle className="h-6 w-6 text-blue-600" />
          </div>
          <h3 className="mb-1 text-base font-semibold text-slate-800">Confirmer cette tournée ?</h3>
          <p className="text-sm text-slate-500">
            La tournée <strong>{route.reference}</strong> sera confirmée et ne pourra plus être modifiée.
          </p>
        </div>
        <div className="flex gap-3 border-t px-6 py-4">
          <Button variant="outline" className="flex-1" onClick={onCancel}>Annuler</Button>
          <Button className="flex-1 bg-blue-600 hover:bg-blue-700" onClick={onConfirm} disabled={isPending}>
            {isPending
              ? <span className="flex items-center gap-2"><span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />Confirmation…</span>
              : 'Confirmer'}
          </Button>
        </div>
      </div>
    </div>
  )
}

// ─── CancelConfirmModal ────────────────────────────────────────────────────────

function CancelConfirmModal({ route, onConfirm, onCancel, isPending }: {
  route: RouteViewDto; onConfirm: () => void; onCancel: () => void; isPending: boolean
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-white shadow-2xl">
        <div className="p-6">
          <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100">
            <AlertTriangle className="h-6 w-6 text-red-600" />
          </div>
          <h3 className="mb-1 text-base font-semibold text-slate-800">Annuler cette tournée ?</h3>
          <p className="text-sm text-slate-500">
            La tournée <strong>{route.reference}</strong> sera annulée et toutes ses livraisons seront libérées.
          </p>
        </div>
        <div className="flex gap-3 border-t px-6 py-4">
          <Button variant="outline" className="flex-1" onClick={onCancel}>Fermer</Button>
          <Button variant="destructive" className="flex-1" onClick={onConfirm} disabled={isPending}>
            {isPending
              ? <span className="flex items-center gap-2"><span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />Annulation…</span>
              : 'Confirmer l\'annulation'}
          </Button>
        </div>
      </div>
    </div>
  )
}

// ─── RoutesPage ───────────────────────────────────────────────────────────────

export default function RoutesPage() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const resetWizard = useWizardStore(s => s.reset)

  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<RouteStatus | undefined>(undefined)
  const [cancelTarget, setCancelTarget] = useState<RouteViewDto | null>(null)
  const [confirmTarget, setConfirmTarget] = useState<RouteViewDto | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<RouteViewDto | null>(null)
  const [downloadingId, setDownloadingId] = useState<number | null>(null)

  useEffect(() => {
    const t = setTimeout(() => { setDebouncedSearch(searchInput); setPage(1) }, 300)
    return () => clearTimeout(t)
  }, [searchInput])

  const filters: RouteFilterDto = {
    page, pageSize: PAGE_SIZE,
    searchTerm: debouncedSearch || undefined,
    status: statusFilter,
  }

  const { data, isLoading, refetch } = useQuery({
    queryKey: routeKeys.list(filters),
    queryFn: () => routesApi.getList(filters),
    placeholderData: prev => prev,
  })

  const deleteMutation = useMutation({
    mutationFn: (id: number) => routesApi.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: routeKeys.lists() })
      toast.success('Tournée supprimée')
      setDeleteTarget(null)
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur lors de la suppression') : 'Erreur'),
  })

  const confirmMutation = useMutation({
    mutationFn: (id: number) => routesApi.confirm(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: routeKeys.lists() })
      toast.success('Tournée confirmée')
      setConfirmTarget(null)
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur de confirmation') : 'Erreur'),
  })

  const cancelMutation = useMutation({
    mutationFn: (id: number) => routesApi.cancel(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: routeKeys.lists() })
      toast.success('Tournée annulée')
      setCancelTarget(null)
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur d\'annulation') : 'Erreur'),
  })

  const routes = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1
  const from = Math.min((page - 1) * PAGE_SIZE + 1, totalCount)
  const to = Math.min(page * PAGE_SIZE, totalCount)

  function handleNewRoute() {
    resetWizard()
    navigate('/routes/new')
  }

  async function handleDownload(e: React.MouseEvent, route: RouteViewDto) {
    e.stopPropagation()
    setDownloadingId(route.id)
    try {
      await routesApi.downloadSheet(route.id, route.reference)
    } catch {
      toast.error('Erreur lors du téléchargement de la feuille de route')
    } finally {
      setDownloadingId(null)
    }
  }

  function formatDistance(km?: number) {
    if (!km) return '—'
    return `${km.toFixed(1)} km`
  }

  function formatDuration(min?: number) {
    if (!min) return '—'
    const h = Math.floor(min / 60)
    const m = min % 60
    return h > 0 ? `${h}h${m > 0 ? String(m).padStart(2, '0') : ''}` : `${m} min`
  }

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Hero */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 p-6 shadow-lg">
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }} />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="mb-1 flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
                <Route className="h-5 w-5 text-white" />
              </div>
              <h1 className="text-2xl font-bold tracking-tight text-white">Tournées</h1>
            </div>
            <p className="text-sm text-sky-200">Planifiez et suivez vos tournées de livraison</p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            {!isLoading && (
              <StatChip icon={<Route className="h-3.5 w-3.5" />} label={`${totalCount} tournée${totalCount > 1 ? 's' : ''}`} />
            )}
            <button
              onClick={handleNewRoute}
              className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white backdrop-blur-sm transition-colors hover:bg-white/25"
            >
              <Plus className="h-3.5 w-3.5" />Nouvelle tournée
            </button>
          </div>
        </div>
      </div>

      {/* Status tabs */}
      <div className="flex items-center gap-1 overflow-x-auto rounded-xl border bg-slate-50 p-1">
        {STATUS_TABS.map(tab => (
          <button
            key={String(tab.value)}
            onClick={() => { setStatusFilter(tab.value); setPage(1) }}
            className={cn(
              'shrink-0 rounded-lg px-3 py-1.5 text-sm font-medium transition-colors',
              statusFilter === tab.value
                ? 'bg-white text-slate-800 shadow-sm'
                : 'text-slate-500 hover:text-slate-700',
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Toolbar */}
      <div className="flex items-center gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <Input className="pl-9 pr-9" placeholder="Rechercher une référence, véhicule, chauffeur…" value={searchInput} onChange={e => setSearchInput(e.target.value)} />
          {searchInput && (
            <button onClick={() => setSearchInput('')} className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
              <X className="h-4 w-4" />
            </button>
          )}
        </div>
        <Button variant="outline" size="icon" onClick={() => refetch()} title="Actualiser"><RefreshCw className="h-4 w-4" /></Button>
      </div>

      {/* Table */}
      <div className="overflow-hidden rounded-2xl border bg-white shadow-sm">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead className="pl-6 text-xs font-semibold uppercase tracking-wider text-slate-400">Référence</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Date</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Véhicule</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Chauffeur</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Livraisons</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Distance</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Durée</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Statut</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Créée par</TableHead>
              <TableHead className="w-24 pr-6" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              [...Array(5)].map((_, i) => (
                <TableRow key={i}>
                  {[...Array(9)].map((_, j) => (
                    <TableCell key={j} className={j === 0 ? 'pl-6' : ''}><Skeleton className="h-4 w-24" /></TableCell>
                  ))}
                </TableRow>
              ))
            ) : routes.length === 0 ? (
              <TableRow>
                <TableCell colSpan={10} className="py-16 text-center">
                  <div className="flex flex-col items-center gap-3">
                    <div className="flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
                      <Route className="h-6 w-6" />
                    </div>
                    <p className="text-sm font-medium text-slate-500">
                      {debouncedSearch || statusFilter !== undefined ? 'Aucune tournée ne correspond à vos filtres' : 'Aucune tournée'}
                    </p>
                    {!debouncedSearch && statusFilter === undefined && (
                      <Button size="sm" onClick={handleNewRoute}>
                        <Plus className="mr-1.5 h-3.5 w-3.5" />Créer la première tournée
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              routes.map(route => (
                <TableRow
                  key={route.id}
                  className="cursor-pointer hover:bg-sky-50/40"
                  onClick={() => navigate(`/routes/${route.id}`)}
                >
                  <TableCell className="py-3 pl-6">
                    <div className="flex items-center gap-2">
                      <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-sky-100">
                        <Route className="h-4 w-4 text-sky-600" />
                      </div>
                      <span className="font-mono text-sm font-semibold text-slate-800">{route.reference}</span>
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1.5 text-sm text-slate-600">
                      <Calendar className="h-3.5 w-3.5 text-slate-400" />
                      {new Date(route.date).toLocaleDateString('fr-FR')}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1.5 text-sm text-slate-600">
                      <Truck className="h-3.5 w-3.5 text-slate-400" />
                      {route.vehicleName}
                    </div>
                  </TableCell>
                  <TableCell>
                    {route.mainDriverName ? (
                      <div className="flex items-center gap-1.5 text-sm text-slate-600">
                        <Users className="h-3.5 w-3.5 text-slate-400" />
                        {route.mainDriverName}
                        {route.teamCount > 1 && (
                          <span className="text-xs text-slate-400">+{route.teamCount - 1}</span>
                        )}
                      </div>
                    ) : (
                      <span className="text-sm text-slate-400">—</span>
                    )}
                  </TableCell>
                  <TableCell>
                    <span className="text-sm font-medium text-slate-700">{route.totalDeliveries}</span>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1 text-sm text-slate-600">
                      <Navigation className="h-3 w-3 text-slate-400" />
                      {formatDistance(route.totalDistance)}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1 text-sm text-slate-600">
                      <Clock className="h-3 w-3 text-slate-400" />
                      {formatDuration(route.totalDuration)}
                    </div>
                  </TableCell>
                  <TableCell><StatusBadge status={route.status} /></TableCell>
                  <TableCell>
                    {route.createdBy ? (
                      <div className="flex items-center gap-1.5 text-sm text-slate-600">
                        <UserCircle className="h-3.5 w-3.5 text-slate-400" />
                        {route.createdBy}
                      </div>
                    ) : (
                      <span className="text-sm text-slate-400">—</span>
                    )}
                  </TableCell>
                  <TableCell className="pr-6" onClick={e => e.stopPropagation()}>
                    <div className="flex items-center justify-end gap-1">
                      <button onClick={() => navigate(`/routes/${route.id}`)} className="rounded-lg p-1.5 text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-700" title="Voir le détail">
                        <Eye className="h-4 w-4" />
                      </button>
                      {route.status === RouteStatus.Draft && (
                        <button onClick={e => { e.stopPropagation(); navigate(`/routes/${route.id}/edit`) }} className="rounded-lg p-1.5 text-amber-500 transition-colors hover:bg-amber-50 hover:text-amber-600" title="Modifier">
                          <Pencil className="h-4 w-4" />
                        </button>
                      )}
                      {route.status === RouteStatus.Draft && (
                        <button
                          onClick={e => { e.stopPropagation(); setConfirmTarget(route) }}
                          disabled={confirmMutation.isPending}
                          className="rounded-lg p-1.5 text-blue-500 transition-colors hover:bg-blue-50 hover:text-blue-600"
                          title="Confirmer la tournée"
                        >
                          {confirmMutation.isPending
                            ? <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent inline-block" />
                            : <CheckCircle className="h-4 w-4" />}
                        </button>
                      )}
                      {(route.status === RouteStatus.Confirmed || route.status === RouteStatus.InProgress || route.status === RouteStatus.Completed) && (
                        <button
                          onClick={e => handleDownload(e, route)}
                          disabled={downloadingId === route.id}
                          className="rounded-lg p-1.5 text-sky-500 transition-colors hover:bg-sky-50 hover:text-sky-600"
                          title="Télécharger la feuille de route"
                        >
                          {downloadingId === route.id
                            ? <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent inline-block" />
                            : <Download className="h-4 w-4" />}
                        </button>
                      )}
                      {route.status === RouteStatus.Draft && (
                        <button onClick={e => { e.stopPropagation(); setDeleteTarget(route) }} className="rounded-lg p-1.5 text-red-400 transition-colors hover:bg-red-50 hover:text-red-500" title="Supprimer la tournée">
                          <Trash2 className="h-4 w-4" />
                        </button>
                      )}
                      {route.status === RouteStatus.Confirmed && (
                        <button onClick={e => { e.stopPropagation(); setCancelTarget(route) }} className="rounded-lg p-1.5 text-red-400 transition-colors hover:bg-red-50 hover:text-red-500" title="Annuler la tournée">
                          <Trash2 className="h-4 w-4" />
                        </button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {/* Pagination */}
      {totalCount > 0 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-slate-500">
            <strong>{from}</strong> à <strong>{to}</strong> sur <strong>{totalCount}</strong> tournée{totalCount > 1 ? 's' : ''}
          </p>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}>
              <ChevronLeft className="h-4 w-4" />Précédent
            </Button>
            <span className="rounded-lg border bg-white px-3 py-1.5 text-sm font-medium text-slate-700">{page} / {totalPages}</span>
            <Button variant="outline" size="sm" onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page >= totalPages}>
              Suivant<ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}

      {/* Delete modal */}
      {deleteTarget && (
        <DeleteConfirmModal
          route={deleteTarget}
          onConfirm={() => deleteMutation.mutate(deleteTarget.id)}
          onCancel={() => setDeleteTarget(null)}
          isPending={deleteMutation.isPending}
        />
      )}

      {/* Confirm modal */}
      {confirmTarget && (
        <ConfirmRouteModal
          route={confirmTarget}
          onConfirm={() => confirmMutation.mutate(confirmTarget.id)}
          onCancel={() => setConfirmTarget(null)}
          isPending={confirmMutation.isPending}
        />
      )}

      {/* Cancel modal */}
      {cancelTarget && (
        <CancelConfirmModal
          route={cancelTarget}
          onConfirm={() => cancelMutation.mutate(cancelTarget.id)}
          onCancel={() => setCancelTarget(null)}
          isPending={cancelMutation.isPending}
        />
      )}
    </div>
  )
}
