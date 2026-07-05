import { get, post, put, del } from './client'

export interface TeamUserDto {
  id: string
  email: string
  firstName: string
  lastName: string
  phone: string
  role: string
  isActive: boolean
}

export interface InviteUserDto {
  email: string
  role: string
}

export const ROLES = ['Admin', 'Manager', 'Driver', 'Accountant', 'ReadOnly'] as const
export type AppRole = typeof ROLES[number]

export const ROLE_LABELS: Record<string, string> = {
  Admin: 'Admin',
  Manager: 'Manager',
  Driver: 'Chauffeur',
  Accountant: 'Comptable',
  ReadOnly: 'Lecture seule',
}

export const teamKeys = {
  all: ['team'] as const,
  users: (includeDeactivated: boolean) => [...teamKeys.all, 'users', includeDeactivated] as const,
}

export const teamApi = {
  getUsers: (includeDeactivated = false) =>
    get<TeamUserDto[]>(`/api/usermanagement/users?includeDeactivated=${includeDeactivated}`),

  invite: (data: InviteUserDto) =>
    post<void>('/api/usermanagement/invite', data),

  changeRole: (userId: string, newRole: string) =>
    put<void>(`/api/usermanagement/users/${userId}/role`, { newRole }),

  deactivate: (userId: string) =>
    post<void>(`/api/usermanagement/${userId}/deactivate`),

  activate: (userId: string) =>
    post<void>(`/api/usermanagement/${userId}/activate`),

  delete: (userId: string) =>
    del<void>(`/api/usermanagement/users/${userId}`),
}
