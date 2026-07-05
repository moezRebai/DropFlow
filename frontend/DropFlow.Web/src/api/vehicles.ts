import { get, post, put, del } from './client'
import type { PagedResult } from './clients'

export interface VehicleDto {
  id: number
  brand: string
  model: string
  plateNumber: string
  maxDeliveries: number
  maxVolume: number
  isActive: boolean
  createdDate: string
}

export interface CreateVehicleDto {
  brand: string
  model: string
  plateNumber: string
  maxDeliveries: number
  maxVolume: number
}

export interface UpdateVehicleDto {
  brand: string
  model: string
  plateNumber: string
  maxDeliveries: number
  maxVolume: number
  isActive: boolean
}

export interface VehicleFilterDto {
  page?: number
  pageSize?: number
  searchTerm?: string
  isActive?: boolean
}

export const vehicleKeys = {
  all: ['vehicles'] as const,
  lists: () => [...vehicleKeys.all, 'list'] as const,
  list: (filters: VehicleFilterDto) => [...vehicleKeys.lists(), filters] as const,
  detail: (id: number) => [...vehicleKeys.all, 'detail', id] as const,
}

function buildQuery(filters: VehicleFilterDto): string {
  const params = new URLSearchParams()
  if (filters.page) params.set('page', String(filters.page))
  if (filters.pageSize) params.set('pageSize', String(filters.pageSize))
  if (filters.searchTerm) params.set('searchTerm', filters.searchTerm)
  if (filters.isActive !== undefined) params.set('isActive', String(filters.isActive))
  const q = params.toString()
  return q ? `?${q}` : ''
}

export const vehiclesApi = {
  getList: (filters: VehicleFilterDto) =>
    get<PagedResult<VehicleDto>>(`/api/vehicles${buildQuery(filters)}`),

  getById: (id: number) =>
    get<VehicleDto>(`/api/vehicles/${id}`),

  create: (data: CreateVehicleDto) =>
    post<number>('/api/vehicles', data),

  update: (id: number, data: UpdateVehicleDto) =>
    put<void>(`/api/vehicles/${id}`, data),

  delete: (id: number) =>
    del<void>(`/api/vehicles/${id}`),
}
