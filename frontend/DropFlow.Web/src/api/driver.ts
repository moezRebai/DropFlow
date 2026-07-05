import { get, post } from './client'

// ─── DTOs ───────────────────────────────────────────────────────────────────

export interface DriverDeliveryListDto {
  id: number
  sequenceOrder: number
  reference: string
  clientName: string
  city: string
  zipCode: string
  timeSlotName?: string | null
  estimatedArrivalTime?: string | null
  status: number
  statusDisplay: string
  withAssembly: boolean
  totalPackages: number
  hasClientPayment: boolean
  isClientAbsent: boolean
  isValidated: boolean
}

export interface DriverRouteDto {
  routeId: number
  reference: string
  date: string
  vehicleName: string
  departureAddress: string
  startTime: string
  estimatedEndTime?: string | null
  status: number
  statusDisplay: string
  totalDeliveries: number
  totalDistanceKm: number
  totalDurationMinutes: number
  teamMembers: string[]
  deliveries: DriverDeliveryListDto[]
}

export interface DriverTodayResponse {
  hasRoute: boolean
  message?: string | null
  route?: DriverRouteDto | null
}

export interface DriverRouteSummaryDto {
  routeId: number
  reference: string
  date: string
  status: number
  statusDisplay: string
  vehicleName: string
  totalDeliveries: number
  deliveredCount: number
  startTime?: string | null
  totalDistanceKm: number
}

export interface DriverDashboardResponse {
  todayRoute: DriverTodayResponse
  upcomingRoutes: DriverRouteSummaryDto[]
}

export interface DriverDeliveryItemDto {
  reference?: string | null
  designation: string
  quantity: number
  information?: string | null
}

export interface DriverDeliveryDetailDto {
  id: number
  sequenceOrder: number
  reference: string
  clientFirstName: string
  clientLastName: string
  clientName: string
  clientPhone: string
  clientEmail?: string | null
  address: string
  zipCode: string
  city: string
  addressComplement?: string | null
  fullAddress: string
  latitude?: number | null
  longitude?: number | null
  storeName: string
  fileNumber?: string | null
  scheduledDate?: string | null
  timeSlotName?: string | null
  estimatedArrivalTime?: string | null
  withAssembly: boolean
  totalPackages: number
  clientPaymentAmount?: number | null
  deliveryNotes?: string | null
  items: DriverDeliveryItemDto[]
  status: number
  statusDisplay: string
  isValidated: boolean
  isClientAbsent: boolean
  validationComment?: string | null
  deliveredDateTime?: string | null
  hasSignature: boolean
  hasPhoto: boolean
}

export interface DriverHistoryDeliveryDto {
  id: number
  reference: string
  date?: string | null
  clientName: string
  city: string
  status: number
  statusDisplay: string
  isClientAbsent: boolean
  deliveredDateTime?: string | null
  routeReference: string
}

export interface DriverHistoryResponse {
  deliveries: DriverHistoryDeliveryDto[]
  totalCount: number
}

export interface ValidateDeliveryDto {
  signatureBase64?: string | null
  photoBase64?: string | null
  comment?: string | null
  isClientAbsent: boolean
}

// ─── Route status (matches backend RouteStatus enum) ────────────────────────

export const ROUTE_STATUS = {
  Draft: 0,
  Confirmed: 1,
  InProgress: 2,
  Completed: 3,
  Cancelled: 4,
} as const

// ─── Query keys ─────────────────────────────────────────────────────────────

export const driverKeys = {
  all: ['driver'] as const,
  dashboard: () => [...driverKeys.all, 'dashboard'] as const,
  today: () => [...driverKeys.all, 'today'] as const,
  upcoming: () => [...driverKeys.all, 'upcoming'] as const,
  route: (id: number) => [...driverKeys.all, 'route', id] as const,
  delivery: (id: number) => [...driverKeys.all, 'delivery', id] as const,
  history: (page: number, pageSize: number) => [...driverKeys.all, 'history', page, pageSize] as const,
}

// ─── API ────────────────────────────────────────────────────────────────────

export const driverApi = {
  getDashboard: () => get<DriverDashboardResponse>('/api/driver/dashboard'),
  getToday: () => get<DriverTodayResponse>('/api/driver/route/today'),
  getUpcoming: () => get<DriverRouteSummaryDto[]>('/api/driver/routes/upcoming'),
  getRoute: (id: number) => get<DriverTodayResponse>(`/api/driver/routes/${id}`),
  getDelivery: (id: number) => get<DriverDeliveryDetailDto>(`/api/driver/deliveries/${id}`),
  validateDelivery: (id: number, data: ValidateDeliveryDto) =>
    post<{ message: string }>(`/api/driver/deliveries/${id}/validate`, data),
  startRoute: (id: number) => post<{ message: string }>(`/api/driver/route/${id}/start`),
  completeRoute: (id: number) => post<{ message: string }>(`/api/driver/route/${id}/complete`),
  getHistory: (page = 1, pageSize = 20) =>
    get<DriverHistoryResponse>(`/api/driver/deliveries/history?page=${page}&pageSize=${pageSize}`),
}
