import { useState } from 'react'
import { Link, Outlet, useLocation } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  AlertTriangle, Bell, Building2, CheckCircle, ChevronDown,
  ChevronLeft, ChevronRight, HelpCircle, Info, LayoutDashboard,
  LogOut, Menu, Package, PackageCheck, Route, ScrollText,
  Settings, UserCircle, Users, Users2, X, XCircle,
} from 'lucide-react'
import { dashboardApi, dashboardKeys } from '@/api/dashboard'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { DropflowLogo } from '@/components/shared/DropflowLogo'
import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem,
  DropdownMenuSeparator, DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Sheet, SheetContent } from '@/components/ui/sheet'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'
import { cn } from '@/lib/utils'
import { useAuthStore, type UserRole } from '@/store/authStore'
import { useLogout } from '@/hooks/useLogout'

interface NavItem {
  path: string
  label: string
  icon: React.ElementType
  badge?: number
}

type Zone = 'standard' | 'admin'

function getZone(role: UserRole, tenantId: number): Zone {
  return role === 'Admin' && tenantId === 0 ? 'admin' : 'standard'
}

const ZONE_GRADIENT: Record<Zone, string> = {
  standard: 'from-sky-500 to-blue-600',
  admin: 'from-violet-600 to-indigo-700',
}

function getNavItems(role: UserRole, tenantId: number): NavItem[] {
  if (role === 'Admin' && tenantId === 0) {
    return [
      { path: '/admin/dashboard', label: 'Tableau de bord', icon: LayoutDashboard },
      { path: '/admin/tenants', label: 'Entreprises', icon: Building2 },
      { path: '/admin/users', label: 'Utilisateurs', icon: Users },
      { path: '/admin/audit-logs', label: "Logs d'audit", icon: ScrollText },
    ]
  }
  if (role === 'Driver') {
    return [
      { path: '/my-deliveries', label: 'Mes Livraisons', icon: PackageCheck },
      { path: '/profile', label: 'Mon Profil', icon: UserCircle },
    ]
  }
  const items: NavItem[] = [
    { path: '/dashboard', label: 'Tableau de bord', icon: LayoutDashboard },
    { path: '/deliveries', label: 'Livraisons', icon: Package },
    { path: '/routes', label: 'Tournées', icon: Route },
    { path: '/clients', label: 'Clients', icon: Users2 },
  ]
  if (role === 'Admin' || role === 'Manager') {
    items.push({ path: '/settings', label: 'Paramètres', icon: Settings })
  }
  return items
}

// ─── Sidebar content ──────────────────────────────────────────────────────────

interface SidebarContentProps {
  collapsed?: boolean
  onNavigate?: () => void
}

function NavLink({ item, isActive, collapsed, gradient, onNavigate }: {
  item: NavItem
  isActive: boolean
  collapsed: boolean
  gradient: string
  onNavigate?: () => void
}) {
  const link = (
    <Link
      to={item.path}
      onClick={onNavigate}
      aria-current={isActive ? 'page' : undefined}
      className={cn(
        'group relative z-10 flex items-center rounded-lg py-2 text-sm transition-colors duration-200',
        collapsed ? 'justify-center px-2' : 'gap-3 pr-3',
        isActive
          ? 'bg-sidebar-accent/70 font-semibold text-sidebar-foreground'
          : 'font-medium text-sidebar-foreground/65 hover:bg-sidebar-accent/40 hover:text-sidebar-foreground',
      )}
    >
      <span className={cn(
        'flex h-8 w-8 shrink-0 items-center justify-center rounded-lg transition-all duration-200',
        isActive
          ? cn('bg-gradient-to-br text-white shadow-sm', gradient)
          : 'text-sidebar-foreground/50 group-hover:text-sidebar-foreground',
      )}>
        <item.icon className="h-4 w-4" />
      </span>
      {!collapsed && (
        <>
          <span className="flex-1 truncate">{item.label}</span>
          {item.badge !== undefined && item.badge > 0 && (
            <Badge variant={isActive ? 'secondary' : 'default'} className="text-xs">
              {item.badge}
            </Badge>
          )}
        </>
      )}
    </Link>
  )

  if (!collapsed) return link

  return (
    <Tooltip delayDuration={200}>
      <TooltipTrigger asChild>{link}</TooltipTrigger>
      <TooltipContent side="right">{item.label}</TooltipContent>
    </Tooltip>
  )
}

