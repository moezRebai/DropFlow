import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import {
  DndContext, closestCenter, PointerSensor, useSensor, useSensors,
  type DragEndEvent,
} from '@dnd-kit/core'
import {
  SortableContext, verticalListSortingStrategy, useSortable, arrayMove,
} from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import {
  Sparkles, GripVertical, Navigation, MapPin, Clock,
  AlertTriangle, CheckCircle, RefreshCw, ArrowRight, Timer,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { routesApi } from '@/api/routes'
import type { OptimizedDeliveryDto } from '@/api/routes'
import { useWizardStore, formatDistance, formatDuration, computeTimeline, minutesToTime } from '@/store/wizardStore'
import type { TimelineEntry } from '@/store/wizardStore'

// ─── SortableRow ──────────────────────────────────────────────────────────────

function SortableRow({ delivery, index, timeline }: {
  delivery: OptimizedDeliveryDto & { clientName?: string }
  index: number
  timeline?: TimelineEntry
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: delivery.deliveryId,
  })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  }

  return (
    <div
      ref={setNodeRef}
      style={style}
      className="border-b last:border-b-0 bg-white px-4 py-3 hover:bg-slate-50"
    >
      <div className="flex items-center gap-3">
        <button
          {...attributes}
          {...listeners}
          className="cursor-grab touch-none text-slate-300 hover:text-slate-500 active:cursor-grabbing"
          aria-label="Réordonner"
        >
          <GripVertical className="h-4 w-4" />
        </button>

        <div className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-sky-100 text-xs font-bold text-sky-700">
          {index + 1}
        </div>

        <div className="min-w-0 flex-1">
          <p className="text-sm font-medium text-slate-800 truncate">
            {delivery.clientName ?? `Livraison #${delivery.deliveryId}`}
          </p>
          <p className="text-xs text-slate-500 truncate">{delivery.address}</p>
        </div>

        {delivery.distanceToNextMeters > 0 && (
          <div className="shrink-0 text-right">
            <p className="flex items-center gap-0.5 text-xs text-slate-400 justify-end">
              <Navigation className="h-3 w-3" />
              {delivery.distanceToNextMeters >= 1000
                ? `${(delivery.distanceToNextMeters / 1000).toFixed(1)} km`
                : `${delivery.distanceToNextMeters} m`}
            </p>
          </div>
        )}
      </div>

      {timeline && timeline.travelMinutes > 0 && (
        <div className="ml-9 mt-2 flex flex-wrap items-center gap-1.5">
          {/* Départ précédent */}
          <div className="flex items-center gap-1 rounded-md border border-sky-200 bg-sky-50 px-2 py-0.5">
            <Navigation className="h-3 w-3 text-sky-500" />
            <span className="text-xs text-slate-500">Départ</span>
            <span className="text-xs font-semibold text-sky-700">{minutesToTime(timeline.prevDepartureMinutes)}</span>
          </div>
          <ArrowRight className="h-3 w-3 shrink-0 text-slate-300" />
          {/* Trajet */}
          <div className="flex items-center gap-1 rounded-md border border-slate-200 bg-slate-50 px-2 py-0.5">
            <Clock className="h-3 w-3 text-slate-400" />
            <span className="text-xs font-medium text-slate-600">{timeline.travelMinutes} min de trajet</span>
          </div>
          <ArrowRight className="h-3 w-3 shrink-0 text-slate-300" />
          {/* Arrivée */}
          <div className="flex items-center gap-1 rounded-md border border-emerald-200 bg-emerald-50 px-2 py-0.5">
            <MapPin className="h-3 w-3 text-emerald-500" />
            <span className="text-xs text-slate-500">Arrivée</span>
            <span className="text-xs font-semibold text-emerald-700">{minutesToTime(timeline.arrivalMinutes)}</span>
          </div>
          {timeline.serviceMinutes > 0 && (
            <>
              <ArrowRight className="h-3 w-3 shrink-0 text-slate-300" />
              {/* Prestation */}
              <div className="flex items-center gap-1 rounded-md border border-amber-200 bg-amber-50 px-2 py-0.5">
                <Timer className="h-3 w-3 text-amber-500" />
                <span className="text-xs font-medium text-amber-700">{timeline.serviceMinutes} min prestation</span>
              </div>
              <ArrowRight className="h-3 w-3 shrink-0 text-slate-300" />
              {/* Départ client */}
              <div className="flex items-center gap-1 rounded-md border border-orange-200 bg-orange-50 px-2 py-0.5">
                <Navigation className="h-3 w-3 text-orange-500" />
                <span className="text-xs text-slate-500">Départ client</span>
                <span className="text-xs font-semibold text-orange-700">{minutesToTime(timeline.departureMinutes)}</span>
              </div>
            </>
          )}
        </div>
      )}
    </div>
  )
}

