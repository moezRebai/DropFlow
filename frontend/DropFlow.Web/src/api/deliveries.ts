import { get, post, put, patch, del } from './client'

// ─── Enums ───────────────────────────────────────────────────────────────────

export enum DeliveryStatus {
  ToBePlanned = 0,
  Confirmed = 1,
  InProgress = 2,
  Delivered = 3,
  Canceled = 4,
}

export enum DeliveryType {
  Standard = 0,
  Urgent = 1,
}

// ─── Status helpers ───────────────────────────────────────────────────────────

export const STATUS_LABELS: Record<DeliveryStatus, string> = {
  [DeliveryStatus.ToBePlanned]: 'À planifier',
  [DeliveryStatus.Confirmed]: 'Confirmée',
  [DeliveryStatus.InProgress]: 'En cours',
  [DeliveryStatus.Delivered]: 'Livrée',
  [DeliveryStatus.Canceled]: 'Annulée',
}

export const STATUS_COLORS: Record<DeliveryStatus, string> = {
  [DeliveryStatus.ToBePlanned]: 'bg-amber-100 text-amber-700 border-amber-200',
  [DeliveryStatus.Confirmed]: 'bg-blue-100 text-blue-700 border-blue-200',
  [DeliveryStatus.InProgress]: 'bg-purple-100 text-purple-700 border-purple-200',
  [DeliveryStatus.Delivered]: 'bg-green-100 text-green-700 border-green-200',
  [DeliveryStatus.Canceled]: 'bg-gray-100 text-gray-500 border-gray-200',
}

