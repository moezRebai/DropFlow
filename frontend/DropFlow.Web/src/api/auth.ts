import { get, post } from './client'
import type { AuthUser, UserRole } from '@/store/authStore'

export interface LoginDto {
  email: string
  password: string
  tenantId: number
}

export interface RegisterDto {
  companyName: string
  firstName: string
  lastName: string
  email: string
  password: string
  confirmPassword: string
}

export interface ResetPasswordDto {
  email: string
  token: string
  tenantId: number
  newPassword: string
  confirmNewPassword: string
}

export interface AcceptInvitationDto {
  email: string
  token: string
  firstName: string
  lastName: string
  password: string
  confirmPassword: string
}

export interface AuthResult {
  success: boolean
  token?: string
  refreshToken?: string
  message?: string
  user?: {
    id: string
    email: string
    firstName: string
    lastName: string
    role: string
    tenantId: number
    tenantName?: string
  }
}

export interface TenantInfo {
  tenantId: number
  tenantName: string
  role: string
  isActive: boolean
}

export interface PasswordResetResponse {
  success: boolean
  message?: string
}

export function mapAuthResultToUser(result: AuthResult): AuthUser | null {
  if (!result.user) return null
  return {
    id: result.user.id,
    email: result.user.email,
    firstName: result.user.firstName,
    lastName: result.user.lastName,
    role: result.user.role as UserRole,
    tenantId: result.user.tenantId,
    tenantName: result.user.tenantName,
  }
}

export function getRedirectPath(role: UserRole, tenantId: number): string {
  if (role === 'Admin' && tenantId === 0) return '/admin/dashboard'
  if (role === 'Driver') return '/my-deliveries'
  return '/dashboard'
}

export const authApi = {
  login: (data: LoginDto) =>
    post<AuthResult>('/api/auth/login', data),

  register: (data: RegisterDto) =>
    post<AuthResult>('/api/auth/register', data),

  forgotPassword: (email: string) =>
    post<PasswordResetResponse>('/api/auth/forgot-password', { email }),

  resetPassword: (data: ResetPasswordDto) =>
    post<PasswordResetResponse>('/api/auth/reset-password', data),

  acceptInvitation: (data: AcceptInvitationDto) =>
    post<AuthResult>('/api/auth/accept-invitation', data),

  logout: (refreshToken: string) =>
    post<void>('/api/auth/logout', { refreshToken }),

  refresh: (refreshToken: string) =>
    post<AuthResult>('/api/auth/refresh', { refreshToken }),

  getTenants: (email: string) =>
    get<TenantInfo[]>(`/api/auth/tenants?email=${encodeURIComponent(email)}`),
}
