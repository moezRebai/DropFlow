import { useEffect, useRef, useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { useForm, useFieldArray, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import {
  ArrowLeft, Plus, Trash2, Search, X, User, MapPin,
  FileText, Package, Settings2, Euro, Route,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import { useMapsLibrary } from '@vis.gl/react-google-maps'
import { deliveriesApi, deliveryKeys, DeliveryStatus, DeliveryType, STATUS_LABELS } from '@/api/deliveries'
import { emitDeliveryUpdated } from '@/hooks/useDeliveryBroadcast'
import { storesApi, storeKeys } from '@/api/stores'
import { driversApi, driverKeys } from '@/api/drivers'
import { timeslotsApi, timeslotKeys } from '@/api/timeslots'
import { clientsApi, clientKeys, type ClientLookupDto } from '@/api/clients'
import { cn } from '@/lib/utils'

// ─── Schema ───────────────────────────────────────────────────────────────────

const itemSchema = z.object({
  id: z.number().optional(),
  reference: z.string().optional(),
  designation: z.string().min(1, 'Désignation requise'),
  quantity: z.number().min(1, 'Minimum 1'),
  information: z.string().optional(),
})

const schema = z.object({
  clientFirstName: z.string().min(1, 'Prénom requis'),
  clientLastName: z.string().min(1, 'Nom requis'),
  clientPhone: z.string().min(1, 'Téléphone requis'),
  clientEmail: z.string().email('Email invalide').optional().or(z.literal('')),
  addressLabel: z.string().optional(),
  address: z.string().min(1, 'Adresse requise'),
  zipCode: z.string().min(1, 'Code postal requis'),
  city: z.string().min(1, 'Ville requise'),
  addressComplement: z.string().optional(),
  storeId: z.coerce.number().min(1, 'Dépôt requis'),
  fileNumber: z.string().min(1, 'N° dossier requis'),
  scheduledDate: z.string().optional(),
  price: z.coerce.number().min(0.01, 'Prix requis (> 0)'),
  clientPaymentAmount: z.coerce.number().optional(),
  storePaymentAmount: z.coerce.number().optional(),
  status: z.nativeEnum(DeliveryStatus),
  type: z.nativeEnum(DeliveryType),
  urgentDriverId: z.coerce.number().optional(),
  timeSlotId: z.coerce.number().optional(),
  withAssembly: z.boolean(),
  deliveryNotes: z.string().optional(),
  internalNotes: z.string().optional(),
  estimatedDurationMinutes: z.number({ required_error: 'Durée estimée requise' }).min(1, 'Durée estimée requise'),
  items: z.array(itemSchema).min(1, 'Au moins un article est obligatoire'),
}).superRefine((d, ctx) => {
  if (d.type === DeliveryType.Urgent) {
    if (!d.urgentDriverId) ctx.addIssue({ code: 'custom', path: ['urgentDriverId'], message: 'Chauffeur requis' })
    if (!d.scheduledDate) ctx.addIssue({ code: 'custom', path: ['scheduledDate'], message: 'Date requise' })
  }
  if (d.status !== DeliveryStatus.ToBePlanned && !d.scheduledDate) {
    ctx.addIssue({ code: 'custom', path: ['scheduledDate'], message: 'Date obligatoire pour ce statut' })
  }
  if (d.scheduledDate && d.status === DeliveryStatus.ToBePlanned) {
    ctx.addIssue({ code: 'custom', path: ['status'], message: "Incompatible avec une date planifiée — choisir un autre statut" })
  }
})

type FormData = z.infer<typeof schema>

// ─── Section wrapper ──────────────────────────────────────────────────────────

function FormSection({
  icon: Icon, title, color, children,
}: {
  icon: React.ElementType
  title: string
  color: string
  children: React.ReactNode
}) {
  return (
    <div className="overflow-hidden rounded-xl border bg-card shadow-sm">
      <div className={cn('flex items-center gap-2.5 border-b px-5 py-3.5', color)}>
        <Icon className="h-4 w-4" />
        <span className="text-sm font-semibold">{title}</span>
      </div>
      <div className="p-5">{children}</div>
    </div>
  )
}

// ─── Field ────────────────────────────────────────────────────────────────────

function Field({
  label, error, children, required, className,
}: {
  label: string
  error?: string
  children: React.ReactNode
  required?: boolean
  className?: string
}) {
  return (
    <div className={cn('space-y-1.5', className)}>
      <Label className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
        {label}{required && <span className="ml-0.5 text-destructive">*</span>}
      </Label>
      {children}
      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  )
}

// ─── Client search ────────────────────────────────────────────────────────────

function ClientSearch({ onSelect }: { onSelect: (c: ClientLookupDto) => void }) {
  const [query, setQuery] = useState('')
  const [open, setOpen] = useState(false)

  const { data, isFetching } = useQuery({
    queryKey: clientKeys.search(query),
    queryFn: () => clientsApi.search(query),
    enabled: query.length >= 2,
    staleTime: 10_000,
  })

  return (
    <div className="relative">
      <div className="relative">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          placeholder="Rechercher un client existant…"
          value={query}
          onChange={e => { setQuery(e.target.value); setOpen(true) }}
          onFocus={() => setOpen(true)}
          className="pl-9"
        />
        {query && (
          <button type="button" onClick={() => { setQuery(''); setOpen(false) }}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground">
            <X className="h-4 w-4" />
          </button>
        )}
      </div>
      {open && query.length >= 2 && (
        <div className="absolute z-50 mt-1 w-full rounded-xl border bg-popover shadow-lg">
          {isFetching ? (
            <div className="p-3 text-sm text-muted-foreground">Recherche…</div>
          ) : !data?.length ? (
            <div className="p-3 text-sm text-muted-foreground">Aucun client trouvé</div>
          ) : (
            <ul className="max-h-48 overflow-y-auto">
              {data.map(c => (
                <li key={c.id} className="cursor-pointer px-4 py-2.5 hover:bg-muted"
                  onMouseDown={e => { e.preventDefault(); onSelect(c); setQuery(''); setOpen(false) }}>
                  <p className="text-sm font-medium">{c.displayName}</p>
                  <p className="text-xs text-muted-foreground">{c.phone}{c.email ? ` · ${c.email}` : ''}</p>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
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

// ─── Items table ──────────────────────────────────────────────────────────────

function ItemsTable({ control, errors }: {
  control: ReturnType<typeof useForm<FormData>>['control']
  errors: ReturnType<typeof useForm<FormData>>['formState']['errors']
}) {
  const { fields, append, remove } = useFieldArray({ control, name: 'items' })
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const itemsRootError: string | undefined = (errors.items as any)?.root?.message ?? (errors.items as any)?.message

  return (
    <div className="space-y-3">
      {fields.length > 0 && (
        <div className="hidden grid-cols-[2fr_80px_1fr_1fr_36px] gap-2 text-xs font-medium uppercase tracking-wide text-muted-foreground sm:grid">
          <span>Désignation *</span><span>Qté *</span><span>Référence</span><span>Info</span><span />
        </div>
      )}
      {fields.map((field, i) => (
        <div key={field.id} className="grid grid-cols-1 gap-2 sm:grid-cols-[2fr_80px_1fr_1fr_36px]">
          <div>
            <Controller control={control} name={`items.${i}.designation`}
              render={({ field: f }) => (
                <Input placeholder="Désignation" {...f}
                  className={cn(errors.items?.[i]?.designation && 'border-destructive')} />
              )} />
            {errors.items?.[i]?.designation && (
              <p className="mt-0.5 text-xs text-destructive">{errors.items[i]?.designation?.message}</p>
            )}
          </div>
          <Controller control={control} name={`items.${i}.quantity`}
            render={({ field: f }) => (
              <Input type="number" min={1} placeholder="1" {...f}
                onChange={e => f.onChange(Number(e.target.value))} />
            )} />
          <Controller control={control} name={`items.${i}.reference`}
            render={({ field: f }) => <Input placeholder="Réf." {...f} value={f.value ?? ''} />} />
          <Controller control={control} name={`items.${i}.information`}
            render={({ field: f }) => <Input placeholder="Info" {...f} value={f.value ?? ''} />} />
          <Button type="button" variant="ghost" size="icon"
            onClick={() => remove(i)}
            className="h-9 w-9 text-muted-foreground hover:text-destructive">
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      ))}

      {fields.length === 0 && (
        <div className={cn(
          'flex flex-col items-center gap-2 rounded-lg border border-dashed py-6 text-muted-foreground',
          itemsRootError && 'border-destructive bg-destructive/5',
        )}>
          <Package className="h-8 w-8 opacity-30" />
          <p className="text-sm">Aucun article ajouté</p>
          {itemsRootError && <p className="text-xs font-medium text-destructive">{itemsRootError}</p>}
        </div>
      )}

      <Button type="button" variant="outline" size="sm"
        onClick={() => append({ designation: '', quantity: 1 })}>
        <Plus className="mr-1.5 h-4 w-4" />
        Ajouter un article
      </Button>
    </div>
  )
}

// ─── Main ─────────────────────────────────────────────────────────────────────

export default function CreateDeliveryPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const isEdit = Boolean(id)
  const deliveryId = Number(id)

  const [serverError, setServerError] = useState<string | null>(null)

  const { data: stores = [] } = useQuery({ queryKey: storeKeys.lookup, queryFn: storesApi.getLookup, staleTime: 5 * 60_000 })
  const { data: drivers = [] } = useQuery({ queryKey: driverKeys.active, queryFn: driversApi.getActive, staleTime: 5 * 60_000 })
  const { data: timeslots = [] } = useQuery({ queryKey: timeslotKeys.all, queryFn: timeslotsApi.getAll, staleTime: 5 * 60_000 })

  const { data: existingResult, isLoading: isLoadingDelivery } = useQuery({
    queryKey: deliveryKeys.detail(deliveryId),
    queryFn: () => deliveriesApi.getById(deliveryId),
    enabled: isEdit && !isNaN(deliveryId),
  })
  const existing = existingResult?.data

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      clientFirstName: '', clientLastName: '', clientPhone: '', clientEmail: '',
      address: '', zipCode: '', city: '',
      storeId: 0, fileNumber: '', price: 0,
      status: DeliveryStatus.ToBePlanned,
      type: DeliveryType.Standard,
      withAssembly: false, items: [],
    },
  })

  const { register, control, handleSubmit, watch, setValue, formState: { errors, isSubmitting } } = form
  const watchedType = watch('type')

  useEffect(() => {
    if (!existing) return
    if (existing.status === DeliveryStatus.Delivered) {
      navigate(`/deliveries/${deliveryId}`, { replace: true })
      return
    }
    form.reset({
      clientFirstName: existing.clientName.split(' ')[0] ?? '',
      clientLastName: existing.clientName.split(' ').slice(1).join(' ') ?? '',
      clientPhone: existing.clientPhone,
      clientEmail: existing.clientEmail ?? '',
      addressLabel: existing.addressLabel ?? '',
      address: existing.address,
      zipCode: existing.zipCode,
      city: existing.city,
      addressComplement: existing.addressComplement ?? '',
      storeId: existing.storeId,
      fileNumber: existing.fileNumber,
      scheduledDate: existing.scheduledDate ? existing.scheduledDate.substring(0, 10) : '',
      price: existing.price,
      clientPaymentAmount: existing.clientPaymentAmount ?? undefined,
      storePaymentAmount: existing.storePaymentAmount ?? undefined,
      status: existing.status,
      type: existing.type,
      urgentDriverId: existing.urgentDriverId ?? undefined,
      timeSlotId: existing.timeSlotId ?? undefined,
      withAssembly: existing.withAssembly,
      deliveryNotes: existing.deliveryNotes ?? '',
      internalNotes: existing.internalNotes ?? '',
      estimatedDurationMinutes: existing.estimatedDurationMinutes ?? undefined,
      items: existing.items.map(it => ({
        id: it.id, designation: it.designation, quantity: it.quantity,
        reference: it.reference ?? '', information: it.information ?? '',
      })),
    })
  }, [existing, form])

  function handleClientSelect(c: ClientLookupDto) {
    const [first, ...rest] = c.displayName.split(' ')
    setValue('clientFirstName', first ?? '')
    setValue('clientLastName', rest.join(' '))
    setValue('clientPhone', c.phone)
    setValue('clientEmail', c.email ?? '')
    const def = c.addresses.find(a => a.isDefault) ?? c.addresses[0]
    if (def) {
      setValue('address', def.address)
      setValue('zipCode', def.zipCode)
      setValue('city', def.city)
    }
  }

  const createMutation = useMutation({
    mutationFn: (data: FormData) => deliveriesApi.create({
      ...data,
      clientEmail: data.clientEmail || undefined,
      scheduledDate: data.scheduledDate || undefined,
      urgentDriverId: data.type === DeliveryType.Urgent ? data.urgentDriverId : undefined,
      timeSlotId: data.timeSlotId || undefined,
    }),
    onSuccess: (res) => {
      toast.success('Livraison créée')
      queryClient.invalidateQueries({ queryKey: deliveryKeys.lists() })
      if (res.data) emitDeliveryUpdated(res.data)
      navigate(res.data ? `/deliveries/${res.data}` : '/deliveries')
    },
    onError: (err) => {
      if (isAxiosError(err)) {
        const d = err.response?.data
        setServerError(d?.errors?.[0] ?? d?.message ?? 'Erreur lors de la création')
      } else {
        setServerError('Erreur lors de la création')
      }
    },
  })

  const updateMutation = useMutation({
    mutationFn: (data: FormData) => deliveriesApi.update(deliveryId, {
      ...data,
      clientEmail: data.clientEmail || undefined,
      scheduledDate: data.scheduledDate || undefined,
      urgentDriverId: data.type === DeliveryType.Urgent ? data.urgentDriverId : undefined,
      timeSlotId: data.timeSlotId || undefined,
      items: data.items.map(it => ({ ...it, id: it.id ?? undefined })),
    }),
    onSuccess: () => {
      toast.success('Livraison mise à jour')
      queryClient.invalidateQueries({ queryKey: deliveryKeys.lists() })
      queryClient.invalidateQueries({ queryKey: deliveryKeys.detail(deliveryId) })
      emitDeliveryUpdated(deliveryId)
      navigate('/deliveries')
    },
    onError: (err) => {
      if (isAxiosError(err)) {
        const d = err.response?.data
        setServerError(d?.errors?.[0] ?? d?.message ?? 'Erreur lors de la mise à jour')
      } else {
        setServerError('Erreur lors de la mise à jour')
      }
    },
  })

  async function onSubmit(data: FormData) {
    setServerError(null)
    if (isEdit) updateMutation.mutate(data)
    else createMutation.mutate(data)
  }

  if (isEdit && isLoadingDelivery) {
    return <div className="space-y-4 p-6"><Skeleton className="h-14" /><Skeleton className="h-64" /><Skeleton className="h-48" /></div>
  }

  const isPending = isSubmitting || createMutation.isPending || updateMutation.isPending

  return (
    <div className="flex h-full flex-col">
      {/* ── Sticky header ─────────────────────────────────────────────────── */}
      <div className="flex shrink-0 items-center gap-3 border-b bg-card px-6 py-3.5">
        <Button type="button" variant="ghost" size="icon" onClick={() => navigate(-1)}>
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <div className="flex-1">
          <h1 className="text-lg font-bold">
            {isEdit ? 'Modifier la livraison' : 'Nouvelle livraison'}
          </h1>
          {isEdit && existing && (
            <p className="text-xs text-muted-foreground">{existing.reference}</p>
          )}
        </div>
        <Button type="button" variant="outline" size="sm" asChild>
          <Link to={isEdit ? `/deliveries/${deliveryId}` : '/deliveries'}>Annuler</Link>
        </Button>
        <Button size="sm" disabled={isPending} onClick={handleSubmit(onSubmit)}>
          {isPending ? (
            <span className="flex items-center gap-2">
              <span className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-current border-t-transparent" />
              {isEdit ? 'Enregistrement…' : 'Création…'}
            </span>
          ) : (
            isEdit ? 'Enregistrer' : 'Créer la livraison'
          )}
        </Button>
      </div>

      {/* ── Scrollable body ───────────────────────────────────────────────── */}
      <div className="flex-1 overflow-y-auto">
        <div className="mx-auto max-w-3xl space-y-4 p-6">
          {serverError && (
            <Alert variant="destructive">
              <AlertDescription>{serverError}</AlertDescription>
            </Alert>
          )}

          {/* ── Client ──────────────────────────────────────────────────── */}
          <FormSection icon={User} title="Client" color="bg-blue-50 text-blue-700 dark:bg-blue-950/40 dark:text-blue-300">
            <div className="space-y-4">
              <ClientSearch onSelect={handleClientSelect} />
              <Separator />
              <div className="grid grid-cols-2 gap-3">
                <Field label="Prénom" required error={errors.clientFirstName?.message}>
                  <Input {...register('clientFirstName')} />
                </Field>
                <Field label="Nom" required error={errors.clientLastName?.message}>
                  <Input {...register('clientLastName')} />
                </Field>
                <Field label="Téléphone" required error={errors.clientPhone?.message}>
                  <Input type="tel" {...register('clientPhone')} />
                </Field>
                <Field label="Email" error={errors.clientEmail?.message}>
                  <Input type="email" {...register('clientEmail')} />
                </Field>
              </div>
            </div>
          </FormSection>

          {/* ── Adresse ─────────────────────────────────────────────────── */}
          <FormSection icon={MapPin} title="Adresse de livraison" color="bg-violet-50 text-violet-700 dark:bg-violet-950/40 dark:text-violet-300">
            <div className="space-y-3">
              <Field label="Libellé (optionnel)">
                <Input placeholder="Bureau, Domicile…" {...register('addressLabel')} />
              </Field>
              <Field label="Adresse" required error={errors.address?.message}>
                <Controller
                  control={control}
                  name="address"
                  render={({ field }) => (
                    <AddressAutocomplete
                      value={field.value}
                      onChange={field.onChange}
                      onPlaceSelect={({ address, zipCode, city }) => {
                        setValue('address', address, { shouldValidate: true })
                        setValue('zipCode', zipCode, { shouldValidate: true })
                        setValue('city', city, { shouldValidate: true })
                      }}
                      error={errors.address?.message}
                    />
                  )}
                />
              </Field>
              <div className="grid grid-cols-3 gap-3">
                <Field label="Code postal" required error={errors.zipCode?.message}>
                  <Input {...register('zipCode')} />
                </Field>
                <Field label="Ville" required error={errors.city?.message} className="col-span-2">
                  <Input {...register('city')} />
                </Field>
              </div>
              <Field label="Complément">
                <Input {...register('addressComplement')} />
              </Field>
            </div>
          </FormSection>

          {/* ── Livraison ───────────────────────────────────────────────── */}
          <FormSection icon={FileText} title="Détails de la livraison" color="bg-amber-50 text-amber-700 dark:bg-amber-950/40 dark:text-amber-300">
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-3">
                <Field label="Enseigne" required error={errors.storeId?.message}>
                  <Controller control={control} name="storeId"
                    render={({ field }) => (
                      <Select value={String(field.value || '')} onValueChange={v => field.onChange(Number(v))}>
                        <SelectTrigger><SelectValue placeholder="Sélectionner…" /></SelectTrigger>
                        <SelectContent>
                          {stores.map(s => (
                            <SelectItem key={s.id} value={String(s.id)}>
                              {s.name} — {s.city}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )} />
                </Field>
                <Field label="N° dossier" required error={errors.fileNumber?.message}>
                  <Input {...register('fileNumber')} />
                </Field>
                <Field label="Date de livraison" error={errors.scheduledDate?.message}>
                  <Input type="date" {...register('scheduledDate')} />
                </Field>
                <Field label="Statut" required error={errors.status?.message}>
                  <Controller control={control} name="status"
                    render={({ field }) => (
                      <Select value={String(field.value)} onValueChange={v => field.onChange(Number(v))}>
                        <SelectTrigger><SelectValue /></SelectTrigger>
                        <SelectContent>
                          {Object.values(DeliveryStatus)
                            .filter((v): v is DeliveryStatus => typeof v === 'number')
                            .map(s => <SelectItem key={s} value={String(s)}>{STATUS_LABELS[s]}</SelectItem>)}
                        </SelectContent>
                      </Select>
                    )} />
                </Field>
                <Field label="Durée estimée" required error={errors.estimatedDurationMinutes?.message}>
                  <Controller control={control} name="estimatedDurationMinutes"
                    render={({ field }) => {
                      const PRESETS = [5,10,15,20,30,45,60,90,120,180,240,300,360,420,480]
                      const fmt = (m: number) =>
                        m < 60 ? `${m} min` : m % 60 === 0 ? `${m / 60}h` : `${Math.floor(m / 60)}h${m % 60}`
                      const isCustom = field.value != null && !PRESETS.includes(field.value)
                      return (
                        <Select value={field.value ? String(field.value) : ''} onValueChange={v => field.onChange(v ? Number(v) : undefined)}>
                          <SelectTrigger><SelectValue placeholder="Non définie" /></SelectTrigger>
                          <SelectContent>
                            {isCustom && (
                              <SelectItem value={String(field.value)}>
                                {fmt(field.value!)} (actuel)
                              </SelectItem>
                            )}
                            {PRESETS.map(m => (
                              <SelectItem key={m} value={String(m)}>{fmt(m)}</SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      )
                    }} />
                </Field>
                <Field label="Créneau d'intervention">
                  <Controller control={control} name="timeSlotId"
                    render={({ field }) => (
                      <Select value={field.value ? String(field.value) : ''} onValueChange={v => field.onChange(v ? Number(v) : undefined)}>
                        <SelectTrigger><SelectValue placeholder="Non défini" /></SelectTrigger>
                        <SelectContent>
                          {timeslots.map(ts => (
                            <SelectItem key={ts.id} value={String(ts.id)}>
                              {ts.name} ({ts.startTime}–{ts.endTime})
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )} />
                </Field>
              </div>

              {/* Montage + Tournée (edit) */}
              <div className="flex flex-wrap items-center gap-6">
                <label className="flex cursor-pointer items-center gap-2.5">
                  <Controller control={control} name="withAssembly"
                    render={({ field }) => (
                      <Checkbox checked={field.value} onCheckedChange={field.onChange} />
                    )} />
                  <span className="text-sm font-medium">Avec montage</span>
                </label>
                {isEdit && existing?.routeReference && (
                  <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
                    <Route className="h-4 w-4 text-green-600" />
                    <span>Tournée :</span>
                    <Badge variant="secondary" className="font-mono text-xs">{existing.routeReference}</Badge>
                  </div>
                )}
              </div>

              <Separator />

              {/* Type */}
              <div>
                <Label className="mb-2 block text-xs font-medium uppercase tracking-wide text-muted-foreground">
                  Type de livraison
                </Label>
                <div className="flex gap-3">
                  {([DeliveryType.Standard, DeliveryType.Urgent] as const).map(t => (
                    <button
                      key={t}
                      type="button"
                      onClick={() => setValue('type', t)}
                      className={cn(
                        'flex-1 rounded-lg border px-4 py-2.5 text-sm font-medium transition-all',
                        watchedType === t
                          ? t === DeliveryType.Urgent
                            ? 'border-red-400 bg-red-50 text-red-700 dark:bg-red-950/40 dark:text-red-300'
                            : 'border-primary bg-primary/5 text-primary'
                          : 'text-muted-foreground hover:bg-muted',
                      )}
                    >
                      {t === DeliveryType.Standard ? 'Standard' : '⚡ Urgente'}
                    </button>
                  ))}
                </div>
              </div>

              {watchedType === DeliveryType.Urgent && (
                <div className="rounded-lg border border-red-200 bg-red-50 p-3 dark:border-red-900 dark:bg-red-950/30">
                  <Field label="Chauffeur urgent" required error={errors.urgentDriverId?.message}>
                    <Controller control={control} name="urgentDriverId"
                      render={({ field }) => (
                        <Select value={field.value ? String(field.value) : ''} onValueChange={v => field.onChange(Number(v))}>
                          <SelectTrigger className="bg-background"><SelectValue placeholder="Sélectionner…" /></SelectTrigger>
                          <SelectContent>
                            {drivers.map(d => (
                              <SelectItem key={d.id} value={String(d.id)}>{d.firstName} {d.lastName}</SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      )} />
                  </Field>
                </div>
              )}
            </div>
          </FormSection>

          {/* ── Financier ───────────────────────────────────────────────── */}
          <FormSection icon={Euro} title="Financier" color="bg-green-50 text-green-700 dark:bg-green-950/40 dark:text-green-300">
            <div className="grid grid-cols-3 gap-3">
              <Field label="Prix (€)" required error={errors.price?.message}>
                <Input type="number" step="0.01" min="0" {...register('price')} />
              </Field>
              <Field label="Paiement client (€)">
                <Input type="number" step="0.01" min="0" {...register('clientPaymentAmount')} />
              </Field>
              <Field label="Paiement dépôt (€)">
                <Input type="number" step="0.01" min="0" {...register('storePaymentAmount')} />
              </Field>
            </div>
          </FormSection>

          {/* ── Options ─────────────────────────────────────────────────── */}
          <FormSection icon={Settings2} title="Notes" color="bg-slate-50 text-slate-600 dark:bg-slate-900/40 dark:text-slate-300">
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-3">
                <Field label="Note chauffeur">
                  <textarea
                    {...register('deliveryNotes')}
                    rows={3}
                    placeholder="Visible par le chauffeur…"
                    className="w-full rounded-md border bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  />
                </Field>
                <Field label="Note interne">
                  <textarea
                    {...register('internalNotes')}
                    rows={3}
                    placeholder="Équipe uniquement…"
                    className="w-full rounded-md border bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  />
                </Field>
              </div>
            </div>
          </FormSection>

          {/* ── Articles ────────────────────────────────────────────────── */}
          <FormSection icon={Package} title="Articles" color="bg-teal-50 text-teal-700 dark:bg-teal-950/40 dark:text-teal-300">
            <ItemsTable control={control} errors={errors} />
          </FormSection>

          {/* Bottom spacing */}
          <div className="h-4" />
        </div>
      </div>
    </div>
  )
}