function SidebarContent({ collapsed = false, onNavigate }: SidebarContentProps) {
  const { pathname } = useLocation()
  const user = useAuthStore(s => s.user)
  const logout = useLogout()

  const navItems = user ? getNavItems(user.role, user.tenantId) : []
  const zone = user ? getZone(user.role, user.tenantId) : 'standard'
  const gradient = ZONE_GRADIENT[zone]

  return (
    <TooltipProvider>
      <div className="flex h-full flex-col">

        {/* Logo */}
        <div className={cn(
          'relative flex shrink-0 items-center overflow-hidden border-b border-sidebar-border transition-all duration-200',
          collapsed ? 'h-14 justify-center px-0' : 'min-h-16 gap-2.5 px-4 py-3.5',
        )}>
          <div className={cn('pointer-events-none absolute inset-0 bg-gradient-to-br opacity-[0.06]', gradient)} />
          <div className={cn('relative flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br shadow-sm', gradient)}>
            <DropflowLogo className="h-4 w-4 text-white" />
          </div>
          {!collapsed && (
            <div className="relative flex min-w-0 flex-1 flex-col">
              <span className="text-sm font-bold leading-tight tracking-tight text-sidebar-foreground">DropFlow</span>
              {user?.tenantName ? (
                <span className="truncate text-xs leading-tight text-sidebar-foreground/55 mt-0.5">
                  {user.tenantName}
                </span>
              ) : zone === 'admin' && (
                <span className="truncate text-xs font-medium leading-tight mt-0.5 text-violet-600 dark:text-violet-400">
                  Super Admin
                </span>
              )}
            </div>
          )}
        </div>

        {/* Nav */}
        <ScrollArea className="flex-1">
          <nav className={cn('relative py-3', collapsed ? 'space-y-1 px-2' : 'space-y-0.5 px-3')}>
            {!collapsed && (
              <span aria-hidden className="pointer-events-none absolute bottom-3 left-7 top-3 z-0 w-px bg-sidebar-border" />
            )}
            {navItems.map(item => {
              const isActive = pathname === item.path || pathname.startsWith(item.path + '/')
              return (
                <NavLink
                  key={item.path}
                  item={item}
                  isActive={isActive}
                  collapsed={collapsed}
                  gradient={gradient}
                  onNavigate={onNavigate}
                />
              )
            })}
          </nav>
        </ScrollArea>

        {/* Footer */}
        <div className="shrink-0 border-t border-sidebar-border p-2">
          {collapsed ? (
            <Tooltip delayDuration={200}>
              <TooltipTrigger asChild>
                <Link
                  to="/help"
                  onClick={onNavigate}
                  className="flex items-center justify-center rounded-lg px-2 py-2 text-sm font-medium text-sidebar-foreground/60 transition-colors duration-200 hover:bg-sidebar-accent/50 hover:text-sidebar-foreground"
                >
                  <HelpCircle className="h-4 w-4 shrink-0" />
                </Link>
              </TooltipTrigger>
              <TooltipContent side="right">Aide &amp; Support</TooltipContent>
            </Tooltip>
          ) : (
            <Link
              to="/help"
              onClick={onNavigate}
              className="flex items-center gap-3 rounded-lg px-2 py-2 text-sm font-medium text-sidebar-foreground/60 transition-colors duration-200 hover:bg-sidebar-accent/50 hover:text-sidebar-foreground"
            >
              <HelpCircle className="h-4 w-4 shrink-0" />
              Aide &amp; Support
            </Link>
          )}

          {collapsed ? (
            <Tooltip delayDuration={200}>
              <TooltipTrigger asChild>
                <button
                  onClick={logout}
                  className="flex w-full items-center justify-center rounded-lg px-2 py-2 text-sm font-medium text-sidebar-foreground/60 transition-colors duration-200 hover:bg-destructive/10 hover:text-destructive"
                >
                  <LogOut className="h-4 w-4 shrink-0" />
                </button>
              </TooltipTrigger>
              <TooltipContent side="right">Déconnexion</TooltipContent>
            </Tooltip>
          ) : (
            <button
              onClick={logout}
              className="flex w-full cursor-pointer items-center gap-3 rounded-lg px-2 py-2 text-sm font-medium text-sidebar-foreground/60 transition-colors duration-200 hover:bg-destructive/10 hover:text-destructive"
            >
              <LogOut className="h-4 w-4 shrink-0" />
              Déconnexion
            </button>
          )}
        </div>
      </div>
    </TooltipProvider>
  )
}

