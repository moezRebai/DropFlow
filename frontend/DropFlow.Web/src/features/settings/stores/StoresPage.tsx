import { useState, useEffect, useCallback, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMapsLibrary } from '@vis.gl/react-google-maps'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import {
  ShoppingBag, Plus, Search, X, RefreshCw, Pencil, Trash2,
  ChevronLeft, ChevronRight, AlertTriangle, CheckCircle2, XCircle,
  MapPin, Phone, Mail,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { cn } from '@/lib/utils'
import { storesApi, storeKeys } from '@/api/stores'
import type { StoreDto, StoreFilterDto } from '@/api/stores'

const PAGE_SIZE = 20

// ─── Schema ──────────────────────────────────────────────────────────────────

const schema = z.object({
  name: z.string().min(1, 'Nom requis'),
  address: z.string().min(2, 'Adresse requise'),
  zipCode: z.string().min(4, 'Code postal requis'),
  city: z.string().min(1, 'Ville requise'),
  contactName: z.string().optional().default(''),
  phone: z.string().optional().default(''),
  email: z.string().email('Email invalide').optional().or(z.literal('')),
  notes: z.string().optional().default(''),
  isActive: z.boolean().default(true),
})
type FormValues = z.infer<typeof schema>

// ─── Dialog state ─────────────────────────────────────────────────────────────

type DialogState =
  | { type: 'none' }
  | { type: 'create' }
  | { type: 'edit'; store: StoreDto }
  | { type: 'delete'; store: StoreDto }

// ─── StatChip ─────────────────────────────────────────────────────────────────

function StatChip({ icon, label }: { icon: React.ReactNode; label: string }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white">
      {icon}{label}
    </span>
  )
}

// ─── Address autocomplete ─────────────────────────────────────────────────────

function AddressAutocomplete({ value, onChange, onPlaceSelect, error }: {
  value: string
  onChange: (v: string) => void
  onPlaceSelect: (result: { address: string; zipCode: string; city: string }) => void
  error?: string
}) {
  const places = useMapsLibrary('places')
  const inputRef = useRef<HTMLInputElement>(null)
  const onPlaceSelectRef = useRef(onPlaceSelect)
  const onChangeRef = useRef(onChange)

  useEffect(() => {
    onPlaceSelectRef.current = onPlaceSelect
    onChangeRef.current = onChange
  })

  useEffect(() => {
    if (!places || !inputRef.current) return

    const ac = new places.Autocomplete(inputRef.current, {
      types: ['address'],
      componentRestrictions: { country: 'fr' },
      fields: ['address_components'],
    })

    ac.addListener('place_changed', () => {
      const place = ac.getPlace()
      if (!place.address_components) return

      let streetNumber = '', route = '', city = '', zipCode = ''
      for (const c of place.address_components) {
        if (c.types.includes('street_number')) streetNumber = c.long_name
        if (c.types.includes('route'))         route = c.long_name
        if (c.types.includes('locality'))      city = c.long_name
        if (c.types.includes('postal_code'))   zipCode = c.long_name
      }

      const address = [streetNumber, route].filter(Boolean).join(' ')
      onChangeRef.current(address)
      onPlaceSelectRef.current({ address, zipCode, city })
    })

    return () => google.maps.event.clearInstanceListeners(ac)
  }, [places])

  return (
    <input
      ref={inputRef}
      value={value}
      onChange={e => onChange(e.target.value)}
      autoComplete="new-password"
      placeholder="Commencez à saisir une adresse…"
      className={cn(
        'flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors',
        'placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring',
        error && 'border-destructive',
      )}
    />
  )
}

// ─── StoreFormModal ───────────────────────────────────────────────────────────

