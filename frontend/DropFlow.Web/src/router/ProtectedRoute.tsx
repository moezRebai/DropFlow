import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore, type UserRole } from '@/store/authStore'

interface ProtectedRouteProps {
  allowedRoles?: UserRole[]
  isSuperAdmin?: boolean
}

export function ProtectedRoute({ allowedRoles, isSuperAdmin }: ProtectedRouteProps) {
  const { isAuthenticated, user } = useAuthStore()

  if (!isAuthenticated || !user) {
    return <Navigate to="/login" replace />
  }

  if (isSuperAdmin && user.tenantId !== 0) {
    return <Navigate to="/access-denied" replace />
  }

  if (allowedRoles && !allowedRoles.includes(user.role)) {
    return <Navigate to="/access-denied" replace />
  }

  return <Outlet />
}
