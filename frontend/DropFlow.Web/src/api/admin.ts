import { get, post, put, del } from './client'

// ─── Platform stats ─────────────────────────────────────────────────────────

export interface GlobalStatsDto {
  totalTenants: number
  activeTenants: number
  inactiveTenants: number
  totalUsers: number
  activeUsers: number
  tenantsCreatedThisMonth: number
  tenantsCreatedThisWeek: number
  usersCreatedThisMonth: number
  tenantsByPlan: Record<string, number>
}

export interface UserStatsDto {
  totalUsers: number
  activeUsers: number
  inactiveUsers: number
  usersCreatedThisMonth: number
  usersCreatedThisWeek: number
  usersByRole: Record<string, number>
  usersByTenant: Record<string, number>
}

// ─── Tenants ────────────────────────────────────────────────────────────────

export interface AdminTenantDto {
  id: number
  name: string
  subDomain?: string
  planType: string
  maxUsers: number
  maxDeliveries: number
  isActive: boolean
  createdDate: string
  expiryDate?: string
  userCount: number
  activeUserCount: number
  lastActivityDate?: string
}

export interface TenantUserDto {
  id: string
  email: string
  firstName: string
  lastName: string
  phoneNumber?: string
  role: string
  isActive: boolean
  createdDate: string
  lastLoginDate?: string
  tenantId: number
  tenantName: string
  fullName: string
}

export interface TenantDetailsDto extends AdminTenantDto {
  recentUsers: TenantUserDto[]
}

export interface UpdateTenantPlanDto {
  planType: string
  maxUsers: number
  maxDeliveries: number
  expiryDate?: string | null
}

// ─── Global users ───────────────────────────────────────────────────────────

export interface AdminUserDto {
  id: string
  email: string
  firstName: string
  lastName: string
  phoneNumber: string
  address: string
  role: string
  tenantId: number
  tenantName: string
  isActive: boolean
  createdDate: string
  lastLoginDate?: string
  deletedDate?: string
  fullName: string
  isDeleted: boolean
}

export interface AdminUsersFilter {
  tenantId?: number
  role?: string
  isActive?: boolean
  searchTerm?: string
  includeDeactivated?: boolean
  includeDeleted?: boolean
  pageNumber?: number
  pageSize?: number
}

// ─── Audit logs ─────────────────────────────────────────────────────────────

export type AuditSeverity = 'Info' | 'Warning' | 'Critical'

export interface AuditLogDto {
  id: number
  tenantId: number
  tenantName?: string
  userId?: string
  userEmail?: string
  action: string
  entityName: string
  entityId?: number
  changes?: string
  severity: AuditSeverity
  timestamp: string
}

export interface AuditLogFilter {
  tenantId?: number
  userId?: string
  action?: string
  severity?: AuditSeverity
  startDate?: string
  endDate?: string
  pageNumber?: number
  pageSize?: number
}

// ─── Reference data ─────────────────────────────────────────────────────────

export const PLAN_TYPES = ['Free', 'Starter', 'Business', 'Enterprise'] as const
export type PlanType = typeof PLAN_TYPES[number]

export const PLAN_LABELS: Record<string, string> = {
  Free: 'Gratuit',
  Starter: 'Starter',
  Business: 'Business',
  Enterprise: 'Enterprise',
}

export const PLAN_COLORS: Record<string, string> = {
  Free: 'bg-muted text-muted-foreground',
  Starter: 'bg-sky-100 text-sky-700 dark:bg-sky-500/15 dark:text-sky-400',
  Business: 'bg-violet-100 text-violet-700 dark:bg-violet-500/15 dark:text-violet-400',
  Enterprise: 'bg-amber-100 text-amber-700 dark:bg-amber-500/15 dark:text-amber-400',
}