export const TYPE_LABELS: Record<DeliveryType, string> = {
  [DeliveryType.Standard]: 'Standard',
  [DeliveryType.Urgent]: 'Urgente',
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

export interface DeliveryItemDto {
  id: number
  reference?: string
  designation: string
  quantity: number
  information?: string
}

export interface CreateDeliveryItemDto {
  reference?: string
  designation: string
  quantity: number
  information?: string
}

export interface UpdateDeliveryItemDto {
  id?: number
  reference?: string
  designation: string
  quantity: number
  information?: string
}

export interface TimeSlotDto {
  id: number
  label: string
  startTime: string
  endTime: string
}

export interface DeliveryViewDto {
  id: number
  sequentialNumber: number
  reference: string
  type: DeliveryType
  clientName: string
  city: string
  fullAddress: string
  latitude?: number
  longitude?: number
  storeName: string
  internalNotes?: string
  estimatedDurationMinutes?: number
  price: number
  scheduledDate?: string
  createdDate: string
  status: DeliveryStatus
  routeId?: number
  routeReference?: string
  urgentDriverName?: string
  withAssembly: boolean
  totalPackages: number
}

export interface DeliveryDto {
  id: number
  sequentialNumber: number
  reference: string
  type: DeliveryType
  typeDisplay: string
  clientId: number
  clientName: string
  clientPhone: string
  clientEmail?: string
  clientAddressId: number
  address: string
  zipCode: string
  city: string
  addressComplement?: string
  addressLabel?: string
  latitude?: number
  longitude?: number
  storeId: number
  storeName: string
  fileNumber: string
  scheduledDate?: string
  price: number
  clientPaymentAmount?: number
  storePaymentAmount?: number
  status: DeliveryStatus
  statusDisplay: string
  routeId?: number
  routeReference?: string
  estimatedArrivalTime?: string
  actualArrivalTime?: string
  urgentDriverId?: number
  urgentDriverName?: string
  estimatedDurationMinutes?: number
  timeSlotId?: number
  timeSlot?: TimeSlotDto
  withAssembly: boolean
  deliveryNotes?: string
  internalNotes?: string
  items: DeliveryItemDto[]
  totalPackages: number
  createdDate: string
  createdBy: string
}

export interface DeliveryFilterDto {
  page?: number
  pageSize?: number
  storeId?: number
  type?: DeliveryType
  statuses?: DeliveryStatus[]
  clientSearch?: string
  dateFrom?: string
  dateTo?: string
  routeId?: number
  globalSearch?: string
  sortBy?: string
  sortDescending?: boolean
  withIssues?: boolean
}

export interface CreateDeliveryDto {
  clientId?: number
  clientAddressId?: number
  clientFirstName: string
  clientLastName: string
  clientPhone: string
  clientEmail?: string
  addressLabel?: string
  address: string
  zipCode: string
  city: string
  addressComplement?: string
  storeId: number
  fileNumber: string
  scheduledDate?: string
  price: number
  clientPaymentAmount?: number
  storePaymentAmount?: number
  status: DeliveryStatus
  type: DeliveryType
  urgentDriverId?: number
  withAssembly: boolean
  deliveryNotes?: string
  internalNotes?: string
  estimatedDurationMinutes?: number
  timeSlotId?: number
  items: CreateDeliveryItemDto[]
}

export interface UpdateDeliveryDto extends CreateDeliveryDto {
  items: UpdateDeliveryItemDto[]
}

export interface DeliveryStatsDto {
  totalCount: number
  toBePlannedCount: number
  confirmedTodayCount: number
  plannedCount: number
  inProgressCount: number
  deliveredCount: number
  totalAmount: number
  totalClientPayment: number
  totalStorePayment: number
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface ResponseResult<T = void> {
  success: boolean
  message?: string
  data?: T
}

export interface BulkUpdateStatusRequest {
  deliveryIds: number[]
  status: DeliveryStatus
}

export interface BulkDeleteRequest {
  deliveryIds: number[]
}

// ─── Query keys ───────────────────────────────────────────────────────────────

export const deliveryKeys = {
  all: ['deliveries'] as const,
  lists: () => [...deliveryKeys.all, 'list'] as const,
  list: (filters: DeliveryFilterDto) => [...deliveryKeys.lists(), filters] as const,
  details: () => [...deliveryKeys.all, 'detail'] as const,
  detail: (id: number) => [...deliveryKeys.details(), id] as const,
  stats: () => [...deliveryKeys.all, 'stats'] as const,
  availableForRoute: (date: string, routeId?: number) => [...deliveryKeys.all, 'available-for-route', date, routeId] as const,
}

// ─── API ──────────────────────────────────────────────────────────────────────

function buildQuery(filters: DeliveryFilterDto): string {
  const params = new URLSearchParams()
  if (filters.page) params.set('page', String(filters.page))
  if (filters.pageSize) params.set('pageSize', String(filters.pageSize))
  if (filters.storeId) params.set('storeId', String(filters.storeId))
  if (filters.type !== undefined) params.set('type', String(filters.type))
  if (filters.statuses?.length) filters.statuses.forEach(s => params.append('statuses', String(s)))
  if (filters.clientSearch) params.set('clientSearch', filters.clientSearch)
  if (filters.dateFrom) params.set('dateFrom', filters.dateFrom)
  if (filters.dateTo) params.set('dateTo', filters.dateTo)
  if (filters.routeId) params.set('routeId', String(filters.routeId))
  if (filters.globalSearch) params.set('globalSearch', filters.globalSearch)
  if (filters.sortBy) params.set('sortBy', filters.sortBy)
  if (filters.sortDescending !== undefined) params.set('sortDescending', String(filters.sortDescending))
  if (filters.withIssues) params.set('withIssues', 'true')
  const q = params.toString()
  return q ? `?${q}` : ''
}

export const deliveriesApi = {
  getList: (filters: DeliveryFilterDto) =>
    get<PagedResult<DeliveryViewDto>>(`/api/deliveries${buildQuery(filters)}`),

  getById: (id: number) =>
    get<ResponseResult<DeliveryDto>>(`/api/deliveries/${id}`),

  getStats: () =>
    get<DeliveryStatsDto>('/api/deliveries/stats'),

  create: (data: CreateDeliveryDto) =>
    post<ResponseResult<number>>('/api/deliveries', data),

  update: (id: number, data: UpdateDeliveryDto) =>
    put<ResponseResult>(`/api/deliveries/${id}`, data),

  updateStatus: (id: number, status: DeliveryStatus) =>
    patch<ResponseResult>(`/api/deliveries/${id}/status`, { status }),

  delete: (id: number) =>
    del<ResponseResult>(`/api/deliveries/${id}`),

  duplicate: (id: number) =>
    post<ResponseResult<number>>(`/api/deliveries/${id}/duplicate`, {}),

  bulkUpdateStatus: (data: BulkUpdateStatusRequest) =>
    post<ResponseResult>('/api/deliveries/batch/status', data),

  bulkDelete: (data: BulkDeleteRequest) =>
    post<ResponseResult>('/api/deliveries/batch/delete', data),

  geocode: (id: number) =>
    post<ResponseResult<DeliveryDto>>(`/api/deliveries/${id}/geocode`, {}),

  availableForRoute: (date: string, currentRouteId?: number) => {
    const params = new URLSearchParams({ date })
    if (currentRouteId) params.set('currentRouteId', String(currentRouteId))
    return get<ResponseResult<DeliveryDto[]>>(`/api/deliveries/available-for-route?${params}`)
  },
}
