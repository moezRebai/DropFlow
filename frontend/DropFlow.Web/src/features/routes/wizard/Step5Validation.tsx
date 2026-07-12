import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import {
  Calendar, Truck, Clock, MapPin, Users, Navigation,
  Package, CheckCircle, AlertTriangle, ArrowRight, Timer,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { routesApi, routeKeys, TeamMemberRole, TEAM_ROLE_LABELS } from '@/api/routes'
import type { CreateRouteDto, CreateDeliverySequenceDto, UpdateRouteDto, UpdateDeliverySequenceDto } from '@/api/routes'
import { useWizardStore, formatDistance, formatDuration, getMainDriver, computeTimeline, minutesToTime } from '@/store/wizardStore'

function SectionTitle({ icon, children }: { icon: React.ReactNode; children: React.ReactNode }) {
  return (
    <h3 className="flex items-center gap-2 text-sm font-semibold text-foreground mb-2">
      <span className="text-muted-foreground">{icon}</span>{children}
    </h3>
  )
}

function InfoRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex items-start gap-2">
      <span className="w-36 shrink-0 text-xs text-muted-foreground">{label}</span>
      <span className="text-sm font-medium text-foreground">{value}</span>
    </div>
  )
}

export function Step5Validation() {
  const wizard = useWizardStore()
  const navigate = useNavigate()
  const qc = useQueryClient()

  const [confirmAfterCreate, setConfirmAfterCreate] = useState(false)
  const isEdit = !!wizard.editRouteId

  const mainDriver = getMainDriver(wizard.team)
  const helpers = wizard.team.filter(t => t.role === TeamMemberRole.Helper)

  function buildTimeline() {
    const serviceMap = new Map(wizard.selectedDeliveries.map(d => [d.id, d.estimatedDurationMinutes ?? 0]))
    const hasTimeline = wizard.optimizedDeliveries.some(d => d.durationToNextMinutes > 0)
    return hasTimeline && wizard.startTime
      ? computeTimeline(wizard.optimizedDeliveries, wizard.startTime, serviceMap)
      : []
  }

  function buildCreateDto(): CreateRouteDto {
    const timeline = buildTimeline()
    const deliveries: CreateDeliverySequenceDto[] = wizard.optimizedDeliveries.map((od, idx) => ({
      deliveryId: od.deliveryId,
      sequenceOrder: od.sequenceOrder,
      distanceToNextMeters: od.distanceToNextMeters || undefined,
      travelDurationMinutes: od.durationToNextMinutes || undefined,
      estimatedArrivalTime: timeline[idx] ? `${minutesToTime(timeline[idx].arrivalMinutes)}:00` : undefined,
    }))
    return {
      date: wizard.date,
      vehicleId: wizard.vehicleId!,
      startTime: wizard.startTime || undefined,
      departureAddress: wizard.departureAddress,
      departureLatitude: wizard.departureLatitude ?? undefined,
      departureLongitude: wizard.departureLongitude ?? undefined,
      team: wizard.team.map(t => ({ driverId: t.driverId, role: t.role })),
      deliveries,
      totalDistance: wizard.totalDistanceKm !== null ? wizard.totalDistanceKm : undefined,
      totalDuration: wizard.totalDurationMinutes !== null ? wizard.totalDurationMinutes : undefined,
      wasOptimizedByGoogle: wizard.wasOptimizedByGoogle,
      wasManuallyReordered: wizard.wasManuallyReordered,
    }
  }

  function buildUpdateDto(): UpdateRouteDto {
    const timeline = buildTimeline()
    const deliveries: UpdateDeliverySequenceDto[] = wizard.optimizedDeliveries.map((od, idx) => ({
      deliveryId: od.deliveryId,
      sequenceOrder: od.sequenceOrder,
      distanceToNextMeters: od.distanceToNextMeters || undefined,
      travelDurationMinutes: od.durationToNextMinutes || undefined,
      estimatedArrivalTime: timeline[idx] ? `${minutesToTime(timeline[idx].arrivalMinutes)}:00` : undefined,
    }))
    return {
      date: wizard.date,
      vehicleId: wizard.vehicleId!,
      startTime: `${wizard.startTime}:00`,
      departureAddress: wizard.departureAddress,
      departureLatitude: wizard.departureLatitude ?? undefined,
      departureLongitude: wizard.departureLongitude ?? undefined,
      team: wizard.team.map(t => ({ driverId: t.driverId, role: t.role })),
      deliveries,
      totalDistance: wizard.totalDistanceKm ?? 0,
      totalDuration: wizard.totalDurationMinutes ?? 0,
      wasOptimizedByGoogle: wizard.wasOptimizedByGoogle,
      wasManuallyReordered: wizard.wasManuallyReordered,
    }
  }

  const createMutation = useMutation({
    mutationFn: async () => {
      if (isEdit) {
        await routesApi.update(wizard.editRouteId!, buildUpdateDto()) // 204 No Content — throws on error
        if (confirmAfterCreate) {
          const cr = await routesApi.confirm(wizard.editRouteId!)
          if (!cr.succeeded) toast.warning('Mise à jour effectuée mais non confirmée : ' + (cr.message ?? ''))
        }
        return wizard.editRouteId!
      }

      const createDto = buildCreateDto()
      const result = await routesApi.create(createDto)
      if (!result.succeeded || !result.data) throw new Error(result.errors?.[0] ?? result.message ?? 'Erreur lors de la création')
      const routeId = result.data
      if (confirmAfterCreate) {
        const cr = await routesApi.confirm(routeId)
        if (!cr.succeeded) toast.warning('Tournée créée mais non confirmée : ' + (cr.message ?? ''))
      }
      return routeId
    },
    onSuccess: (routeId) => {
      toast.success(
        isEdit
          ? (confirmAfterCreate ? 'Tournée mise à jour et confirmée' : 'Tournée mise à jour')
          : (confirmAfterCreate ? 'Tournée créée et confirmée' : 'Tournée créée')
      )
      qc.invalidateQueries({ queryKey: routeKeys.lists() })
      wizard.reset()
      navigate(isEdit ? `/routes/${routeId}` : '/routes')
    },
    onError: (err) => {
      const message = isAxiosError(err)
        ? (err.response?.data?.errors?.[0] ?? err.response?.data?.message ?? err.message)
        : (err instanceof Error ? err.message : 'Erreur')
      toast.error(message)
    },
  })

  const totalDistance = wizard.totalDistanceKm !== null ? formatDistance(wizard.totalDistanceKm * 1000) : '—'
  const totalDuration = wizard.totalDurationMinutes !== null ? formatDuration(wizard.totalDurationMinutes) : '—'

  const serviceMap = new Map(
    wizard.selectedDeliveries.map(d => [d.id, d.estimatedDurationMinutes ?? 0])
  )
  const hasTimeline = wizard.optimizedDeliveries.some(d => d.durationToNextMinutes > 0)
  const timeline = hasTimeline && wizard.startTime
    ? computeTimeline(wizard.optimizedDeliveries, wizard.startTime, serviceMap)
    : []

  const endTime = wizard.startTime && wizard.totalDurationMinutes !== null
    ? (() => {
        const [h, m] = wizard.startTime.split(':').map(Number)
        return minutesToTime(h * 60 + m + wizard.totalDurationMinutes!)
      })()
    : null

  return (
    <div className="flex flex-col gap-6">

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">

        {/* Route info */}
        <div className="rounded-xl border bg-card p-4 shadow-sm">
          <SectionTitle icon={<Calendar className="h-4 w-4" />}>Informations générales</SectionTitle>
          <div className="flex flex-col gap-2">
            <InfoRow label="Date" value={new Date(wizard.date).toLocaleDateString('fr-FR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })} />
            <InfoRow label="Véhicule" value={
              <span className="flex items-center gap-1"><Truck className="h-3.5 w-3.5 text-muted-foreground" />{wizard.vehicleName}</span>
            } />
            <InfoRow label="Heure de départ" value={
              <span className="flex items-center gap-1"><Clock className="h-3.5 w-3.5 text-muted-foreground" />{wizard.startTime}</span>
            } />
            <InfoRow label="Adresse de départ" value={
              <span className="flex items-center gap-1 text-xs"><MapPin className="h-3.5 w-3.5 text-muted-foreground shrink-0" />{wizard.departureAddress}</span>
            } />
          </div>
        </div>

        {/* Team */}
        <div className="rounded-xl border bg-card p-4 shadow-sm">
          <SectionTitle icon={<Users className="h-4 w-4" />}>Équipe</SectionTitle>
          <div className="flex flex-col gap-2">
            {mainDriver ? (
              <div className="flex items-center gap-2">
                <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-sky-400 to-blue-600 text-xs font-bold text-white">
                  {mainDriver.driverName.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2)}
                </div>
                <div>
                  <p className="text-sm font-medium text-foreground">{mainDriver.driverName}</p>
                  <p className="text-xs text-sky-600 dark:text-sky-400">{TEAM_ROLE_LABELS[TeamMemberRole.MainDriver]}</p>
                </div>
              </div>
            ) : (
              <p className="text-xs text-muted-foreground">Aucun chauffeur</p>
            )}
            {helpers.map(h => (
              <div key={h.driverId} className="flex items-center gap-2">
                <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-slate-400 to-slate-600 text-xs font-bold text-white">
                  {h.driverName.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2)}
                </div>
                <div>
                  <p className="text-sm font-medium text-foreground">{h.driverName}</p>
                  <p className="text-xs text-muted-foreground">{TEAM_ROLE_LABELS[TeamMemberRole.Helper]}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Route stats */}
      <div className="rounded-xl border bg-card p-4 shadow-sm">
        <SectionTitle icon={<Navigation className="h-4 w-4" />}>Itinéraire</SectionTitle>
        <div className="flex items-center gap-6 flex-wrap">
          <div className="flex items-center gap-1.5">
            <Package className="h-4 w-4 text-muted-foreground" />
            <span className="text-sm font-semibold text-foreground">{wizard.selectedDeliveries.length}</span>
            <span className="text-sm text-muted-foreground">livraison{wizard.selectedDeliveries.length > 1 ? 's' : ''}</span>
          </div>
          <div className="flex items-center gap-1.5">
            <Navigation className="h-4 w-4 text-muted-foreground" />
            <span className="text-sm font-semibold text-foreground">{totalDistance}</span>
          </div>
          <div className="flex items-center gap-1.5">
            <Clock className="h-4 w-4 text-muted-foreground" />
            <span className="text-sm font-semibold text-foreground">{totalDuration}</span>
          </div>
          {wizard.wasOptimizedByGoogle && (
            <span className="flex items-center gap-1 text-xs font-medium text-emerald-600 dark:text-emerald-400">
              <CheckCircle className="h-3.5 w-3.5" />Optimisé via Google Maps
            </span>
          )}
          {wizard.wasManuallyReordered && (
            <span className="flex items-center gap-1 text-xs font-medium text-amber-600 dark:text-amber-400">
              <AlertTriangle className="h-3.5 w-3.5" />Ordre manuel
            </span>
          )}
          {endTime && (
            <>
              <div className="flex items-center gap-1.5 rounded-lg border border-sky-200 bg-sky-50 px-2.5 py-1 text-xs dark:border-sky-500/30 dark:bg-sky-500/10">
                <Navigation className="h-3.5 w-3.5 text-sky-500 dark:text-sky-400" />
                <span className="text-muted-foreground">Départ</span>
                <span className="font-semibold text-sky-700 dark:text-sky-400">{wizard.startTime}</span>
              </div>
              <ArrowRight className="h-3.5 w-3.5 text-muted-foreground" />
              <div className="flex items-center gap-1.5 rounded-lg border border-orange-200 bg-orange-50 px-2.5 py-1 text-xs dark:border-orange-500/30 dark:bg-orange-500/10">
                <Timer className="h-3.5 w-3.5 text-orange-500 dark:text-orange-400" />
                <span className="text-muted-foreground">Fin estimée</span>
                <span className="font-semibold text-orange-700 dark:text-orange-400">{endTime}</span>
              </div>
            </>
          )}
        </div>

        {/* Delivery list preview */}
        <div className="mt-3 max-h-96 overflow-y-auto rounded-lg border bg-muted divide-y">
          {wizard.optimizedDeliveries.map((od, i) => {
            const d = wizard.selectedDeliveries.find(s => s.id === od.deliveryId)
            const tl = timeline[i]
            return (
              <div key={od.deliveryId} className="px-3 py-2.5">
                <div className="flex items-center gap-2.5">
                  <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-sky-100 text-xs font-bold text-sky-700 dark:bg-sky-500/15 dark:text-sky-400">{i + 1}</span>
                  <div className="min-w-0">
                    <p className="text-xs font-medium text-foreground truncate">{d?.clientName ?? `Livraison #${od.deliveryId}`}</p>
                    <p className="text-xs text-muted-foreground truncate">{od.address}</p>
                  </div>
                </div>

                {tl && tl.travelMinutes > 0 && (
                  <div className="ml-7 mt-1.5 flex flex-wrap items-center gap-1">
                    <div className="flex items-center gap-1 rounded-md border border-sky-200 bg-sky-50 px-2 py-0.5 dark:border-sky-500/30 dark:bg-sky-500/10">
                      <Navigation className="h-3 w-3 text-sky-500 dark:text-sky-400" />
                      <span className="text-xs text-muted-foreground">Départ</span>
                      <span className="text-xs font-semibold text-sky-700 dark:text-sky-400">{minutesToTime(tl.prevDepartureMinutes)}</span>
                    </div>
                    <ArrowRight className="h-3 w-3 shrink-0 text-muted-foreground/40" />
                    <div className="flex items-center gap-1 rounded-md border bg-card px-2 py-0.5">
                      <Clock className="h-3 w-3 text-muted-foreground" />
                      <span className="text-xs font-medium text-muted-foreground">{tl.travelMinutes} min</span>
                    </div>
                    <ArrowRight className="h-3 w-3 shrink-0 text-muted-foreground/40" />
                    <div className="flex items-center gap-1 rounded-md border border-emerald-200 bg-emerald-50 px-2 py-0.5 dark:border-emerald-500/30 dark:bg-emerald-500/10">
                      <MapPin className="h-3 w-3 text-emerald-500 dark:text-emerald-400" />
                      <span className="text-xs text-muted-foreground">Arrivée</span>
                      <span className="text-xs font-semibold text-emerald-700 dark:text-emerald-400">{minutesToTime(tl.arrivalMinutes)}</span>
                    </div>
                    {tl.serviceMinutes > 0 && (
                      <>
                        <ArrowRight className="h-3 w-3 shrink-0 text-muted-foreground/40" />
                        <div className="flex items-center gap-1 rounded-md border border-amber-200 bg-amber-50 px-2 py-0.5 dark:border-amber-500/30 dark:bg-amber-500/10">
                          <Timer className="h-3 w-3 text-amber-500 dark:text-amber-400" />
                          <span className="text-xs font-medium text-amber-700 dark:text-amber-300">{tl.serviceMinutes} min</span>
                        </div>
                        <ArrowRight className="h-3 w-3 shrink-0 text-muted-foreground/40" />
                        <div className="flex items-center gap-1 rounded-md border border-orange-200 bg-orange-50 px-2 py-0.5 dark:border-orange-500/30 dark:bg-orange-500/10">
                          <Navigation className="h-3 w-3 text-orange-500 dark:text-orange-400" />
                          <span className="text-xs text-muted-foreground">Départ</span>
                          <span className="text-xs font-semibold text-orange-700 dark:text-orange-400">{minutesToTime(tl.departureMinutes)}</span>
                        </div>
                      </>
                    )}
                  </div>
                )}
              </div>
            )
          })}
        </div>
      </div>

      {/* Confirm toggle */}
      <label className="flex items-center gap-3 cursor-pointer rounded-xl border border-blue-200 bg-blue-50 px-4 py-3 dark:border-blue-500/30 dark:bg-blue-500/10">
        <input
          type="checkbox"
          checked={confirmAfterCreate}
          onChange={e => setConfirmAfterCreate(e.target.checked)}
          className="h-4 w-4 rounded border-blue-300 text-blue-600 focus:ring-blue-500"
        />
        <div>
          <p className="text-sm font-medium text-blue-800 dark:text-blue-300">Confirmer la tournée</p>
          <p className="text-xs text-blue-600 dark:text-blue-400">La tournée sera confirmée après création (notifie les chauffeurs)</p>
        </div>
      </label>

      {!confirmAfterCreate && (
        <p className="text-xs text-muted-foreground text-center flex items-center justify-center gap-1">
          <AlertTriangle className="h-3.5 w-3.5" />
          {isEdit ? 'La tournée sera mise à jour en brouillon.' : 'La tournée sera sauvegardée en brouillon — vous pourrez la confirmer ensuite.'}
        </p>
      )}

      {/* Nav */}
      <div className="flex justify-between pt-2">
        <Button variant="outline" onClick={wizard.prev} disabled={createMutation.isPending}>← Précédent</Button>
        <Button
          onClick={() => createMutation.mutate()}
          disabled={createMutation.isPending}
          className="bg-sky-600 hover:bg-sky-700 text-white px-8 font-semibold disabled:opacity-50"
        >
          {createMutation.isPending ? (
            <span className="flex items-center gap-2">
              <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
              {isEdit ? 'Mise à jour…' : 'Création en cours…'}
            </span>
          ) : (
            <span className="flex items-center gap-2">
              <CheckCircle className="h-4 w-4" />
              {isEdit
                ? (confirmAfterCreate ? 'Mettre à jour et confirmer' : 'Mettre à jour')
                : 'Créer la tournée'}
            </span>
          )}
        </Button>
      </div>
    </div>
  )
}
