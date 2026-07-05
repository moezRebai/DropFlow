import { useState, useCallback, useRef, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMapsLibrary } from '@vis.gl/react-google-maps'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import {
  Building2, MapPin, Plus, Pencil, Trash2, AlertTriangle,
  RefreshCw, CheckCircle2, Star, ToggleLeft, ToggleRight,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { cn } from '@/lib/utils'
import { settingsApi, settingsKeys } from '@/api/settings'
import type { TenantDepotDto } from '@/api/settings'

// ─── Schemas ──────────────────────────────────────────────────────────────────

const companySchema = z.object({
  companyName: z.string().optional(),
  address: z.string().optional(),
  zipCode: z.string().optional(),
  city: z.string().optional(),
  phone: z.string().optional(),
  email: z.string().email('Email invalide').optional().or(z.literal('')),
  website: z.string().url('URL invalide').optional().or(z.literal('')),
})

const legalSchema = z.object({
  siret: z.string().optional(),
  vatNumber: z.string().optional(),
  legalForm: z.string().optional(),
  legalMentions: z.string().optional(),
  bankDetails: z.string().optional(),
})

const depotSchema = z.object({
  name: z.string().min(1, 'Nom requis'),
  fullAddress: z.string().min(2, 'Adresse requise'),
  city: z.string().optional(),
  zipCode: z.string().optional(),
  isDefault: z.boolean().default(false),
  isActive: z.boolean().default(true),
})

type CompanyValues = z.infer<typeof companySchema>
type LegalValues = z.infer<typeof legalSchema>
type DepotValues = z.infer<typeof depotSchema>

// ─── Tab type ─────────────────────────────────────────────────────────────────

type Tab = 'company' | 'depots'

// ─── Depot dialog state ───────────────────────────────────────────────────────

type DepotDialog =
  | { type: 'none' }
  | { type: 'create' }
  | { type: 'edit'; depot: TenantDepotDto }
  | { type: 'delete'; depot: TenantDepotDto }

// ─── StatChip ─────────────────────────────────────────────────────────────────

function StatChip({ icon, label }: { icon: React.ReactNode; label: string }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white">
      {icon}{label}
    </span>
  )
}

// ─── Company info form ────────────────────────────────────────────────────────

function CompanyInfoForm({ initialData }: { initialData: CompanyValues }) {
  const qc = useQueryClient()

  const form = useForm<CompanyValues>({
    resolver: zodResolver(companySchema),
    defaultValues: initialData,
  })

  const mutation = useMutation({
    mutationFn: settingsApi.updateCompanyInfo,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: settingsKeys.company })
      toast.success('Informations société mises à jour')
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur de mise à jour') : 'Erreur'),
  })

  return (
    <form onSubmit={form.handleSubmit(v => mutation.mutate(v))} className="space-y-4">
      <div className="space-y-1.5">
        <Label htmlFor="companyName">Raison sociale</Label>
        <Input id="companyName" {...form.register('companyName')} placeholder="Ma Société SAS" />
      </div>
      <div className="space-y-1.5">
        <Label htmlFor="companyAddress">Adresse</Label>
        <Input id="companyAddress" {...form.register('address')} placeholder="12 rue du Commerce" />
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label htmlFor="zipCode">Code postal</Label>
          <Input id="zipCode" {...form.register('zipCode')} placeholder="75001" />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="city">Ville</Label>
          <Input id="city" {...form.register('city')} placeholder="Paris" />
        </div>
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label htmlFor="companyPhone">Téléphone</Label>
          <Input id="companyPhone" {...form.register('phone')} placeholder="01 00 00 00 00" />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="companyEmail">Email</Label>
          <Input id="companyEmail" type="email" {...form.register('email')} placeholder="contact@societe.fr" />
          {form.formState.errors.email && <p className="text-xs text-red-500">{form.formState.errors.email.message}</p>}
        </div>
      </div>
      <div className="space-y-1.5">
        <Label htmlFor="website">Site web</Label>
        <Input id="website" {...form.register('website')} placeholder="https://www.societe.fr" />
        {form.formState.errors.website && <p className="text-xs text-red-500">{form.formState.errors.website.message}</p>}
      </div>
      <div className="flex justify-end pt-2">
        <Button type="submit" disabled={mutation.isPending}>
          {mutation.isPending
            ? <span className="flex items-center gap-2"><span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />Enregistrement…</span>
            : 'Enregistrer'}
        </Button>
      </div>
    </form>
  )
}

