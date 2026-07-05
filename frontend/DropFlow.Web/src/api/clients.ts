import { get, post, put, del } from './client'

// ─── Shared pagination types ──────────────────────────────────────────────────

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

export interface ClientAddressDto {
  id: number
  clientId: number
  label?: string
  address: string
  zipCode: string
  city: string
  complement?: string
  latitude?: number
  longitude?: number
  isDefault: boolean
  fullAddress: string
}

export interface ClientDto {
  id: number
  firstName: string
  lastName: string
  displayName: string
  phone: string
  email?: string
  isActive: boolean
  addresses: ClientAddressDto[]
  totalDeliveries: number
  totalRevenue: number
  createdDate: string
}

export interface ClientDeliveryDto {
  id: number
  reference: string
  storeName?: string
  scheduledDate: string
  createdDate: string
  status: number
  price: number
}

export interface ClientFilterDto {
  page?: number
  pageSize?: number
  searchTerm?: string
}

export interface CreateClientAddressDto {
  label?: string
  address: string
  zipCode: string
  city: string
  complement?: string
}

export interface CreateClientDto {
  firstName: string
  lastName: string
  phone: string
  email?: string
  address: CreateClientAddressDto
}

export interface UpdateClientDto {
  firstName: string
  lastName: string
  phone: string
  email?: string
  isActive: boolean
}

export interface UpdateClientAddressDto {
  label?: string
  address: string
  zipCode: string
  city: string
  complement?: string
}

export interface ClientAddressLookupDto {
  id: number
  label?: string
  fullAddress: string
  address: string
  zipCode: string
  city: string
  isDefault: boolean
}

export interface ClientLookupDto {
  id: number
  displayName: string
  phone: string
  email?: string
  addresses: ClientAddressLookupDto[]
}

// ─── Query keys ───────────────────────────────────────────────────────────────

export const clientKeys = {
  all: ['clients'] as const,
  lists: () => [...clientKeys.all, 'list'] as const,
  list: (filters: ClientFilterDto) => [...clientKeys.lists(), filters] as const,
  details: () => [...clientKeys.all, 'detail'] as const,
  detail: (id: number) => [...clientKeys.details(), id] as const,
  addresses: (id: number) => [...clientKeys.detail(id), 'addresses'] as const,
  deliveries: (id: number) => [...clientKeys.detail(id), 'deliveries'] as const,
  search: (q: string) => ['clients', 'search', q] as const,
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

function buildQuery(filters: ClientFilterDto): string {
  const params = new URLSearchParams()
  if (filters.page) params.set('page', String(filters.page))
  if (filters.pageSize) params.set('pageSize', String(filters.pageSize))
  if (filters.searchTerm) params.set('searchTerm', filters.searchTerm)
  const q = params.toString()
  return q ? `?${q}` : ''
}

// ─── API ──────────────────────────────────────────────────────────────────────

export const clientsApi = {
  getList: (filters: ClientFilterDto) =>
    get<PagedResult<ClientDto>>(`/api/clients${buildQuery(filters)}`),

  getById: (id: number) =>
    get<ClientDto>(`/api/clients/${id}`),

  getDeliveries: (id: number) =>
    get<ClientDeliveryDto[]>(`/api/clients/${id}/deliveries`),

  getAddresses: (id: number) =>
    get<ClientAddressDto[]>(`/api/clients/${id}/addresses`),

  create: (data: CreateClientDto) =>
    post<number>('/api/clients', data),

  update: (id: number, data: UpdateClientDto) =>
    put<void>(`/api/clients/${id}`, data),

  delete: (id: number) =>
    del<void>(`/api/clients/${id}`),

  addAddress: (id: number, data: CreateClientAddressDto) =>
    post<ClientAddressDto>(`/api/clients/${id}/addresses`, data),

  updateAddress: (id: number, addressId: number, data: UpdateClientAddressDto) =>
    put<void>(`/api/clients/${id}/addresses/${addressId}`, data),

  deleteAddress: (id: number, addressId: number) =>
    del<void>(`/api/clients/${id}/addresses/${addressId}`),

  setDefaultAddress: (id: number, addressId: number) =>
    put<void>(`/api/clients/${id}/addresses/${addressId}/set-default`, {}),

  search: (query: string) =>
    get<ClientLookupDto[]>(`/api/clients/search?query=${encodeURIComponent(query)}`),
}
