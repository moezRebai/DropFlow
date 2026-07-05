import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { lazy, Suspense } from 'react'
import { ProtectedRoute } from './ProtectedRoute'
import { useAuthStore } from '@/store/authStore'
import { AppLayout } from '@/layouts/AppLayout'
import { AuthLayout } from '@/layouts/AuthLayout'
import { SettingsLayout } from '@/features/settings/SettingsLayout'
import { ErrorBoundary } from '@/features/errors/ErrorPage'

// Auth
const LoginPage = lazy(() => import('@/features/auth/LoginPage'))
const RegisterPage = lazy(() => import('@/features/auth/RegisterPage'))
const ForgotPasswordPage = lazy(() => import('@/features/auth/ForgotPasswordPage'))
const ResetPasswordPage = lazy(() => import('@/features/auth/ResetPasswordPage'))
const AcceptInvitationPage = lazy(() => import('@/features/auth/AcceptInvitationPage'))
const AccessDeniedPage = lazy(() => import('@/features/auth/AccessDeniedPage'))

// Dashboard
const DashboardPage = lazy(() => import('@/features/dashboard/DashboardPage'))

// Deliveries
const DeliveriesPage = lazy(() => import('@/features/deliveries/DeliveriesPage'))
const DeliveryDetailPage = lazy(() => import('@/features/deliveries/DeliveryDetailPage'))
const CreateDeliveryPage = lazy(() => import('@/features/deliveries/CreateDeliveryPage'))
const PersonalDeliveriesPage = lazy(() => import('@/features/deliveries/PersonalDeliveriesPage'))

// Routes / Tournées
const RoutesPage = lazy(() => import('@/features/routes/RoutesPage'))
const RouteDetailPage = lazy(() => import('@/features/routes/RouteDetailPage'))
const RouteWizard = lazy(() => import('@/features/routes/wizard/RouteWizard'))
const EditRoutePage = lazy(() => import('@/features/routes/EditRoutePage'))

// Clients
const ClientsPage = lazy(() => import('@/features/clients/ClientsPage'))

// Help
const HelpPage = lazy(() => import('@/features/help/HelpPage'))

// Errors
const NotFoundPage = lazy(() => import('@/features/errors/ErrorPage').then(m => ({ default: m.NotFoundPage })))

// Admin
const AdminDashboardPage = lazy(() => import('@/features/admin/AdminDashboardPage'))
const TenantsPage = lazy(() => import('@/features/admin/TenantsPage'))
const TenantDetailPage = lazy(() => import('@/features/admin/TenantDetailPage'))
const UsersPage = lazy(() => import('@/features/admin/UsersPage'))
const AuditLogsPage = lazy(() => import('@/features/admin/AuditLogsPage'))

// Profile
const ProfilePage = lazy(() => import('@/features/profile/ProfilePage'))

// Settings
const CompanySettings = lazy(() => import('@/features/settings/company/CompanySettings'))
const DriversPage = lazy(() => import('@/features/settings/drivers/DriversPage'))
const VehiclesPage = lazy(() => import('@/features/settings/vehicles/VehiclesPage'))
const StoresPage = lazy(() => import('@/features/settings/stores/StoresPage'))
const TimeSlotsPage = lazy(() => import('@/features/settings/timeslots/TimeSlotsPage'))
const TeamPage = lazy(() => import('@/features/settings/team/TeamPage'))
const SettingsHubPage = lazy(() => import('@/features/settings/SettingsHubPage'))



function HomeRedirect() {
  const user = useAuthStore(s => s.user)
  if (user?.role === 'Admin' && user.tenantId === 0) {
    return <Navigate to="/admin/dashboard" replace />
  }
  return <Navigate to={user?.role === 'Driver' ? '/my-deliveries' : '/dashboard'} replace />
}

function PageLoader() {
  return (
    <div className="flex h-screen items-center justify-center">
      <div className="h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent" />
    </div>
  )
}

export function AppRouter() {
  return (
    <BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
      <ErrorBoundary>
      <Suspense fallback={<PageLoader />}>
        <Routes>
          {/* Auth — non protégées */}
          <Route element={<AuthLayout />}>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/reset-password" element={<ResetPasswordPage />} />
            <Route path="/accept-invitation" element={<AcceptInvitationPage />} />
          </Route>

          <Route path="/access-denied" element={<AccessDeniedPage />} />

          {/* Routes protégées */}
          <Route element={<ProtectedRoute />}>
            <Route element={<AppLayout />}>
              <Route path="/" element={<HomeRedirect />} />

              {/* Profil & aide — tous les rôles authentifiés */}
              <Route path="/profile" element={<ProfilePage />} />
              <Route path="/help" element={<HelpPage />} />

              {/* Routes réservées aux non-Driver */}
              <Route element={<ProtectedRoute allowedRoles={['Admin', 'Manager', 'Accountant', 'ReadOnly']} />}>
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/clients" element={<ClientsPage />} />
                <Route path="/deliveries" element={<DeliveriesPage />} />
                <Route path="/deliveries/new" element={<CreateDeliveryPage />} />
                <Route path="/deliveries/:id" element={<DeliveryDetailPage />} />
                <Route path="/deliveries/:id/edit" element={<CreateDeliveryPage />} />
              </Route>

              {/* Livraisons personnelles — Driver (+ admin pour test) */}
              <Route path="/my-deliveries" element={<PersonalDeliveriesPage />} />

              {/* Tournées — Admin + Manager */}
              <Route element={<ProtectedRoute allowedRoles={['Admin', 'Manager']} />}>
                <Route path="/routes" element={<RoutesPage />} />
                <Route path="/routes/new" element={<RouteWizard />} />
                <Route path="/routes/:id" element={<RouteDetailPage />} />
                <Route path="/routes/:id/edit" element={<EditRoutePage />} />
              </Route>

              {/* Paramètres — Admin + Manager (sous-pages sensibles Admin only) */}
              <Route element={<ProtectedRoute allowedRoles={['Admin', 'Manager']} />}>
                <Route path="/settings" element={<SettingsHubPage />} />
                <Route element={<SettingsLayout />}>
                  <Route path="/settings/company" element={<CompanySettings />} />
                  <Route path="/settings/drivers" element={<DriversPage />} />
                  <Route path="/settings/vehicles" element={<VehiclesPage />} />
                  <Route path="/settings/stores" element={<StoresPage />} />
                  <Route path="/settings/timeslots" element={<TimeSlotsPage />} />
                  <Route path="/settings/team" element={<TeamPage />} />
                </Route>
              </Route>

              {/* Admin — Super Admin uniquement (tenantId === 0) */}
              <Route element={<ProtectedRoute allowedRoles={['Admin']} isSuperAdmin />}>
                <Route path="/admin" element={<Navigate to="/admin/dashboard" replace />} />
                <Route path="/admin/dashboard" element={<AdminDashboardPage />} />
                <Route path="/admin/tenants" element={<TenantsPage />} />
                <Route path="/admin/tenants/:id" element={<TenantDetailPage />} />
                <Route path="/admin/users" element={<UsersPage />} />
                <Route path="/admin/audit-logs" element={<AuditLogsPage />} />
              </Route>
            </Route>
          </Route>

          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </Suspense>
      </ErrorBoundary>
    </BrowserRouter>
  )
}
