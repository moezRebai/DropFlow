import { get, post, put, del } from './client'
import type { PagedResult } from './clients'

export interface DriverDto {
  id: number
  userId: string
  firstName: string
  lastName: string
  email: string
  phone: string
  licenseNumber?: string
  licenseExpiryDate?: string
  vehicleType?: string
  isActive: boolean
  createdDate: string
}

export interface CreateDriverDto {
  userId: string
  licenseNumber?: string
  licenseExpiryDate?: string
  vehicleType?: string
}

export interface UpdateDriverDto {
  licenseNumber?: string
  licenseExpiryDate?: string
  vehicleType?: string
  isActive: boolean
}

export interface DriverFilterDto {
  page?: number
  pageSize?: number
  searchTerm?: string
  isActive?: boolean
}

export const driverKeys = {
  all: ['drivers'] as const,
  lists: () => [...driverKeys.all, 'list'] as const,
  list: (filters: DriverFilterDto) => [...driverKeys.lists(), filters] as const,
  detail: (id: number) => [...driverKeys.all, 'detail', id] as const,
  active: ['drivers', 'active'] as const,
  available: (date: string) => ['drivers', 'available', date] as const,
}

function buildQuery(filters: DriverFilterDto): string {
  const params = new URLSearchParams()
  if (filters.page) params.set('page', String(filters.page))
  if (filters.pageSize) params.set('pageSize', String(filters.pageSize))
  if (filters.searchTerm) params.set('searchTerm', filters.searchTerm)
  if (filters.isActive !== undefined) params.set('isActive', String(filters.isActive))
  const q = params.toString()
  return q ? `?${q}` : ''
}

export const driversApi = {
  getList: (filters: DriverFilterDto) =>
    get<PagedResult<DriverDto>>(`/api/drivers${buildQuery(filters)}`),

  getById: (id: number) =>
    get<DriverDto>(`/api/drivers/${id}`),

  getActive: () =>
    get<DriverDto[]>('/api/drivers/active'),

  getAvailable: (date: string) =>
    get<DriverDto[]>(`/api/drivers/available?date=${date}`),

  create: (data: CreateDriverDto) =>
    post<number>('/api/drivers', data),

  update: (id: number, data: UpdateDriverDto) =>
    put<void>(`/api/drivers/${id}`, data),

  delete: (id: number) =>
    del<void>(`/api/drivers/${id}`),
}
