import { get, post, put, del } from './client'
import type { PagedResult } from './clients'

export interface StoreLookupDto {
  id: number
  name: string
  city: string
}

export interface StoreDto {
  id: number
  name: string
  address: string
  zipCode: string
  city: string
  contactName: string
  phone: string
  email: string
  notes: string
  isActive: boolean
  createdDate: string
}

export interface CreateStoreDto {
  name: string
  address: string
  zipCode: string
  city: string
  contactName: string
  phone: string
  email: string
  notes: string
}

export interface UpdateStoreDto extends CreateStoreDto {
  isActive: boolean
}

export interface StoreFilterDto {
  page?: number
  pageSize?: number
  searchTerm?: string
  isActive?: boolean
}

export const storeKeys = {
  all: ['stores'] as const,
  lists: () => [...storeKeys.all, 'list'] as const,
  list: (filters: StoreFilterDto) => [...storeKeys.lists(), filters] as const,
  detail: (id: number) => [...storeKeys.all, 'detail', id] as const,
  lookup: ['stores', 'lookup'] as const,
}

function buildQuery(filters: StoreFilterDto): string {
  const params = new URLSearchParams()
  if (filters.page) params.set('page', String(filters.page))
  if (filters.pageSize) params.set('pageSize', String(filters.pageSize))
  if (filters.searchTerm) params.set('searchTerm', filters.searchTerm)
  if (filters.isActive !== undefined) params.set('isActive', String(filters.isActive))
  const q = params.toString()
  return q ? `?${q}` : ''
}

export const storesApi = {
  getList: (filters: StoreFilterDto) =>
    get<PagedResult<StoreDto>>(`/api/stores${buildQuery(filters)}`),

  getById: (id: number) =>
    get<StoreDto>(`/api/stores/${id}`),

  getLookup: () =>
    get<StoreLookupDto[]>('/api/stores/lookup'),

  create: (data: CreateStoreDto) =>
    post<number>('/api/stores', data),

  update: (id: number, data: UpdateStoreDto) =>
    put<void>(`/api/stores/${id}`, data),

  delete: (id: number) =>
    del<void>(`/api/stores/${id}`),
}
