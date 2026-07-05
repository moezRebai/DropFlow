import { create } from 'zustand'
import type { DeliveryDto } from '@/api/deliveries'
import type { OptimizedDeliveryDto, TeamMemberDto } from '@/api/routes'
import { TeamMemberRole } from '@/api/routes'

export type WizardStep = 1 | 2 | 3 | 4 | 5

export interface WizardTeamMember extends TeamMemberDto {
  driverName: string
}

interface WizardState {
  currentStep: WizardStep

  // Edit mode
  editRouteId: number | null
  editRouteReference: string

  // Step 1 — Infos de base
  date: string
  vehicleId: number | null
  vehicleName: string
  startTime: string
  departureAddress: string
  departureLatitude: number | null
  departureLongitude: number | null

  // Step 2 — Livraisons sélectionnées
  selectedDeliveries: DeliveryDto[]

  // Step 3 — Équipe
  team: WizardTeamMember[]

  // Step 4 — Résultat de l'optimisation
  optimizedDeliveries: OptimizedDeliveryDto[]
  totalDistanceKm: number | null
  totalDurationMinutes: number | null
  wasOptimizedByGoogle: boolean
  wasManuallyReordered: boolean

  // Actions navigation
  goTo: (step: WizardStep) => void
  next: () => void
  prev: () => void

  // Actions données
  setEditMode: (routeId: number, reference: string) => void
  setStep1: (data: {
    date: string
    vehicleId: number
    vehicleName: string
    startTime: string
    departureAddress: string
    departureLatitude?: number | null
    departureLongitude?: number | null
  }) => void
  setSelectedDeliveries: (deliveries: DeliveryDto[]) => void
  setTeam: (team: WizardTeamMember[]) => void
  setOptimizationResult: (result: {
    deliveries: OptimizedDeliveryDto[]
    totalDistanceKm: number
    totalDurationMinutes: number
    wasOptimizedByGoogle: boolean
    wasManuallyReordered?: boolean
  }) => void
  setManualOrder: (deliveries: OptimizedDeliveryDto[]) => void

  reset: () => void
}

const INITIAL_STATE = {
  currentStep: 1 as WizardStep,
  editRouteId: null,
  editRouteReference: '',
  date: '',
  vehicleId: null,
  vehicleName: '',
  startTime: '08:00',
  departureAddress: '',
  departureLatitude: null,
  departureLongitude: null,
  selectedDeliveries: [],
  team: [],
  optimizedDeliveries: [],
  totalDistanceKm: null,
  totalDurationMinutes: null,
  wasOptimizedByGoogle: false,
  wasManuallyReordered: false,
}

export const useWizardStore = create<WizardState>((set) => ({
  ...INITIAL_STATE,

  setEditMode: (routeId, reference) => set({ editRouteId: routeId, editRouteReference: reference }),

  goTo: (step) => set({ currentStep: step }),
  next: () => set(s => ({ currentStep: Math.min(5, s.currentStep + 1) as WizardStep })),
  prev: () => set(s => ({ currentStep: Math.max(1, s.currentStep - 1) as WizardStep })),

  setStep1: (data) => set({
    date: data.date,
    vehicleId: data.vehicleId,
    vehicleName: data.vehicleName,
    startTime: data.startTime,
    departureAddress: data.departureAddress,
    departureLatitude: data.departureLatitude ?? null,
    departureLongitude: data.departureLongitude ?? null,
  }),

  setSelectedDeliveries: (deliveries) => set({ selectedDeliveries: deliveries }),

  setTeam: (team) => set({ team }),

  setOptimizationResult: ({ deliveries, totalDistanceKm, totalDurationMinutes, wasOptimizedByGoogle, wasManuallyReordered = false }) => set({
    optimizedDeliveries: deliveries,
    totalDistanceKm,
    totalDurationMinutes,
    wasOptimizedByGoogle,
    wasManuallyReordered,
  }),

  setManualOrder: (deliveries) => set({
    optimizedDeliveries: deliveries.map((d, i) => ({ ...d, sequenceOrder: i + 1 })),
    wasOptimizedByGoogle: false,
    wasManuallyReordered: true,
  }),

  reset: () => set(INITIAL_STATE),
}))

// Helpers exportés pour usage dans les composants
export function getMainDriver(team: WizardTeamMember[]): WizardTeamMember | undefined {
  return team.find(t => t.role === TeamMemberRole.MainDriver)
}

export function formatDistance(meters: number): string {
  if (meters >= 1000) return `${(meters / 1000).toFixed(1)} km`
  return `${meters} m`
}

export function formatDuration(minutes: number): string {
  if (minutes >= 60) {
    const h = Math.floor(minutes / 60)
    const m = minutes % 60
    return m > 0 ? `${h}h${String(m).padStart(2, '0')}` : `${h}h`
  }
  return `${minutes} min`
}

// ─── Timeline helpers ─────────────────────────────────────────────────────────

export interface TimelineEntry {
  prevDepartureMinutes: number
  travelMinutes: number
  arrivalMinutes: number
  serviceMinutes: number
  departureMinutes: number
}

export function minutesToTime(totalMinutes: number): string {
  const h = Math.floor(totalMinutes / 60) % 24
  const m = totalMinutes % 60
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`
}

export function computeTimeline(
  deliveries: OptimizedDeliveryDto[],
  startTime: string,
  serviceMap: Map<number, number>,
): TimelineEntry[] {
  const [h, m] = startTime.split(':').map(Number)
  let cursor = h * 60 + (m || 0)
  return deliveries.map(d => {
    const travel = d.durationToNextMinutes
    const arrival = cursor + travel
    const service = serviceMap.get(d.deliveryId) ?? 0
    const departure = arrival + service
    const entry: TimelineEntry = { prevDepartureMinutes: cursor, travelMinutes: travel, arrivalMinutes: arrival, serviceMinutes: service, departureMinutes: departure }
    cursor = departure
    return entry
  })
}
