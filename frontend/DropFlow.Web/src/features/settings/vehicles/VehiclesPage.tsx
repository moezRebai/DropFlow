import { useState, useEffect, useCallback } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import {
  Truck, Plus, Search, X, RefreshCw, Pencil, Trash2,
  ChevronLeft, ChevronRight, AlertTriangle, CheckCircle2, XCircle,
  Package, Gauge,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { cn } from '@/lib/utils'
import { vehiclesApi, vehicleKeys } from '@/api/vehicles'
import type { VehicleDto, VehicleFilterDto } from '@/api/vehicles'

const PAGE_SIZE = 20

// ─── Schema ──────────────────────────────────────────────────────────────────

const schema = z.object({
  brand: z.string().min(1, 'Marque requise'),
  model: z.string().min(1, 'Modèle requis'),
  plateNumber: z.string().min(1, 'Immatriculation requise'),
  maxDeliveries: z.coerce.number().int().min(1, 'Au moins 1 livraison'),
  maxVolume: z.coerce.number().int().min(0, 'Volume positif requis'),
  isActive: z.boolean(),
})
type FormValues = z.infer<typeof schema>

// ─── Dialog state ─────────────────────────────────────────────────────────────

type DialogState =
  | { type: 'none' }
  | { type: 'create' }
  | { type: 'edit'; vehicle: VehicleDto }
  | { type: 'delete'; vehicle: VehicleDto }

// ─── StatChip ─────────────────────────────────────────────────────────────────

function StatChip({ icon, label }: { icon: React.ReactNode; label: string }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white">
      {icon}{label}
    </span>
  )
}

// ─── VehicleFormModal ─────────────────────────────────────────────────────────