// Default resource limits per plan — used to pre-fill the plan dialog (editable).
export const PLAN_DEFAULTS: Record<string, { maxUsers: number; maxDeliveries: number }> = {
  Free: { maxUsers: 3, maxDeliveries: 50 },
  Starter: { maxUsers: 10, maxDeliveries: 500 },
  Business: { maxUsers: 50, maxDeliveries: 5000 },
  Enterprise: { maxUsers: 999, maxDeliveries: 100000 },
}

export const SEVERITY_LABELS: Record<string, string> = {
  Info: 'Info',
  Warning: 'Avertissement',
  Critical: 'Critique',
}

export const SEVERITY_COLORS: Record<string, string> = {
  Info: 'bg-blue-100 text-blue-700 dark:bg-blue-500/15 dark:text-blue-400',
  Warning: 'bg-amber-100 text-amber-700 dark:bg-amber-500/15 dark:text-amber-400',
  Critical: 'bg-red-100 text-red-700 dark:bg-red-500/15 dark:text-red-400',
}

// ─── Query keys ─────────────────────────────────────────────────────────────

export const adminKeys = {
  all: ['admin'] as const,
  stats: () => ['admin', 'stats'] as const,
  userStats: () => ['admin', 'user-stats'] as const,
  tenants: {
    all: ['admin', 'tenants'] as const,
    list: () => ['admin', 'tenants', 'list'] as const,
    detail: (id: number) => ['admin', 'tenants', 'detail', id] as const,
    users: (id: number) => ['admin', 'tenants', id, 'users'] as const,
  },
  users: {
    all: ['admin', 'users'] as const,
    list: (filter: AdminUsersFilter) => ['admin', 'users', 'list', filter] as const,
  },
  audit: (filter: AuditLogFilter) => ['admin', 'audit', filter] as const,
}

// ─── Query string builders ──────────────────────────────────────────────────

function buildQuery(params: Record<string, unknown>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      search.set(key, String(value))
    }
  }
  const q = search.toString()
  return q ? `?${q}` : ''
}

// ─── API ────────────────────────────────────────────────────────────────────

export const adminApi = {
  // Stats
  getStats: () => get<GlobalStatsDto>('/api/admin/stats'),
  getUserStats: () => get<UserStatsDto>('/api/admin/users/stats'),

  // Tenants
  getTenants: () => get<AdminTenantDto[]>('/api/admin/tenants'),
  getTenant: (id: number) => get<TenantDetailsDto>(`/api/admin/tenants/${id}`),
  getTenantUsers: (id: number) => get<TenantUserDto[]>(`/api/admin/tenants/${id}/users`),
  activateTenant: (id: number) => post<{ message: string }>(`/api/admin/tenants/${id}/activate`),
  deactivateTenant: (id: number) => post<{ message: string }>(`/api/admin/tenants/${id}/deactivate`),
  updateTenantPlan: (id: number, data: UpdateTenantPlanDto) =>
    put<{ message: string }>(`/api/admin/tenants/${id}/plan`, data),
  deleteTenant: (id: number) => del<{ message: string }>(`/api/admin/tenants/${id}`),

  // Global users
  getUsers: (filter: AdminUsersFilter) =>
    get<AdminUserDto[]>(`/api/admin/users${buildQuery(filter as Record<string, unknown>)}`),
  activateUser: (userId: string) =>
    post<{ message: string }>(`/api/admin/users/${userId}/activate`),
  deactivateUser: (userId: string) =>
    post<{ message: string }>(`/api/admin/users/${userId}/deactivate`),
  changeUserRole: (userId: string, newRole: string) =>
    put<{ message: string }>(`/api/usermanagement/users/${userId}/role`, { newRole }),
  deleteUser: (userId: string) =>
    del<{ message: string }>(`/api/usermanagement/users/${userId}`),
  restoreUser: (userId: string) =>
    post<{ message: string }>(`/api/usermanagement/users/${userId}/restore`),

  // Audit
  getAuditLogs: (filter: AuditLogFilter) =>
    get<AuditLogDto[]>(`/api/admin/audit${buildQuery(filter as Record<string, unknown>)}`),
}
