import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export type UserRole = 'Admin' | 'Manager' | 'Driver' | 'Accountant' | 'ReadOnly'

export interface AuthUser {
  id: string
  email: string
  firstName: string
  lastName: string
  role: UserRole
  tenantId: number
  tenantName?: string
}

interface AuthState {
  token: string | null
  refreshToken: string | null
  user: AuthUser | null
  isAuthenticated: boolean
  setAuth: (token: string, refreshToken: string, user: AuthUser) => void
  setTokens: (token: string, refreshToken: string) => void
  clearAuth: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      refreshToken: null,
      user: null,
      isAuthenticated: false,

      setAuth: (token, refreshToken, user) =>
        set({ token, refreshToken, user, isAuthenticated: true }),

      setTokens: (token, refreshToken) =>
        set({ token, refreshToken }),

      clearAuth: () =>
        set({ token: null, refreshToken: null, user: null, isAuthenticated: false }),
    }),
    {
      name: 'dropflow-auth',
      partialize: (state) => ({
        token: state.token,
        refreshToken: state.refreshToken,
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
    },
  ),
)