function VehicleFormModal({ vehicle, onClose }: { vehicle?: VehicleDto; onClose: () => void }) {
  const qc = useQueryClient()
  const isEdit = !!vehicle

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: vehicle
      ? { ...vehicle }
      : { brand: '', model: '', plateNumber: '', maxDeliveries: 20, maxVolume: 0, isActive: true },
  })

  const createMutation = useMutation({
    mutationFn: vehiclesApi.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: vehicleKeys.lists() }); toast.success('Véhicule créé'); onClose() },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur de création') : 'Erreur'),
  })

  const updateMutation = useMutation({
    mutationFn: (data: FormValues) => vehiclesApi.update(vehicle!.id, data),
    onSuccess: () => { qc.invalidateQueries({ queryKey: vehicleKeys.lists() }); toast.success('Véhicule mis à jour'); onClose() },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur de mise à jour') : 'Erreur'),
  })

  function onSubmit(values: FormValues) {
    if (isEdit) {
      updateMutation.mutate(values)
    } else {
      const { isActive: _ia, ...createData } = values
      createMutation.mutate(createData)
    }
  }

  const isPending = createMutation.isPending || updateMutation.isPending

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
      <div className="relative z-10 w-full max-w-md overflow-hidden rounded-2xl bg-white shadow-2xl">
        <div className="bg-gradient-to-br from-sky-500 to-blue-600 px-6 py-5">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
              <Truck className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="font-bold text-white">{isEdit ? 'Modifier le véhicule' : 'Nouveau véhicule'}</h2>
              <p className="text-xs text-sky-200">
                {isEdit ? `${vehicle.brand} ${vehicle.model}` : 'Renseignez les informations du véhicule'}
              </p>
            </div>
          </div>
        </div>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 p-6">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label htmlFor="brand">Marque</Label>
              <Input id="brand" {...form.register('brand')} placeholder="Renault" />
              {form.formState.errors.brand && <p className="text-xs text-red-500">{form.formState.errors.brand.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="model">Modèle</Label>
              <Input id="model" {...form.register('model')} placeholder="Master" />
              {form.formState.errors.model && <p className="text-xs text-red-500">{form.formState.errors.model.message}</p>}
            </div>
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="plateNumber">Immatriculation</Label>
            <Input id="plateNumber" {...form.register('plateNumber')} placeholder="AA-123-BB" className="uppercase" />
            {form.formState.errors.plateNumber && <p className="text-xs text-red-500">{form.formState.errors.plateNumber.message}</p>}
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label htmlFor="maxDeliveries">Capacité (livraisons)</Label>
              <Input id="maxDeliveries" type="number" min={1} {...form.register('maxDeliveries')} />
              {form.formState.errors.maxDeliveries && <p className="text-xs text-red-500">{form.formState.errors.maxDeliveries.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="maxVolume">Volume max (m³)</Label>
              <Input id="maxVolume" type="number" min={0} {...form.register('maxVolume')} />
              {form.formState.errors.maxVolume && <p className="text-xs text-red-500">{form.formState.errors.maxVolume.message}</p>}
            </div>
          </div>
          {isEdit && (
            <div className="flex items-center gap-2">
              <input type="checkbox" id="isActive" {...form.register('isActive')}
                className="h-4 w-4 rounded border-gray-300 text-sky-600 focus:ring-sky-500" />
              <Label htmlFor="isActive" className="cursor-pointer">Véhicule actif</Label>
            </div>
          )}
          <div className="flex gap-3 pt-2">
            <Button type="button" variant="outline" className="flex-1" onClick={onClose}>Annuler</Button>
            <Button type="submit" className="flex-1" disabled={isPending}>
              {isPending
                ? <span className="flex items-center gap-2"><span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />{isEdit ? 'Mise à jour…' : 'Création…'}</span>
                : isEdit ? 'Enregistrer' : 'Créer'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ─── DeleteConfirmModal ────────────────────────────────────────────────────────

function DeleteConfirmModal({ vehicle, onConfirm, onCancel, isPending }: {
  vehicle: VehicleDto; onConfirm: () => void; onCancel: () => void; isPending: boolean
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-white shadow-2xl">
        <div className="p-6">
          <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100">
            <AlertTriangle className="h-6 w-6 text-red-600" />
          </div>
          <h3 className="mb-1 text-base font-semibold text-slate-800">Supprimer ce véhicule ?</h3>
          <p className="text-sm text-slate-500">
            <strong>{vehicle.brand} {vehicle.model} ({vehicle.plateNumber})</strong> sera définitivement supprimé.
          </p>
        </div>
        <div className="flex gap-3 border-t px-6 py-4">
          <Button variant="outline" className="flex-1" onClick={onCancel}>Annuler</Button>
          <Button variant="destructive" className="flex-1" onClick={onConfirm} disabled={isPending}>
            {isPending
              ? <span className="flex items-center gap-2"><span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />Suppression…</span>
              : 'Supprimer'}
          </Button>
        </div>
      </div>
    </div>
  )
}

// ─── VehiclesPage ─────────────────────────────────────────────────────────────

export default function VehiclesPage() {
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [dialog, setDialog] = useState<DialogState>({ type: 'none' })

  useEffect(() => {
    const t = setTimeout(() => { setDebouncedSearch(searchInput); setPage(1) }, 300)
    return () => clearTimeout(t)
  }, [searchInput])

  const filters: VehicleFilterDto = { page, pageSize: PAGE_SIZE, searchTerm: debouncedSearch || undefined }

  const { data, isLoading, refetch } = useQuery({
    queryKey: vehicleKeys.list(filters),
    queryFn: () => vehiclesApi.getList(filters),
    placeholderData: prev => prev,
  })

  const deleteMutation = useMutation({
    mutationFn: (id: number) => vehiclesApi.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: vehicleKeys.lists() })
      toast.success('Véhicule supprimé')
      setDialog({ type: 'none' })
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Suppression impossible') : 'Erreur'),
  })

  const closeDialog = useCallback(() => setDialog({ type: 'none' }), [])

  const vehicles = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1
  const from = Math.min((page - 1) * PAGE_SIZE + 1, totalCount)
  const to = Math.min(page * PAGE_SIZE, totalCount)
  const activeCount = vehicles.filter(v => v.isActive).length

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Hero */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 p-6 shadow-lg">
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }} />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="mb-1 flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
                <Truck className="h-5 w-5 text-white" />
              </div>
              <h1 className="text-2xl font-bold tracking-tight text-white">Véhicules</h1>
            </div>
            <p className="text-sm text-sky-200">Gérez la flotte de véhicules</p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            {!isLoading && (
              <>
                <StatChip icon={<Truck className="h-3.5 w-3.5" />} label={`${totalCount} véhicule${totalCount > 1 ? 's' : ''}`} />
                <StatChip icon={<CheckCircle2 className="h-3.5 w-3.5" />} label={`${activeCount} actif${activeCount > 1 ? 's' : ''}`} />
              </>
            )}
            <button
              onClick={() => setDialog({ type: 'create' })}
              className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white backdrop-blur-sm transition-colors hover:bg-white/25"
            >
              <Plus className="h-3.5 w-3.5" />Nouveau véhicule
            </button>
          </div>
        </div>
      </div>

      {/* Toolbar */}
      <div className="flex items-center gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <Input className="pl-9 pr-9" placeholder="Rechercher marque, modèle, immatriculation…" value={searchInput} onChange={e => setSearchInput(e.target.value)} />
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
              <TableHead className="pl-6 text-xs font-semibold uppercase tracking-wider text-slate-400">Véhicule</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Immatriculation</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Capacité</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Volume</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Statut</TableHead>
              <TableHead className="w-20 pr-6" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              [...Array(5)].map((_, i) => (
                <TableRow key={i}>
                  <TableCell className="pl-6"><Skeleton className="h-10 w-40" /></TableCell>
                  <TableCell><Skeleton className="h-5 w-24 rounded-lg" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-16" /></TableCell>
                  <TableCell><Skeleton className="h-6 w-16 rounded-full" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-12" /></TableCell>
                </TableRow>
              ))
            ) : vehicles.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="py-16 text-center">
                  <div className="flex flex-col items-center gap-3">
                    <div className="flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
                      <Truck className="h-6 w-6" />
                    </div>
                    <p className="text-sm font-medium text-slate-500">
                      {debouncedSearch ? 'Aucun véhicule ne correspond à votre recherche' : 'Aucun véhicule'}
                    </p>
                    {!debouncedSearch && (
                      <Button size="sm" onClick={() => setDialog({ type: 'create' })}>
                        <Plus className="mr-1.5 h-3.5 w-3.5" />Créer le premier véhicule
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              vehicles.map(vehicle => (
                <TableRow key={vehicle.id} className="hover:bg-sky-50/40">
                  <TableCell className="py-3 pl-6">
                    <div className="flex items-center gap-3">
                      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-slate-100">
                        <Truck className="h-5 w-5 text-slate-500" />
                      </div>
                      <p className="font-semibold text-slate-800">{vehicle.brand} {vehicle.model}</p>
                    </div>
                  </TableCell>
                  <TableCell>
                    <span className="rounded-lg bg-slate-100 px-2.5 py-1 font-mono text-sm font-medium text-slate-700">
                      {vehicle.plateNumber}
                    </span>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1.5 text-sm text-slate-600">
                      <Package className="h-3.5 w-3.5 text-slate-400" />
                      {vehicle.maxDeliveries} livraison{vehicle.maxDeliveries > 1 ? 's' : ''}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1.5 text-sm text-slate-600">
                      <Gauge className="h-3.5 w-3.5 text-slate-400" />
                      {vehicle.maxVolume} m³
                    </div>
                  </TableCell>
                  <TableCell>
                    <span className={cn(
                      'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium',
                      vehicle.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500',
                    )}>
                      {vehicle.isActive ? <CheckCircle2 className="h-3 w-3" /> : <XCircle className="h-3 w-3" />}
                      {vehicle.isActive ? 'Actif' : 'Inactif'}
                    </span>
                  </TableCell>
                  <TableCell className="pr-6" onClick={e => e.stopPropagation()}>
                    <div className="flex items-center justify-end gap-1">
                      <button onClick={() => setDialog({ type: 'edit', vehicle })} className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600" title="Modifier">
                        <Pencil className="h-4 w-4" />
                      </button>
                      <button onClick={() => setDialog({ type: 'delete', vehicle })} className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-red-50 hover:text-red-500" title="Supprimer">
                        <Trash2 className="h-4 w-4" />
                      </button>
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
            <strong>{from}</strong> à <strong>{to}</strong> sur <strong>{totalCount}</strong> véhicule{totalCount > 1 ? 's' : ''}
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

      {/* Modals */}
      {(dialog.type === 'create' || dialog.type === 'edit') && (
        <VehicleFormModal vehicle={dialog.type === 'edit' ? dialog.vehicle : undefined} onClose={closeDialog} />
      )}
      {dialog.type === 'delete' && (
        <DeleteConfirmModal
          vehicle={dialog.vehicle}
          onConfirm={() => deleteMutation.mutate(dialog.vehicle.id)}
          onCancel={closeDialog}
          isPending={deleteMutation.isPending}
        />
      )}
    </div>
  )
}
