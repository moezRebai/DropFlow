import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import {
  ArrowLeft, Route, Navigation, Truck, Clock, MapPin,
  Users, CheckCircle, Play, Flag, XCircle, AlertTriangle,
  Package, ChevronRight, Gauge, Pencil, CalendarClock,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import { Map, useMap, useMapsLibrary } from '@vis.gl/react-google-maps'
import {
  routesApi, routeKeys, RouteStatus,
  ROUTE_STATUS_LABELS, ROUTE_STATUS_COLORS,
  TeamMemberRole, TEAM_ROLE_LABELS,
  type RouteDto,
} from '@/api/routes'

function StatusBadge({ status }: { status: RouteStatus }) {
  return (
    <span className={cn('inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium', ROUTE_STATUS_COLORS[status])}>
      {ROUTE_STATUS_LABELS[status]}
    </span>
  )
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

function formatTime(timeStr?: string) {
  if (!timeStr) return '—'
  return timeStr.substring(0, 5)
}

// ─── RouteMapSection ──────────────────────────────────────────────────────────

function MapOverlays({ route, onResolvingDepartureChange }: { route: RouteDto; onResolvingDepartureChange: (resolving: boolean) => void }) {
  const map = useMap()
  const mapsLib = useMapsLibrary('maps')
  const markerLib = useMapsLibrary('marker')
  const geocodingLib = useMapsLibrary('geocoding')

  useEffect(() => {
    if (!map || !mapsLib || !markerLib || !geocodingLib) return

    const sorted = [...route.deliveries]
      .filter(d => d.latitude != null && d.longitude != null)
      .sort((a, b) => a.sequenceOrder - b.sequenceOrder)

    let cancelled = false
    const markers: google.maps.marker.AdvancedMarkerElement[] = []
    const polylines: google.maps.Polyline[] = []

    const makePin = (label: string, bg: string) => {
      const el = document.createElement('div')
      el.style.cssText = `
        width:28px;height:28px;border-radius:50%;background:${bg};
        border:2.5px solid #fff;display:flex;align-items:center;
        justify-content:center;color:#fff;font-size:11px;font-weight:700;
        box-shadow:0 2px 6px rgba(0,0,0,.35);
      `
      el.textContent = label
      return el
    }

    const draw = (depLat?: number, depLng?: number) => {
      if (cancelled) return

      if (depLat != null && depLng != null) {
        markers.push(new markerLib.AdvancedMarkerElement({
          map,
          position: { lat: depLat, lng: depLng },
          content: makePin('D', '#334155'),
          title: route.departureAddress,
          zIndex: 200,
        }))
      }

      sorted.forEach((d, idx) => {
        markers.push(new markerLib.AdvancedMarkerElement({
          map,
          position: { lat: d.latitude!, lng: d.longitude! },
          content: makePin(String(idx + 1), '#0ea5e9'),
          title: `${idx + 1}. ${d.clientName} — ${d.address}`,
        }))
      })

      const path: google.maps.LatLngLiteral[] = []
      if (depLat != null && depLng != null) path.push({ lat: depLat, lng: depLng })
      sorted.forEach(d => path.push({ lat: d.latitude!, lng: d.longitude! }))

      if (path.length >= 2) {
        polylines.push(new mapsLib.Polyline({
          map,
          path,
          strokeColor: '#0ea5e9',
          strokeOpacity: 0.75,
          strokeWeight: 3,
          geodesic: true,
        }))
      }

      if (path.length > 0) {
        const lats = path.map(p => p.lat)
        const lngs = path.map(p => p.lng)
        map.fitBounds(
          { north: Math.max(...lats), south: Math.min(...lats), east: Math.max(...lngs), west: Math.min(...lngs) },
          60,
        )
      }
    }

    if (route.departureLatitude != null && route.departureLongitude != null) {
      draw(route.departureLatitude, route.departureLongitude)
    } else if (route.departureAddress) {
      // Coords dépôt non stockées : géocodage de l'adresse en fallback
      onResolvingDepartureChange(true)
      const geocoder = new geocodingLib.Geocoder()
      geocoder.geocode({ address: route.departureAddress, region: 'fr' }, (results, status) => {
        if (cancelled) return
        onResolvingDepartureChange(false)
        if (status === 'OK' && results?.[0]) {
          const loc = results[0].geometry.location
          draw(loc.lat(), loc.lng())
        } else {
          draw()
        }
      })
    } else {
      draw()
    }

    return () => {
      cancelled = true
      onResolvingDepartureChange(false)
      markers.forEach(m => { m.map = null })
      polylines.forEach(p => p.setMap(null))
    }
  }, [map, mapsLib, markerLib, geocodingLib, route, onResolvingDepartureChange])

  return null
}

function RouteMapSection({ route }: { route: RouteDto }) {
  const [isResolvingDeparture, setIsResolvingDeparture] = useState(false)
  const deliveriesWithCoords = route.deliveries.filter(d => d.latitude != null && d.longitude != null)
  const hasDeparture = route.departureLatitude != null && route.departureLongitude != null
  const hasAnyCoords = deliveriesWithCoords.length > 0 || hasDeparture

  if (!hasAnyCoords) {
    return (
      <div className="flex h-full min-h-44 items-center justify-center rounded-2xl border border-dashed bg-muted">
        <div className="flex flex-col items-center gap-2 text-muted-foreground">
          <MapPin className="h-8 w-8 opacity-30" />
          <span className="text-sm">Coordonnées GPS non disponibles</span>
        </div>
      </div>
    )
  }

  const defaultCenter = hasDeparture
    ? { lat: route.departureLatitude!, lng: route.departureLongitude! }
    : { lat: deliveriesWithCoords[0].latitude!, lng: deliveriesWithCoords[0].longitude! }

  return (
    <div className="flex h-full min-h-[320px] flex-col overflow-hidden rounded-2xl border bg-card shadow-sm">
      <div className="flex shrink-0 items-center gap-2 border-b bg-muted px-5 py-3.5">
        <MapPin className="h-4 w-4 text-muted-foreground" />
        <span className="text-sm font-semibold text-foreground">Carte de la tournée</span>
        <div className="ml-auto flex items-center gap-3 text-xs text-muted-foreground">
          <span className="flex items-center gap-1">
            <span className="inline-block h-2.5 w-2.5 rounded-full bg-slate-700 dark:bg-slate-400" />Dépôt
          </span>
          <span className="flex items-center gap-1">
            <span className="inline-block h-2.5 w-2.5 rounded-full bg-sky-500" />Livraisons
          </span>
          <span className="rounded-full bg-muted-foreground/10 px-2 py-0.5">
            {deliveriesWithCoords.length}/{route.deliveries.length} géolocalisés
          </span>
        </div>
      </div>
      <div className="relative flex-1 min-h-0">
        <Map
          defaultCenter={defaultCenter}
          defaultZoom={11}
          mapId="route-detail"
          gestureHandling="cooperative"
          disableDefaultUI
          style={{ width: '100%', height: '100%' }}
        >
          <MapOverlays route={route} onResolvingDepartureChange={setIsResolvingDeparture} />
        </Map>
        {isResolvingDeparture && (
          <div className="pointer-events-none absolute left-3 top-3 flex items-center gap-1.5 rounded-lg bg-card/90 px-2.5 py-1.5 text-xs font-medium text-muted-foreground shadow-sm backdrop-blur-sm">
            <span className="h-3 w-3 animate-spin rounded-full border-2 border-current border-t-transparent" />
            Localisation du dépôt…
          </div>
        )}
      </div>
    </div>
  )
}

// ─── ConfirmActionModal ───────────────────────────────────────────────────────

function ConfirmActionModal({ title, message, confirmLabel, confirmClass, onConfirm, onCancel, isPending }: {
  title: string; message: string; confirmLabel: string; confirmClass?: string
  onConfirm: () => void; onCancel: () => void; isPending: boolean
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-card shadow-2xl">
        <div className="p-6">
          <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-amber-100 dark:bg-amber-500/15">
            <AlertTriangle className="h-6 w-6 text-amber-600 dark:text-amber-400" />
          </div>
          <h3 className="mb-1 text-base font-semibold text-foreground">{title}</h3>
          <p className="text-sm text-muted-foreground">{message}</p>
        </div>
        <div className="flex gap-3 border-t px-6 py-4">
          <Button variant="outline" className="flex-1" onClick={onCancel}>Annuler</Button>
          <Button
            className={cn('flex-1', confirmClass)}
            onClick={onConfirm}
            disabled={isPending}
          >
            {isPending
              ? <span className="flex items-center gap-2"><span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />Chargement…</span>
              : confirmLabel}
          </Button>
        </div>
      </div>
    </div>
  )
}

// ─── RouteDetailPage ──────────────────────────────────────────────────────────

type ModalAction = 'confirm' | 'start' | 'complete' | 'cancel' | null

export default function RouteDetailPage() {
  const { id } = useParams<{ id: string }>()
  const routeId = Number(id)
  const navigate = useNavigate()
  const qc = useQueryClient()

  const [modalAction, setModalAction] = useState<ModalAction>(null)

  const { data: result, isLoading } = useQuery({
    queryKey: routeKeys.detail(routeId),
    queryFn: () => routesApi.getById(routeId),
    enabled: !!routeId,
  })

  const route = result?.data

  function makeOnSuccess(successMsg: string) {
    return () => {
      qc.invalidateQueries({ queryKey: routeKeys.detail(routeId) })
      qc.invalidateQueries({ queryKey: routeKeys.lists() })
      toast.success(successMsg)
      setModalAction(null)
    }
  }

  function makeOnError() {
    return (err: unknown) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur') : 'Erreur')
  }

  const confirmMutation = useMutation({
    mutationFn: () => routesApi.confirm(routeId),
    onSuccess: makeOnSuccess('Tournée confirmée'),
    onError: makeOnError(),
  })
  const startMutation = useMutation({
    mutationFn: () => routesApi.start(routeId),
    onSuccess: makeOnSuccess('Tournée démarrée'),
    onError: makeOnError(),
  })
  const completeMutation = useMutation({
    mutationFn: () => routesApi.complete(routeId),
    onSuccess: makeOnSuccess('Tournée terminée'),
    onError: makeOnError(),
  })
  const cancelMutation = useMutation({
    mutationFn: () => routesApi.cancel(routeId),
    onSuccess: makeOnSuccess('Tournée annulée'),
    onError: makeOnError(),
  })

  const MODAL_CONFIG: Record<NonNullable<ModalAction>, {
    title: string; message: string; confirmLabel: string; confirmClass?: string
    mutation: { mutate: () => void; isPending: boolean }
  }> = {
    confirm: {
      title: 'Confirmer la tournée ?',
      message: 'Les chauffeurs seront notifiés et les livraisons seront réservées.',
      confirmLabel: 'Confirmer',
      confirmClass: 'bg-blue-600 hover:bg-blue-700 text-white',
      mutation: { mutate: confirmMutation.mutate, isPending: confirmMutation.isPending },
    },
    start: {
      title: 'Démarrer la tournée ?',
      message: 'La tournée passera en statut "En cours". Cette action est irréversible.',
      confirmLabel: 'Démarrer',
      confirmClass: 'bg-amber-500 hover:bg-amber-600 text-white',
      mutation: { mutate: startMutation.mutate, isPending: startMutation.isPending },
    },
    complete: {
      title: 'Terminer la tournée ?',
      message: 'La tournée sera marquée comme terminée. Cette action est irréversible.',
      confirmLabel: 'Terminer',
      confirmClass: 'bg-emerald-600 hover:bg-emerald-700 text-white',
      mutation: { mutate: completeMutation.mutate, isPending: completeMutation.isPending },
    },
    cancel: {
      title: 'Annuler la tournée ?',
      message: 'Toutes les livraisons assignées seront libérées et disponibles à nouveau.',
      confirmLabel: 'Annuler la tournée',
      confirmClass: 'bg-red-600 hover:bg-red-700 text-white',
      mutation: { mutate: cancelMutation.mutate, isPending: cancelMutation.isPending },
    },
  }

  if (isLoading) {
    return (
      <div className="flex flex-col gap-6 p-6">
        <Skeleton className="h-40 w-full rounded-2xl" />
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          {[...Array(3)].map((_, i) => <Skeleton key={i} className="h-28 rounded-2xl" />)}
        </div>
        <Skeleton className="h-64 rounded-2xl" />
      </div>
    )
  }

  if (!route) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-4 p-6">
        <Route className="h-12 w-12 text-muted-foreground/40" />
        <p className="text-muted-foreground">Tournée introuvable</p>
        <Button variant="outline" onClick={() => navigate('/routes')}>Retour à la liste</Button>
      </div>
    )
  }

  const mainDriver = route.teamMembers.find(t => t.role === TeamMemberRole.MainDriver)
  const helpers = route.teamMembers.filter(t => t.role === TeamMemberRole.Helper)

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Hero */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 p-6 shadow-lg">
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }} />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <button
              onClick={() => navigate('/routes')}
              className="mb-3 flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-white/25"
            >
              <ArrowLeft className="h-3.5 w-3.5" />Retour aux tournées
            </button>
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
                <Route className="h-5 w-5 text-white" />
              </div>
              <div>
                <h1 className="text-xl font-bold tracking-tight text-white font-mono">{route.reference}</h1>
                <p className="text-sm text-sky-200">
                  {new Date(route.date).toLocaleDateString('fr-FR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
                </p>
              </div>
            </div>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <StatusBadge status={route.status} />
            {route.status === RouteStatus.Draft && (
              <>
                <button onClick={() => navigate(`/routes/${routeId}/edit`)} className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white hover:bg-white/25">
                  <Pencil className="h-3.5 w-3.5" />Modifier
                </button>
                <button onClick={() => setModalAction('confirm')} className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white hover:bg-white/25">
                  <CheckCircle className="h-3.5 w-3.5" />Confirmer
                </button>
              </>
            )}
            {route.status === RouteStatus.Confirmed && (
              <button onClick={() => setModalAction('start')} className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white hover:bg-white/25">
                <Play className="h-3.5 w-3.5" />Démarrer
              </button>
            )}
            {route.status === RouteStatus.InProgress && (
              <button onClick={() => setModalAction('complete')} className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white hover:bg-white/25">
                <Flag className="h-3.5 w-3.5" />Terminer
              </button>
            )}
            {(route.status === RouteStatus.Draft || route.status === RouteStatus.Confirmed) && (
              <button onClick={() => setModalAction('cancel')} className="flex items-center gap-1.5 rounded-xl bg-red-500/20 px-3 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-red-500/30">
                <XCircle className="h-3.5 w-3.5" />Annuler
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Info cards */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <div className="rounded-2xl border bg-card p-4 shadow-sm">
          <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2">
            <Truck className="h-3.5 w-3.5" />Véhicule
          </div>
          <p className="text-base font-semibold text-foreground">{route.vehicleName}</p>
          {route.startTime && (
            <p className="mt-1 flex items-center gap-1 text-xs text-muted-foreground">
              <Clock className="h-3 w-3" />Départ {formatTime(route.startTime)}
            </p>
          )}
        </div>

        <div className="rounded-2xl border bg-card p-4 shadow-sm">
          <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2">
            <MapPin className="h-3.5 w-3.5" />Départ
          </div>
          <p className="text-sm font-medium text-foreground line-clamp-2">{route.departureAddress}</p>
        </div>

        <div className="rounded-2xl border bg-card p-4 shadow-sm">
          <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2">
            <Gauge className="h-3.5 w-3.5" />Distance totale
          </div>
          <p className="text-base font-semibold text-foreground">{formatDistance(route.totalDistance)}</p>
          <p className="mt-1 text-xs text-muted-foreground">Durée : {formatDuration(route.totalDuration)}</p>
        </div>

        <div className="rounded-2xl border bg-card p-4 shadow-sm">
          <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2">
            <Package className="h-3.5 w-3.5" />Livraisons
          </div>
          <p className="text-2xl font-bold text-foreground">{route.totalDeliveries}</p>
          {route.wasOptimizedByGoogle && (
            <span className="mt-1 inline-block rounded-full bg-emerald-100 px-2 py-0.5 text-xs text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-400">Optimisé Google</span>
          )}
        </div>
      </div>

      {/* Map + séquence */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3 min-h-[400px] lg:min-h-[560px]">

        {/* Left: Map (2 cols) */}
        <div className="lg:col-span-2 flex flex-col">
          <RouteMapSection route={route} />
        </div>

        {/* Right: Team + Delivery sequence (1 col) */}
        <div className="flex flex-col gap-6">

        {/* Team */}
        <div className="rounded-2xl border bg-card p-5 shadow-sm">
          <h2 className="mb-4 flex items-center gap-2 text-sm font-semibold text-foreground">
            <Users className="h-4 w-4 text-muted-foreground" />Équipe
          </h2>
          <div className="flex flex-col gap-3">
            {mainDriver && (
              <div className="flex items-center gap-3">
                <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-sky-400 to-blue-600 text-xs font-bold text-white">
                  {mainDriver.driverName.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2)}
                </div>
                <div className="min-w-0">
                  <p className="text-sm font-medium text-foreground truncate">{mainDriver.driverName}</p>
                  <p className="text-xs text-sky-600 dark:text-sky-400 font-medium">{TEAM_ROLE_LABELS[mainDriver.role]}</p>
                </div>
              </div>
            )}
            {helpers.map(h => (
              <div key={h.id} className="flex items-center gap-3">
                <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-slate-400 to-slate-600 text-xs font-bold text-white">
                  {h.driverName.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2)}
                </div>
                <div className="min-w-0">
                  <p className="text-sm font-medium text-foreground truncate">{h.driverName}</p>
                  <p className="text-xs text-muted-foreground">{TEAM_ROLE_LABELS[h.role]}</p>
                </div>
              </div>
            ))}
            {route.teamMembers.length === 0 && (
              <p className="text-sm text-muted-foreground">Aucun membre d'équipe</p>
            )}
          </div>
        </div>

        {/* Delivery sequence */}
        <div className="flex-1 rounded-2xl border bg-card shadow-sm overflow-hidden">
          <div className="flex items-center justify-between px-5 py-4 border-b bg-muted">
            <h2 className="flex items-center gap-2 text-sm font-semibold text-foreground">
              <Navigation className="h-4 w-4 text-muted-foreground" />Séquence de livraisons
            </h2>
          </div>
          <div className="divide-y max-h-[480px] overflow-y-auto">
            {route.deliveries.length === 0 ? (
              <div className="py-10 text-center text-sm text-muted-foreground">Aucune livraison</div>
            ) : (
              route.deliveries
                .sort((a, b) => a.sequenceOrder - b.sequenceOrder)
                .map((delivery, idx) => (
                  <div key={delivery.id} className="flex items-start gap-3 px-5 py-3.5 hover:bg-muted/50">
                    <div className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-sky-100 text-xs font-bold text-sky-700 dark:bg-sky-500/15 dark:text-sky-400">
                      {idx + 1}
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className="font-mono text-xs text-muted-foreground">{delivery.reference}</span>
                        <span className="text-sm font-medium text-foreground truncate">{delivery.clientName}</span>
                      </div>
                      <p className="mt-0.5 text-xs text-muted-foreground truncate">{delivery.address}</p>
                      <div className="mt-1 flex flex-wrap items-center gap-3">
                        {delivery.estimatedArrivalTime && delivery.estimatedArrivalTime !== '00:00:00' && (
                          <span className="flex items-center gap-0.5 text-xs text-muted-foreground">
                            <Clock className="h-3 w-3" />Arrivée {formatTime(delivery.estimatedArrivalTime)}
                          </span>
                        )}
                        {delivery.estimatedDurationMinutes != null && delivery.estimatedDurationMinutes > 0 && (
                          <span className="flex items-center gap-0.5 text-xs text-muted-foreground">
                            <Clock className="h-3 w-3" />{formatDuration(delivery.estimatedDurationMinutes)}
                          </span>
                        )}
                        {delivery.timeSlotName && !delivery.timeSlotName.startsWith('00:00') && (
                          <span className="flex items-center gap-1 rounded-full bg-blue-50 px-2 py-0.5 text-xs font-medium text-blue-600 dark:bg-blue-500/15 dark:text-blue-400">
                            <CalendarClock className="h-3 w-3" />
                            {delivery.timeSlotName}
                          </span>
                        )}
                        {delivery.itemCount > 0 && (
                          <span className="flex items-center gap-0.5 text-xs text-muted-foreground">
                            <Package className="h-3 w-3" />{delivery.itemCount} colis
                          </span>
                        )}
                      </div>
                    </div>
                    {delivery.distanceToNextMeters ? (
                      <div className="shrink-0 text-right">
                        <div className="flex items-center gap-1 text-xs text-muted-foreground">
                          <ChevronRight className="h-3 w-3" />
                          {delivery.distanceToNextMeters >= 1000
                            ? `${(delivery.distanceToNextMeters / 1000).toFixed(1)} km`
                            : `${delivery.distanceToNextMeters} m`}
                        </div>
                        {delivery.travelDurationMinutes && (
                          <p className="text-xs text-muted-foreground">{delivery.travelDurationMinutes} min</p>
                        )}
                      </div>
                    ) : null}
                  </div>
                ))
            )}
          </div>
        </div>

        </div> {/* end right column */}
      </div> {/* end map+sequence grid */}

      {/* Footer info */}
      <div className="flex items-center justify-between rounded-xl border bg-muted px-5 py-3 text-xs text-muted-foreground">
        <span>Créée le {new Date(route.createdDate).toLocaleDateString('fr-FR')} par {route.createdBy ?? '—'}</span>
        <div className="flex items-center gap-3">
          {route.wasOptimizedByGoogle && <span className="text-emerald-600 dark:text-emerald-400 font-medium">Optimisé via Google Maps</span>}
          {route.wasManuallyReordered && <span className="text-amber-600 dark:text-amber-400 font-medium">Réordonnée manuellement</span>}
        </div>
      </div>

      {/* Action modal */}
      {modalAction && (
        <ConfirmActionModal
          {...MODAL_CONFIG[modalAction]}
          onConfirm={MODAL_CONFIG[modalAction].mutation.mutate}
          onCancel={() => setModalAction(null)}
          isPending={MODAL_CONFIG[modalAction].mutation.isPending}
        />
      )}
    </div>
  )
}
