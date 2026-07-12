import { get, post, put, del, downloadFile } from './client'

// ─── Shared ───────────────────────────────────────────────────────────────────

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface ResponseResult<T = void> {
  succeeded: boolean
  message?: string
  errors?: string[]
  data?: T
}

// ─── Enums ────────────────────────────────────────────────────────────────────

export enum RouteStatus {
  Draft = 0,
  Confirmed = 1,
  InProgress = 2,
  Completed = 3,
  Cancelled = 4,
}

export enum TeamMemberRole {
  MainDriver = 1,
  Helper = 2,
}

export const ROUTE_STATUS_LABELS: Record<RouteStatus, string> = {
  [RouteStatus.Draft]:     'Brouillon',
  [RouteStatus.Confirmed]: 'Confirmée',
  [RouteStatus.InProgress]:'En cours',
  [RouteStatus.Completed]: 'Terminée',
  [RouteStatus.Cancelled]: 'Annulée',
}

export const ROUTE_STATUS_COLORS: Record<RouteStatus, string> = {
  [RouteStatus.Draft]:      'bg-muted text-muted-foreground',
  [RouteStatus.Confirmed]:  'bg-blue-100 text-blue-700 dark:bg-blue-500/15 dark:text-blue-400',
  [RouteStatus.InProgress]: 'bg-amber-100 text-amber-700 dark:bg-amber-500/15 dark:text-amber-400',
  [RouteStatus.Completed]:  'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-400',
  [RouteStatus.Cancelled]:  'bg-red-100 text-red-600 dark:bg-red-500/15 dark:text-red-400',
}

