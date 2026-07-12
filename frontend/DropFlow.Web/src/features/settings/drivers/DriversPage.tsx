import { useState, useEffect, useCallback } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import {
  UserCheck, Plus, Search, X, RefreshCw, Pencil, Trash2,
  ChevronLeft, ChevronRight, AlertTriangle, CheckCircle2, XCircle,
  Mail, Phone, CreditCard,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { cn } from '@/lib/utils'
import { driversApi, driverKeys } from '@/api/drivers'
import type { DriverDto, DriverFilterDto } from '@/api/drivers'
import { teamApi, teamKeys } from '@/api/team'

const PAGE_SIZE = 20

// ─── Schemas ──────────────────────────────────────────────────────────────────

const createSchema = z.object({
  userId: z.string().min(1, 'Sélectionnez un utilisateur'),
  licenseNumber: z.string().optional(),
  licenseExpiryDate: z.string().optional(),
  vehicleType: z.string().optional(),
})

const editSchema = z.object({
  licenseNumber: z.string().optional(),
  licenseExpiryDate: z.string().optional(),
  vehicleType: z.string().optional(),
  isActive: z.boolean(),
})

type CreateValues = z.infer<typeof createSchema>
type EditValues = z.infer<typeof editSchema>

// ─── Dialog state ─────────────────────────────────────────────────────────────

type DialogState =
  | { type: 'none' }
  | { type: 'create' }
  | { type: 'edit'; driver: DriverDto }
  | { type: 'delete'; driver: DriverDto }

// ─── StatChip ─────────────────────────────────────────────────────────────────

function StatChip({ icon, label }: { icon: React.ReactNode; label: string }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white">
      {icon}{label}
    </span>
  )
}

// ─── DriverCreateModal ────────────────────────────────────────────────────────