// ─── Legal info form ──────────────────────────────────────────────────────────

function LegalInfoForm({ initialData }: { initialData: LegalValues }) {
  const qc = useQueryClient()

  const form = useForm<LegalValues>({
    resolver: zodResolver(legalSchema),
    defaultValues: initialData,
  })

  const mutation = useMutation({
    mutationFn: settingsApi.updateLegalInfo,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: settingsKeys.company })
      toast.success('Informations légales mises à jour')
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur de mise à jour') : 'Erreur'),
  })

  return (
    <form onSubmit={form.handleSubmit(v => mutation.mutate(v))} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label htmlFor="siret">SIRET</Label>
          <Input id="siret" {...form.register('siret')} placeholder="12345678900012" />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="vatNumber">N° TVA intracommunautaire</Label>
          <Input id="vatNumber" {...form.register('vatNumber')} placeholder="FR12345678900" />
        </div>
      </div>
      <div className="space-y-1.5">
        <Label htmlFor="legalForm">Forme juridique</Label>
        <Input id="legalForm" {...form.register('legalForm')} placeholder="SAS, SARL, EURL…" />
      </div>
      <div className="space-y-1.5">
        <Label htmlFor="legalMentions">Mentions légales</Label>
        <Input id="legalMentions" {...form.register('legalMentions')} placeholder="Mentions légales pour les documents" />
      </div>
      <div className="space-y-1.5">
        <Label htmlFor="bankDetails">Coordonnées bancaires</Label>
        <Input id="bankDetails" {...form.register('bankDetails')} placeholder="IBAN / BIC" />
      </div>
      <div className="flex justify-end pt-2">
        <Button type="submit" disabled={mutation.isPending}>
          {mutation.isPending
            ? <span className="flex items-center gap-2"><span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />Enregistrement…</span>
            : 'Enregistrer'}
        </Button>
      </div>
    </form>
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

// ─── DepotFormModal ───────────────────────────────────────────────────────────

function DepotFormModal({ depot, onClose }: { depot?: TenantDepotDto; onClose: () => void }) {
  const qc = useQueryClient()
  const isEdit = !!depot

  const form = useForm<DepotValues>({
    resolver: zodResolver(depotSchema),
    defaultValues: depot
      ? { name: depot.name, fullAddress: depot.fullAddress, city: depot.city ?? '', zipCode: depot.zipCode ?? '', isDefault: depot.isDefault, isActive: depot.isActive }
      : { name: '', fullAddress: '', city: '', zipCode: '', isDefault: false, isActive: true },
  })

  const createMutation = useMutation({
    mutationFn: (data: DepotValues) => settingsApi.createDepot({
      name: data.name,
      fullAddress: data.fullAddress,
      city: data.city || undefined,
      zipCode: data.zipCode || undefined,
      isDefault: data.isDefault,
      isActive: data.isActive,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: settingsKeys.depots.all })
      toast.success('Dépôt créé')
      onClose()
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur de création') : 'Erreur'),
  })

  const updateMutation = useMutation({
    mutationFn: (data: DepotValues) => settingsApi.updateDepot(depot!.id, {
      name: data.name,
      fullAddress: data.fullAddress,
      city: data.city || undefined,
      zipCode: data.zipCode || undefined,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: settingsKeys.depots.all })
      toast.success('Dépôt mis à jour')
      onClose()
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur de mise à jour') : 'Erreur'),
  })

  const isPending = createMutation.isPending || updateMutation.isPending

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
      <div className="relative z-10 w-full max-w-md overflow-hidden rounded-2xl bg-white shadow-2xl">
        <div className="bg-gradient-to-br from-sky-500 to-blue-600 px-6 py-5">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
              <MapPin className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="font-bold text-white">{isEdit ? 'Modifier le dépôt' : 'Nouveau dépôt'}</h2>
              <p className="text-xs text-sky-200">{isEdit ? depot.name : 'Ajouter un point de départ'}</p>
            </div>
          </div>
        </div>
        <form onSubmit={form.handleSubmit(v => isEdit ? updateMutation.mutate(v) : createMutation.mutate(v))} className="space-y-4 p-6">
          <div className="space-y-1.5">
            <Label htmlFor="depotName">Nom du dépôt *</Label>
            <Input id="depotName" {...form.register('name')} placeholder="Dépôt principal" />
            {form.formState.errors.name && <p className="text-xs text-red-500">{form.formState.errors.name.message}</p>}
          </div>
          <div className="space-y-1.5">
            <Label>Adresse complète *</Label>
            <Controller
              control={form.control}
              name="fullAddress"
              render={({ field }) => (
                <AddressAutocomplete
                  value={field.value}
                  onChange={field.onChange}
                  onPlaceSelect={({ address, zipCode, city }) => {
                    form.setValue('fullAddress', address, { shouldValidate: true })
                    form.setValue('zipCode', zipCode, { shouldValidate: true })
                    form.setValue('city', city, { shouldValidate: true })
                  }}
                  error={form.formState.errors.fullAddress?.message}
                />
              )}
            />
            {form.formState.errors.fullAddress && <p className="text-xs text-red-500">{form.formState.errors.fullAddress.message}</p>}
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label htmlFor="depotZipCode">Code postal</Label>
              <Input id="depotZipCode" {...form.register('zipCode')} placeholder="75001" />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="depotCity">Ville</Label>
              <Input id="depotCity" {...form.register('city')} placeholder="Paris" />
            </div>
          </div>
          {!isEdit && (
            <div className="flex items-center gap-2">
              <input type="checkbox" id="isDefault" {...form.register('isDefault')}
                className="h-4 w-4 rounded border-gray-300 text-sky-600 focus:ring-sky-500" />
              <Label htmlFor="isDefault" className="cursor-pointer">Dépôt par défaut</Label>
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

// ─── DepotDeleteModal ─────────────────────────────────────────────────────────

function DepotDeleteModal({ depot, onConfirm, onCancel, isPending }: {
  depot: TenantDepotDto; onConfirm: () => void; onCancel: () => void; isPending: boolean
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-white shadow-2xl">
        <div className="p-6">
          <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100">
            <AlertTriangle className="h-6 w-6 text-red-600" />
          </div>
          <h3 className="mb-1 text-base font-semibold text-slate-800">Supprimer ce dépôt ?</h3>
          <p className="text-sm text-slate-500">
            Le dépôt <strong>«{depot.name}»</strong> sera définitivement supprimé.
          </p>
          {depot.isDefault && (
            <p className="mt-2 rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-700">
              Ce dépôt est le dépôt par défaut. Pensez à en définir un autre après la suppression.
            </p>
          )}
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

// ─── DepotsTab ────────────────────────────────────────────────────────────────

function DepotsTab() {
  const qc = useQueryClient()
  const [dialog, setDialog] = useState<DepotDialog>({ type: 'none' })

  const DEPOT_FILTERS = { pageSize: 500, includeInactive: true }

  const { data: depotsResult, isLoading, refetch } = useQuery({
    queryKey: settingsKeys.depots.list(DEPOT_FILTERS),
    queryFn: () => settingsApi.getDepots(DEPOT_FILTERS),
  })
  const depots = depotsResult?.items ?? []

  const deleteMutation = useMutation({
    mutationFn: (id: number) => settingsApi.deleteDepot(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: settingsKeys.depots.all })
      toast.success('Dépôt supprimé')
      setDialog({ type: 'none' })
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Suppression impossible') : 'Erreur'),
  })

  const setDefaultMutation = useMutation({
    mutationFn: (id: number) => settingsApi.setDefaultDepot(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: settingsKeys.depots.all })
      toast.success('Dépôt par défaut mis à jour')
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur') : 'Erreur'),
  })

  const toggleMutation = useMutation({
    mutationFn: (id: number) => settingsApi.toggleDepotStatus(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: settingsKeys.depots.all })
      toast.success('Statut mis à jour')
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur') : 'Erreur'),
  })

  const closeDialog = useCallback(() => setDialog({ type: 'none' }), [])

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-slate-500">{depots.length} dépôt{depots.length > 1 ? 's' : ''} configuré{depots.length > 1 ? 's' : ''}</p>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="icon" onClick={() => refetch()} title="Actualiser"><RefreshCw className="h-4 w-4" /></Button>
          <Button size="sm" onClick={() => setDialog({ type: 'create' })}>
            <Plus className="mr-1.5 h-3.5 w-3.5" />Nouveau dépôt
          </Button>
        </div>
      </div>

      <div className="overflow-hidden rounded-2xl border bg-white shadow-sm">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead className="pl-6 text-xs font-semibold uppercase tracking-wider text-slate-400">Dépôt</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Adresse</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Statut</TableHead>
              <TableHead className="w-32 pr-6" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              [...Array(3)].map((_, i) => (
                <TableRow key={i}>
                  <TableCell className="pl-6"><Skeleton className="h-8 w-40" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-48" /></TableCell>
                  <TableCell><Skeleton className="h-6 w-20 rounded-full" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                </TableRow>
              ))
            ) : depots.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="py-12 text-center">
                  <div className="flex flex-col items-center gap-3">
                    <div className="flex h-10 w-10 items-center justify-center rounded-full bg-slate-100 text-slate-400">
                      <MapPin className="h-5 w-5" />
                    </div>
                    <p className="text-sm font-medium text-slate-500">Aucun dépôt configuré</p>
                    <Button size="sm" onClick={() => setDialog({ type: 'create' })}>
                      <Plus className="mr-1.5 h-3.5 w-3.5" />Créer le premier dépôt
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              depots.map(depot => (
                <TableRow key={depot.id} className={cn('hover:bg-sky-50/40', !depot.isActive && 'opacity-60')}>
                  <TableCell className="py-3 pl-6">
                    <div className="flex items-center gap-2">
                      <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-sky-100">
                        <MapPin className="h-4 w-4 text-sky-600" />
                      </div>
                      <div className="flex items-center gap-1.5">
                        <span className="font-semibold text-slate-800">{depot.name}</span>
                        {depot.isDefault && (
                          <span className="inline-flex items-center gap-0.5 rounded-full bg-amber-100 px-1.5 py-0.5 text-xs font-medium text-amber-700">
                            <Star className="h-2.5 w-2.5 fill-current" />Défaut
                          </span>
                        )}
                      </div>
                    </div>
                  </TableCell>
                  <TableCell>
                    <span className="text-sm text-slate-600">{depot.fullAddress}</span>
                  </TableCell>
                  <TableCell>
                    <span className={cn(
                      'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium',
                      depot.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500',
                    )}>
                      <CheckCircle2 className="h-3 w-3" />
                      {depot.isActive ? 'Actif' : 'Inactif'}
                    </span>
                  </TableCell>
                  <TableCell className="pr-6" onClick={e => e.stopPropagation()}>
                    <div className="flex items-center justify-end gap-1">
                      {!depot.isDefault && (
                        <button
                          onClick={() => setDefaultMutation.mutate(depot.id)}
                          className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-amber-50 hover:text-amber-600"
                          title="Définir par défaut"
                          disabled={setDefaultMutation.isPending}
                        >
                          <Star className="h-4 w-4" />
                        </button>
                      )}
                      <button
                        onClick={() => toggleMutation.mutate(depot.id)}
                        className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
                        title={depot.isActive ? 'Désactiver' : 'Activer'}
                        disabled={toggleMutation.isPending}
                      >
                        {depot.isActive ? <ToggleRight className="h-4 w-4" /> : <ToggleLeft className="h-4 w-4" />}
                      </button>
                      <button onClick={() => setDialog({ type: 'edit', depot })} className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600" title="Modifier">
                        <Pencil className="h-4 w-4" />
                      </button>
                      <button onClick={() => setDialog({ type: 'delete', depot })} className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-red-50 hover:text-red-500" title="Supprimer">
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

      {(dialog.type === 'create' || dialog.type === 'edit') && (
        <DepotFormModal depot={dialog.type === 'edit' ? dialog.depot : undefined} onClose={closeDialog} />
      )}
      {dialog.type === 'delete' && (
        <DepotDeleteModal
          depot={dialog.depot}
          onConfirm={() => deleteMutation.mutate(dialog.depot.id)}
          onCancel={closeDialog}
          isPending={deleteMutation.isPending}
        />
      )}
    </div>
  )
}

// ─── CompanySettings ──────────────────────────────────────────────────────────

export default function CompanySettings() {
  const [activeTab, setActiveTab] = useState<Tab>('company')

  const { data: company, isLoading } = useQuery({
    queryKey: settingsKeys.company,
    queryFn: settingsApi.getCompany,
  })

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Hero */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 p-6 shadow-lg">
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }} />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="mb-1 flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
                <Building2 className="h-5 w-5 text-white" />
              </div>
              <h1 className="text-2xl font-bold tracking-tight text-white">Entreprise</h1>
            </div>
            <p className="text-sm text-sky-200">Informations société et dépôts</p>
          </div>
          {!isLoading && company && (
            <div className="flex flex-wrap items-center gap-3">
              <StatChip icon={<Building2 className="h-3.5 w-3.5" />} label={company.companyName ?? company.name} />
              <StatChip icon={<CheckCircle2 className="h-3.5 w-3.5" />} label={company.planType} />
            </div>
          )}
        </div>
      </div>

      {/* Tabs */}
      <div className="flex border-b">
        <button
          onClick={() => setActiveTab('company')}
          className={cn(
            'px-4 py-2.5 text-sm font-medium transition-colors border-b-2 -mb-px',
            activeTab === 'company'
              ? 'border-sky-500 text-sky-600'
              : 'border-transparent text-slate-500 hover:text-slate-700',
          )}
        >
          Informations société
        </button>
        <button
          onClick={() => setActiveTab('depots')}
          className={cn(
            'px-4 py-2.5 text-sm font-medium transition-colors border-b-2 -mb-px',
            activeTab === 'depots'
              ? 'border-sky-500 text-sky-600'
              : 'border-transparent text-slate-500 hover:text-slate-700',
          )}
        >
          Dépôts
        </button>
      </div>

      {/* Tab content */}
      {activeTab === 'company' && (
        <div className="grid gap-6 lg:grid-cols-2">
          {/* Company info */}
          <div className="overflow-hidden rounded-2xl border bg-white shadow-sm">
            <div className="border-b bg-slate-50 px-5 py-4">
              <h2 className="font-semibold text-slate-800">Informations générales</h2>
              <p className="text-xs text-slate-400">Coordonnées et contact de la société</p>
            </div>
            <div className="p-5">
              {isLoading ? (
                <div className="space-y-4">
                  {[...Array(5)].map((_, i) => <Skeleton key={i} className="h-9 w-full rounded-lg" />)}
                </div>
              ) : (
                <CompanyInfoForm initialData={{
                  companyName: company?.companyName ?? '',
                  address: company?.address ?? '',
                  zipCode: company?.zipCode ?? '',
                  city: company?.city ?? '',
                  phone: company?.phone ?? '',
                  email: company?.email ?? '',
                  website: company?.website ?? '',
                }} />
              )}
            </div>
          </div>

          {/* Legal info */}
          <div className="overflow-hidden rounded-2xl border bg-white shadow-sm">
            <div className="border-b bg-slate-50 px-5 py-4">
              <h2 className="font-semibold text-slate-800">Informations légales</h2>
              <p className="text-xs text-slate-400">SIRET, TVA et mentions obligatoires</p>
            </div>
            <div className="p-5">
              {isLoading ? (
                <div className="space-y-4">
                  {[...Array(5)].map((_, i) => <Skeleton key={i} className="h-9 w-full rounded-lg" />)}
                </div>
              ) : (
                <LegalInfoForm initialData={{
                  siret: company?.siret ?? '',
                  vatNumber: company?.vatNumber ?? '',
                  legalForm: company?.legalForm ?? '',
                  legalMentions: company?.legalMentions ?? '',
                  bankDetails: company?.bankDetails ?? '',
                }} />
              )}
            </div>
          </div>
        </div>
      )}

      {activeTab === 'depots' && <DepotsTab />}
    </div>
  )
}