export const TEAM_ROLE_LABELS: Record<TeamMemberRole, string> = {
  [TeamMemberRole.MainDriver]: 'Chauffeur principal',
  [TeamMemberRole.Helper]:     'Assistant',
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

export interface RouteTeamDto {
  id: number
  driverId: number
  driverName: string
  role: TeamMemberRole
  roleDisplay: string
}

export interface RouteDeliveryDto {
  id: number
  deliveryId: number
  reference: string
  clientName: string
  address: string
  latitude?: number
  longitude?: number
  sequenceOrder: number
  departureAddress?: string
  departureTime?: string
  estimatedArrivalTime?: string
  travelDurationMinutes?: number
  distanceToNextMeters?: number
  estimatedDurationMinutes?: number
  timeSlotId?: number
  timeSlotName?: string
  itemCount: number
}

export interface RouteDto {
  id: number
  reference: string
  date: string
  vehicleId: number
  vehicleName: string
  status: RouteStatus
  statusDisplay: string
  startTime?: string
  estimatedEndTime?: string
  totalDistance?: number
  totalDuration?: number
  totalDeliveries: number
  totalVolume?: number
  departureAddress: string
  departureLatitude?: number
  departureLongitude?: number
  wasOptimizedByGoogle: boolean
  wasManuallyReordered: boolean
  teamMembers: RouteTeamDto[]
  deliveries: RouteDeliveryDto[]
  createdDate: string
  createdBy?: string
}

export interface RouteViewDto {
  id: number
  reference: string
  date: string
  vehicleName: string
  status: RouteStatus
  statusDisplay: string
  totalDeliveries: number
  totalDistance?: number
  totalDuration?: number
  mainDriverName?: string
  teamCount: number
  createdDate?: string
  createdBy?: string
}

export interface RouteFilterDto {
  page?: number
  pageSize?: number
  searchTerm?: string
  date?: string
  status?: RouteStatus
  vehicleId?: number
  driverId?: number
}

export interface TeamMemberDto {
  driverId: number
  role: TeamMemberRole
}

export interface CreateDeliverySequenceDto {
  deliveryId: number
  sequenceOrder: number
  distanceToNextMeters?: number
  travelDurationMinutes?: number
  departureAddress?: string
  departureTime?: string
  estimatedArrivalTime?: string
}

export interface CreateRouteDto {
  date: string
  vehicleId: number
  startTime?: string
  departureAddress: string
  departureLatitude?: number
  departureLongitude?: number
  team: TeamMemberDto[]
  deliveries: CreateDeliverySequenceDto[]
  totalDistance?: number
  totalDuration?: number
  wasOptimizedByGoogle: boolean
  wasManuallyReordered: boolean
}

export interface UpdateDeliverySequenceDto {
  deliveryId: number
  sequenceOrder: number
  travelDurationMinutes?: number
  distanceToNextMeters?: number
}

export interface UpdateRouteDto {
  date: string
  vehicleId: number
  startTime: string
  departureAddress: string
  departureLatitude?: number
  departureLongitude?: number
  team: TeamMemberDto[]
  deliveries: UpdateDeliverySequenceDto[]
  totalDistance: number
  totalDuration: number
  wasOptimizedByGoogle: boolean
  wasManuallyReordered: boolean
}

export interface OptimizePathRequestDto {
  departureAddress: string
  deliveryIds: number[]
}

export interface OptimizedDeliveryDto {
  deliveryId: number
  sequenceOrder: number
  address: string
  latitude?: number
  longitude?: number
  distanceToNextMeters: number
  durationToNextMinutes: number
}

export interface OptimizePathResponseDto {
  deliveries: OptimizedDeliveryDto[]
  totalDistanceKm: number
  totalDurationMinutes: number
}

// ─── Query keys ───────────────────────────────────────────────────────────────

export const routeKeys = {
  all: ['routes'] as const,
  lists: () => [...routeKeys.all, 'list'] as const,
  list: (filters: RouteFilterDto) => [...routeKeys.lists(), filters] as const,
  details: () => [...routeKeys.all, 'detail'] as const,
  detail: (id: number) => [...routeKeys.details(), id] as const,
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

function buildQuery(filters: RouteFilterDto): string {
  const params = new URLSearchParams()
  if (filters.page) params.set('page', String(filters.page))
  if (filters.pageSize) params.set('pageSize', String(filters.pageSize))
  if (filters.searchTerm) params.set('searchTerm', filters.searchTerm)
  if (filters.date) params.set('date', filters.date)
  if (filters.status !== undefined) params.set('status', String(filters.status))
  if (filters.vehicleId) params.set('vehicleId', String(filters.vehicleId))
  if (filters.driverId) params.set('driverId', String(filters.driverId))
  const q = params.toString()
  return q ? `?${q}` : ''
}

// ─── API ──────────────────────────────────────────────────────────────────────

export const routesApi = {
  getList: (filters: RouteFilterDto) =>
    get<PagedResult<RouteViewDto>>(`/api/routes${buildQuery(filters)}`),

  getById: (id: number) =>
    get<ResponseResult<RouteDto>>(`/api/routes/${id}`),

  create: (data: CreateRouteDto) =>
    post<ResponseResult<number>>('/api/routes', data),

  update: (id: number, data: UpdateRouteDto) =>
    put<ResponseResult>(`/api/routes/${id}`, data),

  delete: (id: number) =>
    del<ResponseResult>(`/api/routes/${id}`),

  // Optimize: Google reorders deliveries for shortest path
  optimizePath: (data: OptimizePathRequestDto) =>
    post<ResponseResult<OptimizePathResponseDto>>('/api/routes/optimize', data),

  // Recalculate: preserves current order, only updates distances/durations (after manual drag)
  recalculatePath: (data: OptimizePathRequestDto) =>
    post<ResponseResult<OptimizePathResponseDto>>('/api/routes/recalculate-path', data),

  confirm: (id: number) =>
    post<ResponseResult>(`/api/routes/${id}/confirm`, {}),

  start: (id: number) =>
    post<ResponseResult>(`/api/routes/${id}/start`, {}),

  complete: (id: number) =>
    post<ResponseResult>(`/api/routes/${id}/complete`, {}),

  cancel: (id: number) =>
    post<ResponseResult>(`/api/routes/${id}/cancel`, {}),

  downloadSheet: (id: number, reference: string) =>
    downloadFile(`/api/routes/${id}/download-sheet`, `Feuille-Route-${reference}.pdf`),
}