// ─── Notifications Popover ────────────────────────────────────────────────────

const NOTIF_CONFIG: Record<string, {
  iconBg: string
  iconColor: string
  Icon: React.ElementType
}> = {
  Success: { iconBg: 'bg-emerald-100 dark:bg-emerald-500/15', iconColor: 'text-emerald-600 dark:text-emerald-400', Icon: CheckCircle },
  Warning: { iconBg: 'bg-amber-100 dark:bg-amber-500/15',     iconColor: 'text-amber-600 dark:text-amber-400',     Icon: AlertTriangle },
  Error:   { iconBg: 'bg-red-100 dark:bg-red-500/15',         iconColor: 'text-red-600 dark:text-red-400',         Icon: XCircle },
  Info:    { iconBg: 'bg-blue-100 dark:bg-blue-500/15',       iconColor: 'text-blue-600 dark:text-blue-400',       Icon: Info },
}

function NotificationsPopover() {
  const [dismissed, setDismissed] = useState<Set<number>>(new Set())

  const { data: notifications = [] } = useQuery({
    queryKey: dashboardKeys.notifications(),
    queryFn: () => dashboardApi.getNotifications(10),
    refetchInterval: 60_000,
  })

  const visible = notifications.filter(n => !dismissed.has(n.id))
  const count = visible.length

  function dismiss(id: number) {
    setDismissed(prev => new Set([...prev, id]))
  }

  function dismissAll() {
    setDismissed(new Set(notifications.map(n => n.id)))
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          className="relative flex h-9 w-9 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
          aria-label="Notifications"
        >
          <Bell className="h-5 w-5" />
          {count > 0 && (
            <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-red-500 px-1 text-[10px] font-bold leading-none text-white">
              {count > 9 ? '9+' : count}
            </span>
          )}
        </button>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end" className="w-80 overflow-hidden p-0" sideOffset={8}>

        {/* Header */}
        <div className="flex items-center justify-between border-b px-4 py-3">
          <div className="flex items-center gap-2">
            <Bell className="h-4 w-4 text-muted-foreground" />
            <span className="font-semibold text-foreground">Notifications</span>
            {count > 0 && (
              <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-red-100 px-1.5 text-xs font-bold text-red-600 dark:bg-red-500/15 dark:text-red-400">
                {count}
              </span>
            )}
          </div>
          {count > 0 && (
            <button
              onClick={dismissAll}
              className="text-xs font-medium text-sky-600 transition-colors hover:text-sky-700"
            >
              Tout effacer
            </button>
          )}
        </div>

        {/* Notification items — plain divs so dismiss doesn't close the dropdown */}
        <div className="max-h-80 overflow-y-auto">
          {visible.length === 0 ? (
            <div className="flex flex-col items-center gap-2 py-10 text-center">
              <div className="flex h-10 w-10 items-center justify-center rounded-full bg-muted text-muted-foreground">
                <Bell className="h-5 w-5" />
              </div>
              <p className="text-sm font-medium text-muted-foreground">Aucune notification</p>
              <p className="text-xs text-muted-foreground">Vous êtes à jour !</p>
            </div>
          ) : (
            <div className="divide-y">
              {visible.map(n => {
                const cfg = NOTIF_CONFIG[n.type] ?? NOTIF_CONFIG.Info
                return (
                  <div
                    key={n.id}
                    className="group flex items-start gap-3 px-4 py-3 transition-colors hover:bg-muted/50"
                  >
                    <div className={cn(
                      'mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-full',
                      cfg.iconBg, cfg.iconColor,
                    )}>
                      <cfg.Icon className="h-4 w-4" />
                    </div>
                    <div className="min-w-0 flex-1">
                      {n.title && (
                        <p className="truncate text-sm font-semibold text-foreground">{n.title}</p>
                      )}
                      <p className="text-xs leading-snug text-muted-foreground">{n.message}</p>
                      <p className="mt-1 text-xs text-muted-foreground">Il y a {n.timeAgo}</p>
                    </div>
                    <button
                      onClick={() => dismiss(n.id)}
                      className="mt-0.5 shrink-0 rounded p-0.5 text-muted-foreground/60 opacity-0 transition-all hover:bg-muted hover:text-foreground group-hover:opacity-100 focus-visible:opacity-100"
                      aria-label="Ignorer"
                    >
                      <X className="h-3.5 w-3.5" />
                    </button>
                  </div>
                )
              })}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="border-t p-1.5">
          <DropdownMenuItem asChild className="cursor-pointer rounded-lg px-3 py-2 focus:bg-muted">
            <Link to="/dashboard" className="flex items-center justify-between text-sm font-medium text-sky-600 dark:text-sky-400">
              Voir toutes les notifications
              <ChevronRight className="h-4 w-4" />
            </Link>
          </DropdownMenuItem>
        </div>

      </DropdownMenuContent>
    </DropdownMenu>
  )
}

// ─── Top bar ──────────────────────────────────────────────────────────────────

const ROLE_LABELS: Record<string, string> = {
  Admin: 'Super Admin',
  Manager: 'Manager',
  Driver: 'Chauffeur',
  Accountant: 'Comptable',
  ReadOnly: 'Lecture seule',
}

function TopBar({ onMenuClick }: { onMenuClick: () => void }) {
  const user = useAuthStore(s => s.user)
  const logout = useLogout()

  const initials = user
    ? `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase()
    : '?'

  const roleLabel = user ? (ROLE_LABELS[user.role] ?? user.role) : ''
  const showSettings = user?.role === 'Admin' || user?.role === 'Manager'

  return (
    <header className="flex h-14 shrink-0 items-center gap-3 border-b bg-card px-4">
      <Button variant="ghost" size="icon" className="md:hidden" onClick={onMenuClick}>
        <Menu className="h-5 w-5" />
      </Button>

      <div className="flex-1" />

      <NotificationsPopover />

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            className="flex h-10 items-center gap-2.5 rounded-xl px-2 hover:bg-muted"
          >
            <div className="relative shrink-0">
              <div className="flex h-8 w-8 items-center justify-center rounded-full bg-gradient-to-br from-sky-500 to-blue-600 text-xs font-bold text-white">
                {initials}
              </div>
              <span className="absolute -bottom-0.5 -right-0.5 h-2.5 w-2.5 rounded-full border-2 border-card bg-emerald-400" />
            </div>
            <div className="hidden flex-col items-start lg:flex">
              <span className="text-sm font-semibold leading-tight text-foreground">
                {user?.firstName} {user?.lastName}
              </span>
              <span className="text-xs leading-tight text-muted-foreground">{roleLabel}</span>
            </div>
            <ChevronDown className="hidden h-3.5 w-3.5 text-muted-foreground lg:block" />
          </Button>
        </DropdownMenuTrigger>

        <DropdownMenuContent align="end" className="w-72 overflow-hidden p-0" sideOffset={8}>

          {/* Gradient header */}
          <div className="bg-gradient-to-br from-sky-500 to-blue-600 px-4 py-4">
            <div className="flex items-center gap-3">
              <div className="relative shrink-0">
                <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-white/20 text-lg font-bold text-white ring-2 ring-white/30">
                  {initials}
                </div>
                <span className="absolute -bottom-0.5 -right-0.5 h-3 w-3 rounded-full border-2 border-sky-400 bg-emerald-400" />
              </div>
              <div className="min-w-0 flex-1">
                <p className="truncate font-semibold text-white">
                  {user?.firstName} {user?.lastName}
                </p>
                <p className="truncate text-xs text-sky-200">{user?.email}</p>
                <span className="mt-1.5 inline-flex items-center rounded-full bg-white/20 px-2 py-0.5 text-xs font-medium text-white">
                  {roleLabel}
                </span>
              </div>
            </div>
            {user?.tenantName && (
              <div className="mt-3 flex items-center gap-1.5 rounded-lg bg-white/10 px-2.5 py-1.5">
                <Building2 className="h-3.5 w-3.5 shrink-0 text-sky-200" />
                <span className="truncate text-xs text-sky-100">{user.tenantName}</span>
              </div>
            )}
          </div>

          {/* Navigation items */}
          <div className="p-1.5">
            <DropdownMenuItem asChild className="cursor-pointer rounded-lg p-0 focus:bg-muted">
              <Link to="/profile" className="flex items-center gap-3 px-3 py-2.5">
                <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-muted text-muted-foreground">
                  <UserCircle className="h-4 w-4" />
                </div>
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-medium text-foreground">Mon profil</p>
                  <p className="text-xs text-muted-foreground">Infos personnelles & sécurité</p>
                </div>
                <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground/50" />
              </Link>
            </DropdownMenuItem>

            {showSettings && (
              <DropdownMenuItem asChild className="cursor-pointer rounded-lg p-0 focus:bg-muted">
                <Link to="/settings" className="flex items-center gap-3 px-3 py-2.5">
                  <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-muted text-muted-foreground">
                    <Settings className="h-4 w-4" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-medium text-foreground">Paramètres</p>
                    <p className="text-xs text-muted-foreground">Configuration de l'entreprise</p>
                  </div>
                  <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground/50" />
                </Link>
              </DropdownMenuItem>
            )}
          </div>

          <DropdownMenuSeparator />

          {/* Logout */}
          <div className="p-1.5">
            <DropdownMenuItem
              onClick={logout}
              className="cursor-pointer rounded-lg px-3 py-2.5 text-red-600 dark:text-red-400 focus:bg-red-50 focus:text-red-700 dark:focus:bg-red-500/10 dark:focus:text-red-400"
            >
              <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-red-50 text-red-500 dark:bg-red-500/15 dark:text-red-400">
                <LogOut className="h-4 w-4" />
              </div>
              <div className="ml-3 min-w-0 flex-1">
                <p className="text-sm font-medium">Déconnexion</p>
                <p className="text-xs text-red-400 dark:text-red-400/70">Fermer la session en cours</p>
              </div>
            </DropdownMenuItem>
          </div>

        </DropdownMenuContent>
      </DropdownMenu>
    </header>
  )
}

// ─── App layout ───────────────────────────────────────────────────────────────

export function AppLayout() {
  const [mobileOpen, setMobileOpen] = useState(false)
  const [collapsed, setCollapsed] = useState(() =>
    localStorage.getItem('dropflow-sidebar-collapsed') === 'true'
  )

  const toggleCollapsed = () => {
    setCollapsed(v => {
      const next = !v
      localStorage.setItem('dropflow-sidebar-collapsed', String(next))
      return next
    })
  }

  return (
    <>
      <div className="flex h-screen bg-background">
        {/* Desktop sidebar */}
        <aside className={cn(
          'relative hidden shrink-0 flex-col border-r border-sidebar-border bg-sidebar transition-all duration-200 md:flex',
          collapsed ? 'w-14' : 'w-64',
        )}>
          <SidebarContent collapsed={collapsed} />

          <button
            onClick={toggleCollapsed}
            title={collapsed ? 'Développer le menu' : 'Réduire le menu'}
            className="absolute -right-3 top-16 z-20 flex h-6 w-6 items-center justify-center rounded-full border border-sidebar-border bg-card text-muted-foreground shadow-sm transition-all duration-200 hover:border-primary/40 hover:text-primary"
          >
            {collapsed ? <ChevronRight className="h-3.5 w-3.5" /> : <ChevronLeft className="h-3.5 w-3.5" />}
          </button>
        </aside>

        <div className="flex flex-1 flex-col overflow-hidden">
          <TopBar onMenuClick={() => setMobileOpen(true)} />
          <main className="flex-1 overflow-auto">
            <Outlet />
          </main>
        </div>
      </div>

      {/* Mobile sidebar */}
      <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
        <SheetContent side="left" className="w-64 bg-sidebar p-0">
          <SidebarContent onNavigate={() => setMobileOpen(false)} />
        </SheetContent>
      </Sheet>
    </>
  )
}
