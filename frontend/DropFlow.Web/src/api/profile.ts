import { get, put } from './client'

export interface UserProfileDto {
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
  fullName: string
}

export interface UpdateProfileDto {
  firstName: string
  lastName: string
  phoneNumber?: string
  address?: string
}

export interface ChangePasswordDto {
  currentPassword: string
  newPassword: string
  confirmNewPassword: string
}

export const profileKeys = {
  all: ['profile'] as const,
  current: () => [...profileKeys.all, 'current'] as const,
}

export const profileApi = {
  getProfile: () =>
    get<UserProfileDto>('/api/profile'),

  updateProfile: (data: UpdateProfileDto) =>
    put<{ message: string }>('/api/profile', data),

  changePassword: (data: ChangePasswordDto) =>
    put<{ message: string }>('/api/profile/password', data),
}