function DriverCreateModal({ existingUserIds, onClose }: { existingUserIds: Set<string>; onClose: () => void }) {
  const qc = useQueryClient()

  const { data: users = [], isLoading: usersLoading } = useQuery({
    queryKey: teamKeys.users(false),
    queryFn: () => teamApi.getUsers(false),
  })

  const availableUsers = users.filter(u => !existingUserIds.has(u.id))

  const form = useForm<CreateValues>({
    resolver: zodResolver(createSchema),
    defaultValues: { userId: '', licenseNumber: '', licenseExpiryDate: '', vehicleType: '' },
  })

  const mutation = useMutation({
    mutationFn: driversApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: driverKeys.lists() })
      toast.success('Chauffeur créé')
      onClose()
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur de création') : 'Erreur'),
  })

  function onSubmit(values: CreateValues) {
    mutation.mutate({
      userId: values.userId,
      licenseNumber: values.licenseNumber || undefined,
      licenseExpiryDate: values.licenseExpiryDate || undefined,
      vehicleType: values.vehicleType || undefined,
    })
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
      <div className="relative z-10 w-full max-w-md overflow-hidden rounded-2xl bg-card shadow-2xl">
        <div className="bg-gradient-to-br from-sky-500 to-blue-600 px-6 py-5">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
              <UserCheck className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="font-bold text-white">Nouveau chauffeur</h2>
              <p className="text-xs text-sky-200">Associer un utilisateur existant</p>
            </div>
          </div>
        </div>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 p-6">
          <div className="space-y-1.5">
            <Label>Utilisateur</Label>
            <Select
              onValueChange={val => form.setValue('userId', val)}
              defaultValue=""
              disabled={usersLoading}
            >
              <SelectTrigger className={form.formState.errors.userId ? 'border-red-400' : ''}>
                <SelectValue placeholder={usersLoading ? 'Chargement…' : 'Sélectionner un utilisateur'} />
              </SelectTrigger>
              <SelectContent>
                {availableUsers.length === 0 ? (
                  <div className="px-3 py-4 text-center text-sm text-muted-foreground">
                    Aucun utilisateur disponible
                  </div>
                ) : (
                  availableUsers.map(u => (
                    <SelectItem key={u.id} value={u.id}>
                      {u.firstName} {u.lastName} — {u.email}
                    </SelectItem>
                  ))
                )}
              </SelectContent>
            </Select>
            {form.formState.errors.userId && <p className="text-xs text-red-500">{form.formState.errors.userId.message}</p>}
            <p className="text-xs text-muted-foreground">
              Seuls les utilisateurs sans profil chauffeur apparaissent.{' '}
              <span className="text-sky-600 dark:text-sky-400">Invitez d'abord l'utilisateur via l'onglet Équipe.</span>
            </p>
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="licenseNumber">N° de permis (optionnel)</Label>
            <Input id="licenseNumber" {...form.register('licenseNumber')} placeholder="123456789" />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="licenseExpiryDate">Expiration du permis (optionnel)</Label>
            <Input id="licenseExpiryDate" type="date" {...form.register('licenseExpiryDate')} />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="vehicleType">Type de véhicule (optionnel)</Label>
            <Input id="vehicleType" {...form.register('vehicleType')} placeholder="Camionnette, VL…" />
          </div>
          <div className="flex gap-3 pt-2">
            <Button type="button" variant="outline" className="flex-1" onClick={onClose}>Annuler</Button>
            <Button type="submit" className="flex-1" disabled={mutation.isPending}>
              {mutation.isPending
                ? <span className="flex items-center gap-2"><span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />Création…</span>
                : 'Créer'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ─── DriverEditModal ──────────────────────────────────────────────────────────

function DriverEditModal({ driver, onClose }: { driver: DriverDto; onClose: () => void }) {
  const qc = useQueryClient()

  const form = useForm<EditValues>({
    resolver: zodResolver(editSchema),
    defaultValues: {
      licenseNumber: driver.licenseNumber ?? '',
      licenseExpiryDate: driver.licenseExpiryDate ? driver.licenseExpiryDate.split('T')[0] : '',
      vehicleType: driver.vehicleType ?? '',
      isActive: driver.isActive,
    },
  })

  const mutation = useMutation({
    mutationFn: (data: EditValues) => driversApi.update(driver.id, {
      licenseNumber: data.licenseNumber || undefined,
      licenseExpiryDate: data.licenseExpiryDate || undefined,
      vehicleType: data.vehicleType || undefined,
      isActive: data.isActive,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: driverKeys.lists() })
      toast.success('Chauffeur mis à jour')
      onClose()
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur de mise à jour') : 'Erreur'),
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
      <div className="relative z-10 w-full max-w-md overflow-hidden rounded-2xl bg-card shadow-2xl">
        <div className="bg-gradient-to-br from-sky-500 to-blue-600 px-6 py-5">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
              <UserCheck className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="font-bold text-white">Modifier le chauffeur</h2>
              <p className="text-xs text-sky-200">{driver.firstName} {driver.lastName}</p>
            </div>
          </div>
        </div>
        <form onSubmit={form.handleSubmit(v => mutation.mutate(v))} className="space-y-4 p-6">
          <div className="space-y-1.5">
            <Label htmlFor="licenseNumber">N° de permis</Label>
            <Input id="licenseNumber" {...form.register('licenseNumber')} placeholder="123456789" />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="licenseExpiryDate">Expiration du permis</Label>
            <Input id="licenseExpiryDate" type="date" {...form.register('licenseExpiryDate')} />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="vehicleType">Type de véhicule</Label>
            <Input id="vehicleType" {...form.register('vehicleType')} placeholder="Camionnette, VL…" />
          </div>
          <div className="flex items-center gap-2">
            <input type="checkbox" id="isActive" {...form.register('isActive')}
              className="h-4 w-4 rounded border-gray-300 text-sky-600 focus:ring-sky-500" />
            <Label htmlFor="isActive" className="cursor-pointer">Chauffeur actif</Label>
          </div>
          <div className="flex gap-3 pt-2">
            <Button type="button" variant="outline" className="flex-1" onClick={onClose}>Annuler</Button>
            <Button type="submit" className="flex-1" disabled={mutation.isPending}>
              {mutation.isPending
                ? <span className="flex items-center gap-2"><span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />Mise à jour…</span>
                : 'Enregistrer'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ─── DeleteConfirmModal ────────────────────────────────────────────────────────

function DeleteConfirmModal({ driver, onConfirm, onCancel, isPending }: {
  driver: DriverDto; onConfirm: () => void; onCancel: () => void; isPending: boolean
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-white shadow-2xl">
        <div className="p-6">
          <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100">
            <AlertTriangle className="h-6 w-6 text-red-600" />
          </div>
          <h3 className="mb-1 text-base font-semibold text-slate-800">Supprimer ce chauffeur ?</h3>
          <p className="text-sm text-slate-500">
            Le profil chauffeur de <strong>{driver.firstName} {driver.lastName}</strong> sera définitivement supprimé.
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

// ─── DriversPage ──────────────────────────────────────────────────────────────

export default function DriversPage() {
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [dialog, setDialog] = useState<DialogState>({ type: 'none' })

  useEffect(() => {
    const t = setTimeout(() => { setDebouncedSearch(searchInput); setPage(1) }, 300)
    return () => clearTimeout(t)
  }, [searchInput])

  const filters: DriverFilterDto = { page, pageSize: PAGE_SIZE, searchTerm: debouncedSearch || undefined }

  const { data, isLoading, refetch } = useQuery({
    queryKey: driverKeys.list(filters),
    queryFn: () => driversApi.getList(filters),
    placeholderData: prev => prev,
  })

  const deleteMutation = useMutation({
    mutationFn: (id: number) => driversApi.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: driverKeys.lists() })
      toast.success('Chauffeur supprimé')
      setDialog({ type: 'none' })
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Suppression impossible') : 'Erreur'),
  })

  const closeDialog = useCallback(() => setDialog({ type: 'none' }), [])

  const drivers = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1
  const from = Math.min((page - 1) * PAGE_SIZE + 1, totalCount)
  const to = Math.min(page * PAGE_SIZE, totalCount)
  const activeCount = drivers.filter(d => d.isActive).length

  const existingUserIds = new Set(drivers.map(d => d.userId))

  function getInitials(firstName: string, lastName: string) {
    return `${firstName.charAt(0)}${lastName.charAt(0)}`.toUpperCase()
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
                <UserCheck className="h-5 w-5 text-white" />
              </div>
              <h1 className="text-2xl font-bold tracking-tight text-white">Chauffeurs</h1>
            </div>
            <p className="text-sm text-sky-200">Gérez les profils chauffeurs</p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            {!isLoading && (
              <>
                <StatChip icon={<UserCheck className="h-3.5 w-3.5" />} label={`${totalCount} chauffeur${totalCount > 1 ? 's' : ''}`} />
                <StatChip icon={<CheckCircle2 className="h-3.5 w-3.5" />} label={`${activeCount} actif${activeCount > 1 ? 's' : ''}`} />
              </>
            )}
            <button
              onClick={() => setDialog({ type: 'create' })}
              className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white backdrop-blur-sm transition-colors hover:bg-white/25"
            >
              <Plus className="h-3.5 w-3.5" />Nouveau chauffeur
            </button>
          </div>
        </div>
      </div>

      {/* Toolbar */}
      <div className="flex items-center gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <Input className="pl-9 pr-9" placeholder="Rechercher un chauffeur…" value={searchInput} onChange={e => setSearchInput(e.target.value)} />
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
              <TableHead className="pl-6 text-xs font-semibold uppercase tracking-wider text-slate-400">Chauffeur</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Contact</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Permis</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Statut</TableHead>
              <TableHead className="w-20 pr-6" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              [...Array(5)].map((_, i) => (
                <TableRow key={i}>
                  <TableCell className="pl-6"><Skeleton className="h-10 w-48" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                  <TableCell><Skeleton className="h-6 w-16 rounded-full" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-12" /></TableCell>
                </TableRow>
              ))
            ) : drivers.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="py-16 text-center">
                  <div className="flex flex-col items-center gap-3">
                    <div className="flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
                      <UserCheck className="h-6 w-6" />
                    </div>
                    <p className="text-sm font-medium text-slate-500">
                      {debouncedSearch ? 'Aucun chauffeur ne correspond à votre recherche' : 'Aucun chauffeur'}
                    </p>
                    {!debouncedSearch && (
                      <Button size="sm" onClick={() => setDialog({ type: 'create' })}>
                        <Plus className="mr-1.5 h-3.5 w-3.5" />Créer le premier chauffeur
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              drivers.map(driver => (
                <TableRow key={driver.id} className={cn('hover:bg-sky-50/40', !driver.isActive && 'opacity-60')}>
                  <TableCell className="py-3 pl-6">
                    <div className="flex items-center gap-3">
                      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-sky-500 to-blue-600 text-sm font-bold text-white">
                        {getInitials(driver.firstName, driver.lastName)}
                      </div>
                      <div>
                        <p className="font-semibold text-slate-800">{driver.firstName} {driver.lastName}</p>
                        <p className="flex items-center gap-1 text-xs text-slate-400">
                          <Mail className="h-3 w-3" />{driver.email}
                        </p>
                      </div>
                    </div>
                  </TableCell>
                  <TableCell>
                    {driver.phone && (
                      <div className="flex items-center gap-1.5 text-sm text-slate-600">
                        <Phone className="h-3.5 w-3.5 text-slate-400" />{driver.phone}
                      </div>
                    )}
                  </TableCell>
                  <TableCell>
                    {driver.licenseNumber ? (
                      <div className="flex items-center gap-1.5 text-sm text-slate-600">
                        <CreditCard className="h-3.5 w-3.5 text-slate-400" />{driver.licenseNumber}
                        {driver.licenseExpiryDate && (
                          <span className="text-xs text-slate-400">
                            (exp. {new Date(driver.licenseExpiryDate).toLocaleDateString('fr-FR')})
                          </span>
                        )}
                      </div>
                    ) : (
                      <span className="text-sm text-slate-400">—</span>
                    )}
                  </TableCell>
                  <TableCell>
                    <span className={cn(
                      'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium',
                      driver.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500',
                    )}>
                      {driver.isActive ? <CheckCircle2 className="h-3 w-3" /> : <XCircle className="h-3 w-3" />}
                      {driver.isActive ? 'Actif' : 'Inactif'}
                    </span>
                  </TableCell>
                  <TableCell className="pr-6" onClick={e => e.stopPropagation()}>
                    <div className="flex items-center justify-end gap-1">
                      <button onClick={() => setDialog({ type: 'edit', driver })} className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600" title="Modifier">
                        <Pencil className="h-4 w-4" />
                      </button>
                      <button onClick={() => setDialog({ type: 'delete', driver })} className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-red-50 hover:text-red-500" title="Supprimer">
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
            <strong>{from}</strong> à <strong>{to}</strong> sur <strong>{totalCount}</strong> chauffeur{totalCount > 1 ? 's' : ''}
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
      {dialog.type === 'create' && (
        <DriverCreateModal existingUserIds={existingUserIds} onClose={closeDialog} />
      )}
      {dialog.type === 'edit' && (
        <DriverEditModal driver={dialog.driver} onClose={closeDialog} />
      )}
      {dialog.type === 'delete' && (
        <DeleteConfirmModal
          driver={dialog.driver}
          onConfirm={() => deleteMutation.mutate(dialog.driver.id)}
          onCancel={closeDialog}
          isPending={deleteMutation.isPending}
        />
      )}
    </div>
  )
}
