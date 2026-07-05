import { get, post, put, del } from './client'
import type { PagedResult } from './clients'

export interface TenantDto {
  id: number
  name: string
  subDomain?: string
  planType: string
  isActive: boolean
  companyName?: string
  logoUrl?: string
  address?: string
  zipCode?: string
  city?: string
  phone?: string
  email?: string
  website?: string
  siret?: string
  vatNumber?: string
  legalForm?: string
  legalMentions?: string
  bankDetails?: string
  createdDate: string
  modifiedDate?: string
}

export interface UpdateCompanyInfoDto {
  companyName?: string
  address?: string
  zipCode?: string
  city?: string
  phone?: string
  email?: string
  website?: string
}

export interface UpdateLegalInfoDto {
  siret?: string
  vatNumber?: string
  legalForm?: string
  legalMentions?: string
  bankDetails?: string
}

export interface TenantDepotDto {
  id: number
  tenantId: number
  name: string
  fullAddress: string
  city?: string
  zipCode?: string
  latitude?: number
  longitude?: number
  isDefault: boolean
  isActive: boolean
  createdDate: string
  modifiedDate?: string
}

export interface CreateDepotDto {
  name: string
  fullAddress: string
  city?: string
  zipCode?: string
  latitude?: number
  longitude?: number
  isDefault: boolean
  isActive: boolean
}

export interface UpdateDepotDto {
  name: string
  fullAddress: string
  city?: string
  zipCode?: string
  latitude?: number
  longitude?: number
}

export interface DepotFilterDto {
  page?: number
  pageSize?: number
  includeInactive?: boolean
}

export const settingsKeys = {
  company: ['settings', 'company'] as const,
  depots: {
    all: ['settings', 'depots'] as const,
    lists: () => [...settingsKeys.depots.all, 'list'] as const,
    list: (filters: DepotFilterDto) => [...settingsKeys.depots.lists(), filters] as const,
    detail: (id: number) => [...settingsKeys.depots.all, 'detail', id] as const,
  },
}

function buildDepotQuery(filters: DepotFilterDto): string {
  const params = new URLSearchParams()
  if (filters.page) params.set('page', String(filters.page))
  if (filters.pageSize) params.set('pageSize', String(filters.pageSize))
  if (filters.includeInactive) params.set('includeInactive', 'true')
  const q = params.toString()
  return q ? `?${q}` : ''
}

export const settingsApi = {
  getCompany: () =>
    get<TenantDto>('/api/tenants/current'),

  updateCompanyInfo: (data: UpdateCompanyInfoDto) =>
    put<void>('/api/tenants/company-info', data),

  updateLegalInfo: (data: UpdateLegalInfoDto) =>
    put<void>('/api/tenants/legal-info', data),

  getDepots: (filters: DepotFilterDto) =>
    get<PagedResult<TenantDepotDto>>(`/api/tenants/depots${buildDepotQuery(filters)}`),

  getAllDepots: () =>
    get<TenantDepotDto[]>('/api/tenants/depots/all'),

  createDepot: (data: CreateDepotDto) =>
    post<number>('/api/tenants/depots', data),

  updateDepot: (id: number, data: UpdateDepotDto) =>
    put<void>(`/api/tenants/depots/${id}`, data),

  deleteDepot: (id: number) =>
    del<void>(`/api/tenants/depots/${id}`),

  setDefaultDepot: (id: number) =>
    post<void>(`/api/tenants/depots/${id}/set-default`),

  toggleDepotStatus: (id: number) =>
    post<void>(`/api/tenants/depots/${id}/toggle-status`),
}