// ─── Step4Optimize ────────────────────────────────────────────────────────────

export function Step4Optimize() {
  const wizard = useWizardStore()
  const [hasOptimized, setHasOptimized] = useState(wizard.optimizedDeliveries.length > 0)

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }))

  const deliveryIdToName = new Map(
    wizard.selectedDeliveries.map(d => [d.id, d.clientName])
  )

  const optimizeMutation = useMutation({
    mutationFn: () => routesApi.optimizePath({
      departureAddress: wizard.departureAddress,
      deliveryIds: wizard.selectedDeliveries.map(d => d.id),
    }),
    onSuccess: (result) => {
      if (!result.succeeded || !result.data) {
        toast.error(result.errors?.[0] ?? result.message ?? 'Erreur d\'optimisation')
        return
      }
      wizard.setOptimizationResult({
        deliveries: result.data.deliveries,
        totalDistanceKm: result.data.totalDistanceKm,
        totalDurationMinutes: result.data.totalDurationMinutes,
        wasOptimizedByGoogle: true,
      })
      setHasOptimized(true)
      toast.success('Itinéraire optimisé')
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur d\'optimisation') : 'Erreur'),
  })

  const recalculateMutation = useMutation({
    mutationFn: (orderedIds: number[]) => routesApi.recalculatePath({
      departureAddress: wizard.departureAddress,
      deliveryIds: orderedIds,
    }),
    onSuccess: (result) => {
      if (!result.succeeded || !result.data) return
      wizard.setOptimizationResult({
        deliveries: result.data.deliveries,
        totalDistanceKm: result.data.totalDistanceKm,
        totalDurationMinutes: result.data.totalDurationMinutes,
        wasOptimizedByGoogle: false,
        wasManuallyReordered: true,
      })
    },
  })

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event
    if (!over || active.id === over.id) return

    const oldIndex = wizard.optimizedDeliveries.findIndex(d => d.deliveryId === active.id)
    const newIndex = wizard.optimizedDeliveries.findIndex(d => d.deliveryId === over.id)
    if (oldIndex === -1 || newIndex === -1) return

    const reordered = arrayMove(wizard.optimizedDeliveries, oldIndex, newIndex)
    wizard.setManualOrder(reordered)
    recalculateMutation.mutate(reordered.map(d => d.deliveryId))
  }

  const deliveries = wizard.optimizedDeliveries

  const serviceMap = new Map(
    wizard.selectedDeliveries.map(d => [d.id, d.estimatedDurationMinutes ?? 0])
  )

  const timeline = hasOptimized && wizard.startTime && deliveries.length > 0
    ? computeTimeline(deliveries, wizard.startTime, serviceMap)
    : []

  const endTime = wizard.startTime && wizard.totalDurationMinutes !== null
    ? (() => {
        const [h, m] = wizard.startTime.split(':').map(Number)
        return minutesToTime(h * 60 + m + wizard.totalDurationMinutes!)
      })()
    : null

  return (
    <div className="flex flex-col gap-5">

      {/* Optimize panel */}
      <div className="rounded-2xl border bg-gradient-to-br from-sky-50 to-blue-50 p-5">
        <div className="flex items-start justify-between gap-4 flex-wrap">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <Sparkles className="h-4 w-4 text-sky-600" />
              <h3 className="text-sm font-semibold text-slate-700">Itinéraire & Optimisation</h3>
            </div>
            <p className="text-xs text-slate-500 max-w-sm">
              Calcule l'itinéraire le plus court pour {wizard.selectedDeliveries.length} livraison{wizard.selectedDeliveries.length > 1 ? 's' : ''} depuis {wizard.departureAddress}.
            </p>
          </div>
          <Button
            onClick={() => optimizeMutation.mutate()}
            disabled={optimizeMutation.isPending}
            className="bg-sky-600 hover:bg-sky-700 text-white shrink-0"
          >
            {optimizeMutation.isPending ? (
              <span className="flex items-center gap-2">
                <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                Calcul en cours…
              </span>
            ) : (
              <span className="flex items-center gap-2">
                <RefreshCw className="h-4 w-4" />
                Optimiser avec Google Maps
              </span>
            )}
          </Button>
        </div>

        {/* Stats */}
        {hasOptimized && wizard.totalDistanceKm !== null && (
          <div className="mt-4 space-y-2">
            <div className="flex items-center gap-3 flex-wrap">
              <div className="flex items-center gap-1.5 rounded-xl bg-white/70 px-3 py-1.5 text-xs font-semibold text-slate-700 shadow-sm">
                <Navigation className="h-3.5 w-3.5 text-sky-500" />
                {formatDistance(wizard.totalDistanceKm * 1000)}
              </div>
              {wizard.totalDurationMinutes !== null && (
                <div className="flex items-center gap-1.5 rounded-xl bg-white/70 px-3 py-1.5 text-xs font-semibold text-slate-700 shadow-sm">
                  <Clock className="h-3.5 w-3.5 text-sky-500" />
                  {formatDuration(wizard.totalDurationMinutes)}
                </div>
              )}
              {wizard.wasOptimizedByGoogle && !wizard.wasManuallyReordered && (
                <div className="flex items-center gap-1 text-xs text-emerald-600 font-medium">
                  <CheckCircle className="h-3.5 w-3.5" />Optimisé
                </div>
              )}
              {wizard.wasManuallyReordered && (
                <div className="flex items-center gap-1 text-xs text-amber-600 font-medium">
                  <AlertTriangle className="h-3.5 w-3.5" />Réordonné manuellement
                </div>
              )}
            </div>
            {/* Start / end time row */}
            {endTime && (
              <div className="flex items-center gap-2 flex-wrap">
                <div className="flex items-center gap-1.5 rounded-xl bg-white/70 px-3 py-1.5 text-xs shadow-sm">
                  <Navigation className="h-3.5 w-3.5 text-sky-500" />
                  <span className="text-slate-500">Départ tournée</span>
                  <span className="font-semibold text-sky-700">{wizard.startTime}</span>
                </div>
                <ArrowRight className="h-3.5 w-3.5 text-slate-400" />
                <div className="flex items-center gap-1.5 rounded-xl bg-white/70 px-3 py-1.5 text-xs shadow-sm">
                  <Timer className="h-3.5 w-3.5 text-orange-500" />
                  <span className="text-slate-500">Fin estimée</span>
                  <span className="font-semibold text-orange-700">{endTime}</span>
                </div>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Sequence */}
      {deliveries.length > 0 ? (
        <div className="flex flex-col gap-2">
          <div className="flex items-center justify-between">
            <p className="text-sm font-medium text-slate-600">Séquence de livraisons</p>
            <p className="text-xs text-slate-400 flex items-center gap-1">
              <GripVertical className="h-3.5 w-3.5" />Glissez pour réordonner
            </p>
          </div>

          <div className="overflow-hidden rounded-xl border bg-white shadow-sm">
            {/* Departure */}
            <div className="flex items-center gap-3 border-b bg-sky-50 px-4 py-2.5">
              <div className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-sky-600">
                <MapPin className="h-3.5 w-3.5 text-white" />
              </div>
              <div>
                <p className="text-xs font-semibold text-sky-700">Départ</p>
                <p className="text-xs text-slate-500 truncate max-w-xs">{wizard.departureAddress}</p>
              </div>
            </div>

            <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
              <SortableContext items={deliveries.map(d => d.deliveryId)} strategy={verticalListSortingStrategy}>
                {deliveries.map((d, i) => (
                  <SortableRow
                    key={d.deliveryId}
                    delivery={{ ...d, clientName: deliveryIdToName.get(d.deliveryId) }}
                    index={i}
                    timeline={timeline[i]}
                  />
                ))}
              </SortableContext>
            </DndContext>
          </div>
        </div>
      ) : !optimizeMutation.isPending && (
        <div className="flex flex-col items-center gap-3 rounded-xl border-2 border-dashed border-slate-200 py-10">
          <Navigation className="h-8 w-8 text-slate-300" />
          <p className="text-sm text-slate-400">Cliquez sur "Optimiser avec Google Maps" pour calculer la meilleure route</p>
        </div>
      )}

      {/* Skip note */}
      {!hasOptimized && (
        <p className="text-xs text-slate-400 text-center">
          Vous pouvez passer cette étape — les livraisons seront dans l'ordre de sélection.
        </p>
      )}

      {/* Nav */}
      <div className="flex justify-between pt-2">
        <Button variant="outline" onClick={wizard.prev}>← Précédent</Button>
        <Button
          onClick={() => {
            if (!hasOptimized && wizard.optimizedDeliveries.length === 0) {
              const fallback = wizard.selectedDeliveries.map((d, i) => ({
                deliveryId: d.id,
                sequenceOrder: i + 1,
                address: d.address,
                latitude: d.latitude,
                longitude: d.longitude,
                distanceToNextMeters: 0,
                durationToNextMinutes: 0,
              }))
              wizard.setOptimizationResult({
                deliveries: fallback,
                totalDistanceKm: 0,
                totalDurationMinutes: 0,
                wasOptimizedByGoogle: false,
              })
            }
            wizard.next()
          }}
          className="bg-sky-600 hover:bg-sky-700 text-white px-6"
        >
          Étape suivante →
        </Button>
      </div>
    </div>
  )
}
