import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useQuery } from '@tanstack/react-query'
import { MapPin, Truck, Clock, Building2, ChevronDown, Package, AlertCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { vehiclesApi, vehicleKeys } from '@/api/vehicles'
import { settingsApi, settingsKeys } from '@/api/settings'
import { deliveriesApi, deliveryKeys } from '@/api/deliveries'
import { useWizardStore } from '@/store/wizardStore'
import { cn } from '@/lib/utils'

const schema = z.object({
  date: z.string().min(1, 'La date est requise'),
  vehicleId: z.coerce.number().min(1, 'Un véhicule est requis'),
  startTime: z.string().min(1, 'L\'heure de départ est requise'),
  departureMode: z.enum(['depot', 'free']),
  depotId: z.coerce.number().optional(),
  freeAddress: z.string().optional(),
}).refine(data => {
  if (data.departureMode === 'depot') return (data.depotId ?? 0) > 0
  return (data.freeAddress ?? '').trim().length > 0
}, { message: 'L\'adresse de départ est requise', path: ['freeAddress'] })

type FormValues = z.infer<typeof schema>

export function Step1Info() {
  const wizard = useWizardStore()

  const { data: vehicles = [] } = useQuery({
    queryKey: [...vehicleKeys.lists(), { isActive: true }],
    queryFn: () => vehiclesApi.getList({ isActive: true, pageSize: 200 }).then(r => r.items),
  })

  const { data: depots = [] } = useQuery({
    queryKey: settingsKeys.depots.all,
    queryFn: settingsApi.getAllDepots,
  })

  const activeDepots = depots.filter(d => d.isActive)
  const defaultDepot = activeDepots.find(d => d.isDefault)

  const {
    register, handleSubmit, watch, setValue,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      date: wizard.date || new Date().toISOString().substring(0, 10),
      vehicleId: wizard.vehicleId ?? 0,
      startTime: wizard.startTime || '08:00',
      departureMode: 'depot',
      freeAddress: wizard.departureAddress || '',
    },
  })

  const departureMode = watch('departureMode')
  const depotId = watch('depotId')
  const selectedDate = watch('date')

  const { data: availableResult, isFetching: checkingDeliveries } = useQuery({
    queryKey: deliveryKeys.availableForRoute(selectedDate),
    queryFn: () => deliveriesApi.availableForRoute(selectedDate),
    enabled: selectedDate?.length === 10,
    select: r => r.data ?? [],
  })
  const availableCount = availableResult?.length ?? null

  useEffect(() => {
    if (wizard.vehicleId && vehicles.some(v => v.id === wizard.vehicleId)) {
      setValue('vehicleId', wizard.vehicleId)
    }
  }, [wizard.vehicleId, vehicles, setValue])

  // Edit mode: match stored address back to a depot (handles both old "street only" and new "street, CP city" formats)
  useEffect(() => {
    if (!depots.length) return
    if (wizard.departureAddress) {
      const match = depots.find(d => {
        const zipCity = [d.zipCode, d.city].filter(Boolean).join(' ')
        const full = [d.fullAddress, zipCity].filter(Boolean).join(', ')
        return full === wizard.departureAddress || d.fullAddress === wizard.departureAddress
      })
      if (match) {
        setValue('departureMode', 'depot')
        setValue('depotId', match.id)
      } else {
        setValue('departureMode', 'free')
        setValue('freeAddress', wizard.departureAddress)
      }
    } else if (departureMode === 'depot' && !depotId && defaultDepot) {
      setValue('depotId', defaultDepot.id)
    }
  }, [depots, wizard.departureAddress, defaultDepot, departureMode, depotId, setValue])

  function onSubmit(values: FormValues) {
    let departureAddress = ''
    let departureLatitude: number | null = null
    let departureLongitude: number | null = null

    if (values.departureMode === 'depot' && values.depotId) {
      const depot = depots.find(d => d.id === values.depotId)
      if (depot) {
        const zipCity = [depot.zipCode, depot.city].filter(Boolean).join(' ')
        departureAddress = [depot.fullAddress, zipCity].filter(Boolean).join(', ')
        departureLatitude = depot.latitude ?? null
        departureLongitude = depot.longitude ?? null
      }
    } else {
      departureAddress = values.freeAddress ?? ''
    }

    const vehicle = vehicles.find(v => v.id === values.vehicleId)

    wizard.setStep1({
      date: values.date,
      vehicleId: values.vehicleId,
      vehicleName: vehicle?.brand ? `${vehicle.brand} ${vehicle.model}` : `Véhicule ${values.vehicleId}`,
      startTime: values.startTime,
      departureAddress,
      departureLatitude,
      departureLongitude,
    })
    wizard.next()
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-6">

      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">

        {/* Date */}
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="date" className="flex items-center gap-1.5 text-sm font-medium">
            <Clock className="h-3.5 w-3.5 text-muted-foreground" />Date de la tournée
          </Label>
          <Input id="date" type="date" {...register('date')} />
          {errors.date && <p className="text-xs text-red-500">{errors.date.message}</p>}
          {selectedDate?.length === 10 && (
            checkingDeliveries ? (
              <Skeleton className="h-7 w-48 rounded-lg" />
            ) : availableCount === 0 ? (
              <div className="flex items-center gap-1.5 rounded-lg bg-red-50 px-3 py-1.5 text-xs font-medium text-red-600 dark:bg-red-500/10 dark:text-red-400">
                <AlertCircle className="h-3.5 w-3.5 shrink-0" />
                Aucune livraison disponible pour cette date
              </div>
            ) : availableCount !== null ? (
              <div className="flex items-center gap-1.5 rounded-lg bg-emerald-50 px-3 py-1.5 text-xs font-medium text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400">
                <Package className="h-3.5 w-3.5 shrink-0" />
                {availableCount} livraison{availableCount > 1 ? 's' : ''} disponible{availableCount > 1 ? 's' : ''} pour cette date
              </div>
            ) : null
          )}
        </div>

        {/* Start time */}
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="startTime" className="flex items-center gap-1.5 text-sm font-medium">
            <Clock className="h-3.5 w-3.5 text-muted-foreground" />Heure de départ
          </Label>
          <Input id="startTime" type="time" {...register('startTime')} />
          {errors.startTime && <p className="text-xs text-red-500">{errors.startTime.message}</p>}
        </div>

        {/* Vehicle */}
        <div className="flex flex-col gap-1.5 sm:col-span-2">
          <Label htmlFor="vehicleId" className="flex items-center gap-1.5 text-sm font-medium">
            <Truck className="h-3.5 w-3.5 text-muted-foreground" />Véhicule
          </Label>
          <div className="relative">
            <select
              id="vehicleId"
              {...register('vehicleId')}
              className={cn(
                'w-full appearance-none rounded-md border bg-background px-3 py-2 text-sm pr-8 focus:outline-none focus:ring-2 focus:ring-sky-500',
                errors.vehicleId && 'border-red-500'
              )}
            >
              <option value={0}>Sélectionner un véhicule…</option>
              {vehicles.map(v => (
                <option key={v.id} value={v.id}>
                  {v.brand} {v.model} — {v.plateNumber}
                  {v.maxDeliveries ? ` (max ${v.maxDeliveries} livraisons)` : ''}
                </option>
              ))}
            </select>
            <ChevronDown className="pointer-events-none absolute right-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          </div>
          {errors.vehicleId && <p className="text-xs text-red-500">{errors.vehicleId.message}</p>}
        </div>

        {/* Departure address */}
        <div className="flex flex-col gap-3 sm:col-span-2">
          <Label className="flex items-center gap-1.5 text-sm font-medium">
            <MapPin className="h-3.5 w-3.5 text-muted-foreground" />Adresse de départ
          </Label>

          {/* Mode toggle */}
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setValue('departureMode', 'depot')}
              aria-pressed={departureMode === 'depot'}
              className={cn(
                'flex items-center gap-1.5 rounded-lg border px-3 py-2 text-sm font-medium transition-colors',
                departureMode === 'depot'
                  ? 'border-sky-500 bg-sky-50 text-sky-700 dark:bg-sky-500/10 dark:text-sky-400'
                  : 'border-border bg-background text-muted-foreground hover:bg-muted'
              )}
            >
              <Building2 className="h-3.5 w-3.5" />Depuis un dépôt
            </button>
            <button
              type="button"
              onClick={() => setValue('departureMode', 'free')}
              aria-pressed={departureMode === 'free'}
              className={cn(
                'flex items-center gap-1.5 rounded-lg border px-3 py-2 text-sm font-medium transition-colors',
                departureMode === 'free'
                  ? 'border-sky-500 bg-sky-50 text-sky-700 dark:bg-sky-500/10 dark:text-sky-400'
                  : 'border-border bg-background text-muted-foreground hover:bg-muted'
              )}
            >
              <MapPin className="h-3.5 w-3.5" />Adresse libre
            </button>
          </div>

          {departureMode === 'depot' ? (
            <div className="relative">
              <select
                {...register('depotId')}
                className="w-full appearance-none rounded-md border bg-background px-3 py-2 text-sm pr-8 focus:outline-none focus:ring-2 focus:ring-sky-500"
              >
                <option value="">Sélectionner un dépôt…</option>
                {activeDepots.map(d => (
                  <option key={d.id} value={d.id}>
                    {d.name} — {d.fullAddress}{d.isDefault ? ' (défaut)' : ''}
                  </option>
                ))}
              </select>
              <ChevronDown className="pointer-events-none absolute right-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            </div>
          ) : (
            <Input
              {...register('freeAddress')}
              placeholder="Ex. 12 rue de la Paix, 75001 Paris"
            />
          )}
          {errors.freeAddress && <p className="text-xs text-red-500">{errors.freeAddress.message}</p>}
        </div>
      </div>

      <div className="flex justify-end pt-2">
        <Button
          type="submit"
          className="bg-sky-600 hover:bg-sky-700 text-white px-6"
          disabled={availableCount === 0 || checkingDeliveries}
        >
          {checkingDeliveries ? (
            <span className="flex items-center gap-2">
              <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
              Vérification…
            </span>
          ) : 'Étape suivante →'}
        </Button>
      </div>
    </form>
  )
}
