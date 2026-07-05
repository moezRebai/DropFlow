import { useEffect, useRef, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Skeleton } from '@/components/ui/skeleton'
import { routesApi, routeKeys, RouteStatus } from '@/api/routes'
import { deliveriesApi, deliveryKeys } from '@/api/deliveries'
import { useWizardStore } from '@/store/wizardStore'
import RouteWizard from './wizard/RouteWizard'

export default function EditRoutePage() {
  const { id } = useParams<{ id: string }>()
  const routeId = Number(id)
  const navigate = useNavigate()
  const wizard = useWizardStore()
  const [ready, setReady] = useState(false)
  const initialized = useRef(false)

  const { data: routeResult, isLoading: routeLoading } = useQuery({
    queryKey: routeKeys.detail(routeId),
    queryFn: () => routesApi.getById(routeId),
    enabled: !!routeId,
  })

  const route = routeResult?.data

  const { data: deliveriesResult, isLoading: deliveriesLoading } = useQuery({
    queryKey: deliveryKeys.availableForRoute(route?.date.substring(0, 10) ?? '', routeId),
    queryFn: () => deliveriesApi.availableForRoute(route!.date.substring(0, 10), routeId),
    enabled: !!route,
  })

  useEffect(() => {
    if (initialized.current || !route || !deliveriesResult?.data) return

    if (route.status !== RouteStatus.Draft) {
      navigate(`/routes/${routeId}`, { replace: true })
      return
    }

    initialized.current = true

    const available = deliveriesResult.data
    const routeDeliveryIds = new Set(route.deliveries.map(d => d.deliveryId))
    const selected = available.filter(d => routeDeliveryIds.has(d.id))

    wizard.reset()
    wizard.setEditMode(routeId, route.reference)
    wizard.setStep1({
      date: route.date.substring(0, 10),
      vehicleId: route.vehicleId,
      vehicleName: route.vehicleName,
      startTime: route.startTime ? route.startTime.substring(0, 5) : '08:00',
      departureAddress: route.departureAddress,
      departureLatitude: route.departureLatitude ?? null,
      departureLongitude: route.departureLongitude ?? null,
    })
    wizard.setSelectedDeliveries(selected)
    wizard.setTeam(route.teamMembers.map(t => ({
      driverId: t.driverId,
      role: t.role,
      driverName: t.driverName,
    })))

    const sorted = [...route.deliveries].sort((a, b) => a.sequenceOrder - b.sequenceOrder)
    wizard.setOptimizationResult({
      deliveries: sorted.map(d => ({
        deliveryId: d.deliveryId,
        sequenceOrder: d.sequenceOrder,
        address: d.address,
        latitude: d.latitude,
        longitude: d.longitude,
        distanceToNextMeters: d.distanceToNextMeters ?? 0,
        durationToNextMinutes: d.travelDurationMinutes ?? 0,
      })),
      totalDistanceKm: route.totalDistance ?? 0,
      totalDurationMinutes: route.totalDuration ?? 0,
      wasOptimizedByGoogle: route.wasOptimizedByGoogle,
      wasManuallyReordered: route.wasManuallyReordered,
    })

    setReady(true)
  }, [route, deliveriesResult])

  if (routeLoading || deliveriesLoading || !ready) {
    return (
      <div className="flex flex-col gap-6 p-6">
        <Skeleton className="h-40 w-full rounded-2xl" />
        <Skeleton className="h-12 w-full rounded-xl" />
        <Skeleton className="h-64 w-full rounded-2xl" />
      </div>
    )
  }

  return <RouteWizard />
}
