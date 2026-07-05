import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import { authApi } from '@/api/auth'

export function useLogout() {
  const navigate = useNavigate()
  const clearAuth = useAuthStore(s => s.clearAuth)

  return async function logout() {
    const refreshToken = useAuthStore.getState().refreshToken
    if (refreshToken) {
      try { await authApi.logout(refreshToken) } catch {}
    }
    clearAuth()
    navigate('/login', { replace: true })
  }
}