function StoreFormModal({ store, onClose }: { store?: StoreDto; onClose: () => void }) {
  const qc = useQueryClient()
  const isEdit = !!store

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: store
      ? {
          name: store.name,
          address: store.address,
          zipCode: store.zipCode,
          city: store.city,
          contactName: store.contactName ?? '',
          phone: store.phone ?? '',
          email: store.email ?? '',
          notes: store.notes ?? '',
          isActive: store.isActive,
        }
      : { name: '', address: '', zipCode: '', city: '', contactName: '', phone: '', email: '', notes: '', isActive: true },
  })

  const createMutation = useMutation({
    mutationFn: storesApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: storeKeys.lists() })
      qc.invalidateQueries({ queryKey: storeKeys.lookup })
      toast.success('Enseigne créée')
      onClose()
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur de création') : 'Erreur'),
  })

  const updateMutation = useMutation({
    mutationFn: (data: FormValues) => storesApi.update(store!.id, data as Required<FormValues>),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: storeKeys.lists() })
      qc.invalidateQueries({ queryKey: storeKeys.lookup })
      toast.success('Enseigne mise à jour')
      onClose()
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur de mise à jour') : 'Erreur'),
  })

  function onSubmit(values: FormValues) {
    const payload = {
      name: values.name,
      address: values.address,
      zipCode: values.zipCode,
      city: values.city,
      contactName: values.contactName ?? '',
      phone: values.phone ?? '',
      email: values.email ?? '',
      notes: values.notes ?? '',
    }
    if (isEdit) {
      updateMutation.mutate({ ...payload, isActive: values.isActive })
    } else {
      createMutation.mutate(payload)
    }
  }

  const isPending = createMutation.isPending || updateMutation.isPending

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
      <div className="relative z-10 w-full max-w-lg overflow-hidden rounded-2xl bg-card shadow-2xl">
        <div className="bg-gradient-to-br from-sky-500 to-blue-600 px-6 py-5">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
              <ShoppingBag className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="font-bold text-white">{isEdit ? 'Modifier l\'enseigne' : 'Nouvelle enseigne'}</h2>
              <p className="text-xs text-sky-200">{isEdit ? store.name : 'Renseignez les informations de l\'enseigne'}</p>
            </div>
          </div>
        </div>
        <form onSubmit={form.handleSubmit(onSubmit)} className="max-h-[70vh] overflow-y-auto">
          <div className="space-y-4 p-6">
            <div className="space-y-1.5">
              <Label htmlFor="name">Nom de l'enseigne *</Label>
              <Input id="name" {...form.register('name')} placeholder="Carrefour Market" />
              {form.formState.errors.name && <p className="text-xs text-red-500">{form.formState.errors.name.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Adresse *</Label>
              <Controller
                control={form.control}
                name="address"
                render={({ field }) => (
                  <AddressAutocomplete
                    value={field.value}
                    onChange={field.onChange}
                    onPlaceSelect={({ address, zipCode, city }) => {
                      form.setValue('address', address, { shouldValidate: true })
                      form.setValue('zipCode', zipCode, { shouldValidate: true })
                      form.setValue('city', city, { shouldValidate: true })
                    }}
                    error={form.formState.errors.address?.message}
                  />
                )}
              />
              {form.formState.errors.address && <p className="text-xs text-red-500">{form.formState.errors.address.message}</p>}
            </div>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label htmlFor="zipCode">Code postal *</Label>
                <Input id="zipCode" {...form.register('zipCode')} placeholder="75001" />
                {form.formState.errors.zipCode && <p className="text-xs text-red-500">{form.formState.errors.zipCode.message}</p>}
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="city">Ville *</Label>
                <Input id="city" {...form.register('city')} placeholder="Paris" />
                {form.formState.errors.city && <p className="text-xs text-red-500">{form.formState.errors.city.message}</p>}
              </div>
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="contactName">Nom du contact</Label>
              <Input id="contactName" {...form.register('contactName')} placeholder="Jean Dupont" />
            </div>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label htmlFor="phone">Téléphone</Label>
                <Input id="phone" {...form.register('phone')} placeholder="06 00 00 00 00" />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="email">Email</Label>
                <Input id="email" type="email" {...form.register('email')} placeholder="contact@enseigne.fr" />
                {form.formState.errors.email && <p className="text-xs text-red-500">{form.formState.errors.email.message}</p>}
              </div>
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="notes">Notes</Label>
              <Input id="notes" {...form.register('notes')} placeholder="Informations supplémentaires…" />
            </div>
            {isEdit && (
              <div className="flex items-center gap-2">
                <input type="checkbox" id="isActive" {...form.register('isActive')}
                  className="h-4 w-4 rounded border-input text-sky-600 focus:ring-sky-500" />
                <Label htmlFor="isActive" className="cursor-pointer">Enseigne active</Label>
              </div>
            )}
          </div>
          <div className="flex gap-3 border-t px-6 py-4">
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

function DeleteConfirmModal({ store, onConfirm, onCancel, isPending }: {
  store: StoreDto; onConfirm: () => void; onCancel: () => void; isPending: boolean
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-card shadow-2xl">
        <div className="p-6">
          <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100 dark:bg-red-500/15">
            <AlertTriangle className="h-6 w-6 text-red-600 dark:text-red-400" />
          </div>
          <h3 className="mb-1 text-base font-semibold text-foreground">Supprimer cette enseigne ?</h3>
          <p className="text-sm text-muted-foreground">
            <strong>{store.name}</strong> sera définitivement supprimée. Cette action est irréversible.
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

// ─── StoresPage ───────────────────────────────────────────────────────────────

export default function StoresPage() {
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [dialog, setDialog] = useState<DialogState>({ type: 'none' })

  useEffect(() => {
    const t = setTimeout(() => { setDebouncedSearch(searchInput); setPage(1) }, 300)
    return () => clearTimeout(t)
  }, [searchInput])

  const filters: StoreFilterDto = { page, pageSize: PAGE_SIZE, searchTerm: debouncedSearch || undefined }

  const { data, isLoading, refetch } = useQuery({
    queryKey: storeKeys.list(filters),
    queryFn: () => storesApi.getList(filters),
    placeholderData: prev => prev,
  })

  const deleteMutation = useMutation({
    mutationFn: (id: number) => storesApi.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: storeKeys.lists() })
      qc.invalidateQueries({ queryKey: storeKeys.lookup })
      toast.success('Enseigne supprimée')
      setDialog({ type: 'none' })
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Suppression impossible') : 'Erreur'),
  })

  const closeDialog = useCallback(() => setDialog({ type: 'none' }), [])

  const stores = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1
  const from = Math.min((page - 1) * PAGE_SIZE + 1, totalCount)
  const to = Math.min(page * PAGE_SIZE, totalCount)
  const activeCount = stores.filter(s => s.isActive).length

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Hero */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 p-6 shadow-lg">
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }} />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="mb-1 flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
                <ShoppingBag className="h-5 w-5 text-white" />
              </div>
              <h1 className="text-2xl font-bold tracking-tight text-white">Enseignes</h1>
            </div>
            <p className="text-sm text-sky-200">Gérez les enseignes clientes</p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            {!isLoading && (
              <>
                <StatChip icon={<ShoppingBag className="h-3.5 w-3.5" />} label={`${totalCount} enseigne${totalCount > 1 ? 's' : ''}`} />
                <StatChip icon={<CheckCircle2 className="h-3.5 w-3.5" />} label={`${activeCount} active${activeCount > 1 ? 's' : ''}`} />
              </>
            )}
            <button
              onClick={() => setDialog({ type: 'create' })}
              className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white backdrop-blur-sm transition-colors hover:bg-white/25"
            >
              <Plus className="h-3.5 w-3.5" />Nouvelle enseigne
            </button>
          </div>
        </div>
      </div>

      {/* Toolbar */}
      <div className="flex items-center gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input className="pl-9 pr-9" placeholder="Rechercher une enseigne, ville…" value={searchInput} onChange={e => setSearchInput(e.target.value)} />
          {searchInput && (
            <button onClick={() => setSearchInput('')} aria-label="Effacer la recherche" className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground">
              <X className="h-4 w-4" />
            </button>
          )}
        </div>
        <Button variant="outline" size="icon" onClick={() => refetch()} title="Actualiser"><RefreshCw className="h-4 w-4" /></Button>
      </div>

      {/* Table */}
      <div className="overflow-hidden rounded-2xl border bg-card shadow-sm">
        <Table>
          <TableHeader>
            <TableRow className="bg-muted hover:bg-muted">
              <TableHead className="pl-6 text-xs font-semibold uppercase tracking-wider text-muted-foreground">Enseigne</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Localisation</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Contact</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Statut</TableHead>
              <TableHead className="w-20 pr-6" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              [...Array(5)].map((_, i) => (
                <TableRow key={i}>
                  <TableCell className="pl-6"><Skeleton className="h-10 w-40" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                  <TableCell><Skeleton className="h-6 w-16 rounded-full" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-12" /></TableCell>
                </TableRow>
              ))
            ) : stores.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="py-16 text-center">
                  <div className="flex flex-col items-center gap-3">
                    <div className="flex h-12 w-12 items-center justify-center rounded-full bg-muted text-muted-foreground">
                      <ShoppingBag className="h-6 w-6" />
                    </div>
                    <p className="text-sm font-medium text-muted-foreground">
                      {debouncedSearch ? 'Aucune enseigne ne correspond à votre recherche' : 'Aucune enseigne'}
                    </p>
                    {!debouncedSearch && (
                      <Button size="sm" onClick={() => setDialog({ type: 'create' })}>
                        <Plus className="mr-1.5 h-3.5 w-3.5" />Créer la première enseigne
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              stores.map(store => (
                <TableRow key={store.id} className={cn('hover:bg-sky-50/40 dark:hover:bg-sky-500/5', !store.isActive && 'opacity-60')}>
                  <TableCell className="py-3 pl-6">
                    <div className="flex items-center gap-3">
                      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-sky-100 to-blue-100 dark:from-sky-500/15 dark:to-blue-500/15">
                        <ShoppingBag className="h-5 w-5 text-sky-600 dark:text-sky-400" />
                      </div>
                      <p className="font-semibold text-foreground">{store.name}</p>
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
                      <MapPin className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
                      {store.city} {store.zipCode && `(${store.zipCode})`}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="space-y-0.5">
                      {store.contactName && <p className="text-sm text-foreground">{store.contactName}</p>}
                      {store.phone && (
                        <div className="flex items-center gap-1 text-xs text-muted-foreground">
                          <Phone className="h-3 w-3" />{store.phone}
                        </div>
                      )}
                      {store.email && (
                        <div className="flex items-center gap-1 text-xs text-muted-foreground">
                          <Mail className="h-3 w-3" />{store.email}
                        </div>
                      )}
                    </div>
                  </TableCell>
                  <TableCell>
                    <span className={cn(
                      'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium',
                      store.isActive ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-400' : 'bg-muted text-muted-foreground',
                    )}>
                      {store.isActive ? <CheckCircle2 className="h-3 w-3" /> : <XCircle className="h-3 w-3" />}
                      {store.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </TableCell>
                  <TableCell className="pr-6" onClick={e => e.stopPropagation()}>
                    <div className="flex items-center justify-end gap-1">
                      <button onClick={() => setDialog({ type: 'edit', store })} aria-label="Modifier" className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground" title="Modifier">
                        <Pencil className="h-4 w-4" />
                      </button>
                      <button onClick={() => setDialog({ type: 'delete', store })} aria-label="Supprimer" className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-red-50 hover:text-red-500 dark:hover:bg-red-500/10 dark:hover:text-red-400" title="Supprimer">
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
          <p className="text-sm text-muted-foreground">
            <strong>{from}</strong> à <strong>{to}</strong> sur <strong>{totalCount}</strong> enseigne{totalCount > 1 ? 's' : ''}
          </p>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}>
              <ChevronLeft className="h-4 w-4" />Précédent
            </Button>
            <span className="rounded-lg border bg-card px-3 py-1.5 text-sm font-medium text-foreground">{page} / {totalPages}</span>
            <Button variant="outline" size="sm" onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page >= totalPages}>
              Suivant<ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}

      {/* Modals */}
      {(dialog.type === 'create' || dialog.type === 'edit') && (
        <StoreFormModal store={dialog.type === 'edit' ? dialog.store : undefined} onClose={closeDialog} />
      )}
      {dialog.type === 'delete' && (
        <DeleteConfirmModal
          store={dialog.store}
          onConfirm={() => deleteMutation.mutate(dialog.store.id)}
          onCancel={closeDialog}
          isPending={deleteMutation.isPending}
        />
      )}
    </div>
  )
}
