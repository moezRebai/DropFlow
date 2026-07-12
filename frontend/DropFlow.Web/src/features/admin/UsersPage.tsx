import { useState, useEffect, useMemo } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import {
  Users, Search, RefreshCw, ChevronLeft, ChevronRight, UserCog,
  UserX, UserCheck, Trash2, RotateCcw, Shield, Mail, Building2,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { cn } from '@/lib/utils'
import { adminApi, adminKeys } from '@/api/admin'
import type { AdminUserDto, AdminUsersFilter } from '@/api/admin'
import { ROLES, ROLE_LABELS } from '@/api/team'

const PAGE_SIZE = 50

type StatusFilter = 'all' | 'active' | 'inactive'

const ROLE_COLORS: Record<string, string> = {
  Admin: 'bg-purple-100 text-purple-700 dark:bg-purple-500/15 dark:text-purple-400',
  Manager: 'bg-blue-100 text-blue-700 dark:bg-blue-500/15 dark:text-blue-400',
  Driver: 'bg-sky-100 text-sky-700 dark:bg-sky-500/15 dark:text-sky-400',
  Livreur: 'bg-sky-100 text-sky-700 dark:bg-sky-500/15 dark:text-sky-400',
  Accountant: 'bg-amber-100 text-amber-700 dark:bg-amber-500/15 dark:text-amber-400',
  ReadOnly: 'bg-muted text-muted-foreground',
}

function roleLabel(role: string) {
  return ROLE_LABELS[role] ?? (role === 'Livreur' ? 'Chauffeur' : role)
}

function errMsg(err: unknown, fallback: string) {
  if (isAxiosError(err)) return err.response?.data?.message ?? err.response?.data?.errors?.[0] ?? fallback
  return fallback
}

function formatDate(iso?: string) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function StatChip({ icon, label }: { icon: React.ReactNode; label: string }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white">
      {icon}{label}
    </span>
  )
}

// ─── Change role modal ──────────────────────────────────────────────────────

const roleSchema = z.object({ newRole: z.string().min(1, 'Rôle requis') })
type RoleValues = z.infer<typeof roleSchema>

function ChangeRoleModal({ user, onClose }: { user: AdminUserDto; onClose: () => void }) {
  const qc = useQueryClient()
  const form = useForm<RoleValues>({
    resolver: zodResolver(roleSchema),
    defaultValues: { newRole: user.role },
  })
  const mutation = useMutation({
    mutationFn: (v: RoleValues) => adminApi.changeUserRole(user.id, v.newRole),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: adminKeys.users.all })
      toast.success('Rôle mis à jour')
      onClose()
    },
    onError: (e) => toast.error(errMsg(e, 'Mise à jour impossible')),
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-card shadow-2xl">
        <div className="bg-gradient-to-br from-violet-600 to-indigo-700 px-6 py-5">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
              <Shield className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="font-bold text-white">Changer le rôle</h2>
              <p className="text-xs text-violet-200">{user.fullName} · {user.tenantName}</p>
            </div>
          </div>
        </div>
        <form onSubmit={form.handleSubmit(v => mutation.mutate(v))} className="space-y-4 p-6">
          <div className="space-y-1.5">
            <Label>Nouveau rôle</Label>
            <Select defaultValue={user.role} onValueChange={val => form.setValue('newRole', val)}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                {ROLES.map(r => <SelectItem key={r} value={r}>{ROLE_LABELS[r]}</SelectItem>)}
              </SelectContent>
            </Select>
          </div>
          <div className="flex gap-3 pt-2">
            <Button type="button" variant="outline" className="flex-1" onClick={onClose}>Annuler</Button>
            <Button type="submit" className="flex-1" disabled={mutation.isPending}>
              {mutation.isPending ? 'Mise à jour…' : 'Confirmer'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ─── Delete modal ───────────────────────────────────────────────────────────

function DeleteModal({ user, onConfirm, onCancel, isPending }: {
  user: AdminUserDto; onConfirm: () => void; onCancel: () => void; isPending: boolean
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-card shadow-2xl">
        <div className="p-6">
          <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100 dark:bg-red-500/15">
            <Trash2 className="h-6 w-6 text-red-600 dark:text-red-400" />
          </div>
          <h3 className="mb-1 text-base font-semibold text-foreground">Supprimer cet utilisateur ?</h3>
          <p className="text-sm text-muted-foreground">
            Le compte de <strong>{user.fullName}</strong> ({user.email}) sera archivé (suppression réversible).
          </p>
        </div>
        <div className="flex gap-3 border-t px-6 py-4">
          <Button variant="outline" className="flex-1" onClick={onCancel}>Annuler</Button>
          <Button variant="destructive" className="flex-1" onClick={onConfirm} disabled={isPending}>
            {isPending ? 'Suppression…' : 'Supprimer'}
          </Button>
        </div>
      </div>
    </div>
  )
}

// ─── Page ───────────────────────────────────────────────────────────────────

type Dialog =
  | { type: 'none' }
  | { type: 'role'; user: AdminUserDto }
  | { type: 'delete'; user: AdminUserDto }

export default function UsersPage() {
  const qc = useQueryClient()
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [role, setRole] = useState<string>('all')
  const [status, setStatus] = useState<StatusFilter>('all')
  const [includeDeleted, setIncludeDeleted] = useState(false)
  const [page, setPage] = useState(1)
  const [dialog, setDialog] = useState<Dialog>({ type: 'none' })

  // Debounce search
  useEffect(() => {
    const t = setTimeout(() => { setSearch(searchInput); setPage(1) }, 350)
    return () => clearTimeout(t)
  }, [searchInput])

  const filter: AdminUsersFilter = useMemo(() => ({
    searchTerm: search || undefined,
    role: role === 'all' ? undefined : role,
    isActive: status === 'active' ? true : status === 'inactive' ? false : undefined,
    includeDeactivated: status !== 'active',
    includeDeleted,
    pageNumber: page,
    pageSize: PAGE_SIZE,
  }), [search, role, status, includeDeleted, page])

  const { data: users = [], isLoading, isFetching, refetch } = useQuery({
    queryKey: adminKeys.users.list(filter),
    queryFn: () => adminApi.getUsers(filter),
  })

  function invalidate() {
    qc.invalidateQueries({ queryKey: adminKeys.users.all })
    qc.invalidateQueries({ queryKey: adminKeys.userStats() })
  }

  const activateMutation = useMutation({
    mutationFn: (id: string) => adminApi.activateUser(id),
    onSuccess: () => { invalidate(); toast.success('Utilisateur activé') },
    onError: (e) => toast.error(errMsg(e, 'Activation impossible')),
  })
  const deactivateMutation = useMutation({
    mutationFn: (id: string) => adminApi.deactivateUser(id),
    onSuccess: () => { invalidate(); toast.success('Utilisateur désactivé') },
    onError: (e) => toast.error(errMsg(e, 'Désactivation impossible')),
  })
  const restoreMutation = useMutation({
    mutationFn: (id: string) => adminApi.restoreUser(id),
    onSuccess: () => { invalidate(); toast.success('Utilisateur restauré') },
    onError: (e) => toast.error(errMsg(e, 'Restauration impossible')),
  })
  const deleteMutation = useMutation({
    mutationFn: (id: string) => adminApi.deleteUser(id),
    onSuccess: () => { invalidate(); toast.success('Utilisateur supprimé'); setDialog({ type: 'none' }) },
    onError: (e) => toast.error(errMsg(e, 'Suppression impossible')),
  })

  const hasNext = users.length === PAGE_SIZE
  const hasPrev = page > 1

  function resetFilter<T>(setter: (v: T) => void) {
    return (v: T) => { setter(v); setPage(1) }
  }

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Hero */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-violet-600 to-indigo-700 p-6 shadow-lg">
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }} />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="mb-1 flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
                <Users className="h-5 w-5 text-white" />
              </div>
              <h1 className="text-2xl font-bold tracking-tight text-white">Utilisateurs</h1>
            </div>
            <p className="text-sm text-violet-200">Gestion des utilisateurs de toutes les entreprises</p>
          </div>
          <StatChip icon={<Users className="h-3.5 w-3.5" />} label={`Page ${page}${hasNext ? '' : ' (fin)'}`} />
        </div>
      </div>

      {/* Toolbar */}
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="relative w-full lg:max-w-xs">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input value={searchInput} onChange={e => setSearchInput(e.target.value)} placeholder="Rechercher (nom, email, téléphone)…" className="pl-9" />
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Select value={role} onValueChange={resetFilter(setRole)}>
            <SelectTrigger className="w-40"><SelectValue placeholder="Rôle" /></SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Tous les rôles</SelectItem>
              {ROLES.map(r => <SelectItem key={r} value={r}>{ROLE_LABELS[r]}</SelectItem>)}
            </SelectContent>
          </Select>
          <div className="flex gap-1 rounded-xl bg-muted p-1">
            {(['all', 'active', 'inactive'] as StatusFilter[]).map(s => (
              <button
                key={s}
                onClick={() => { setStatus(s); setPage(1) }}
                aria-pressed={status === s}
                className={cn(
                  'rounded-lg px-3 py-1.5 text-sm font-medium transition-all',
                  status === s ? 'bg-card text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
                )}
              >
                {s === 'all' ? 'Tous' : s === 'active' ? 'Actifs' : 'Inactifs'}
              </button>
            ))}
          </div>
          <label className="flex cursor-pointer items-center gap-2 text-sm text-muted-foreground">
            <input type="checkbox" checked={includeDeleted} onChange={e => { setIncludeDeleted(e.target.checked); setPage(1) }} className="h-4 w-4 rounded border-input text-violet-600 focus:ring-violet-500" />
            Supprimés
          </label>
          <Button variant="outline" size="icon" onClick={() => refetch()} title="Actualiser"><RefreshCw className={cn('h-4 w-4', isFetching && 'animate-spin')} /></Button>
        </div>
      </div>

      {/* Table */}
      <div className="overflow-hidden rounded-2xl border bg-card shadow-sm">
        <Table>
          <TableHeader>
            <TableRow className="hover:bg-transparent">
              <TableHead className="pl-6 text-xs font-semibold uppercase tracking-wider text-muted-foreground">Utilisateur</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Entreprise</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Rôle</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Statut</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Créé le</TableHead>
              <TableHead className="w-32 pr-6" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              [...Array(8)].map((_, i) => (
                <TableRow key={i}>
                  <TableCell className="pl-6"><Skeleton className="h-9 w-52" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                  <TableCell><Skeleton className="h-6 w-16 rounded-full" /></TableCell>
                  <TableCell><Skeleton className="h-6 w-16 rounded-full" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                </TableRow>
              ))
            ) : users.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6}>
                  <div className="flex flex-col items-center gap-3 py-16">
                    <div className="flex h-12 w-12 items-center justify-center rounded-full bg-muted text-muted-foreground">
                      <Users className="h-6 w-6" />
                    </div>
                    <p className="text-sm font-medium text-muted-foreground">Aucun utilisateur trouvé</p>
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              users.map(u => (
                <TableRow key={u.id} className={cn('hover:bg-violet-50/40 dark:hover:bg-violet-500/5', (!u.isActive || u.isDeleted) && 'opacity-60')}>
                  <TableCell className="py-3 pl-6">
                    <div className="flex items-center gap-3">
                      <div className={cn(
                        'flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-sm font-bold text-white',
                        u.isActive && !u.isDeleted ? 'bg-gradient-to-br from-violet-500 to-indigo-600' : 'bg-gradient-to-br from-slate-400 to-slate-500',
                      )}>
                        {(u.firstName.charAt(0) + u.lastName.charAt(0)).toUpperCase()}
                      </div>
                      <div>
                        <div className="flex items-center gap-1.5">
                          <p className="font-semibold text-foreground">{u.fullName}</p>
                          {u.isDeleted && <span className="rounded-full bg-red-100 px-1.5 py-0.5 text-xs font-medium text-red-600 dark:bg-red-500/15 dark:text-red-400">supprimé</span>}
                        </div>
                        <p className="flex items-center gap-1 text-xs text-muted-foreground"><Mail className="h-3 w-3" />{u.email}</p>
                      </div>
                    </div>
                  </TableCell>
                  <TableCell>
                    <span className="inline-flex items-center gap-1 text-sm text-muted-foreground">
                      <Building2 className="h-3.5 w-3.5 text-muted-foreground" />
                      {u.tenantName}
                    </span>
                  </TableCell>
                  <TableCell>
                    <span className={cn('inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium', ROLE_COLORS[u.role] ?? 'bg-muted text-muted-foreground')}>
                      {roleLabel(u.role)}
                    </span>
                  </TableCell>
                  <TableCell>
                    <span className={cn(
                      'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
                      u.isActive ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-400' : 'bg-muted text-muted-foreground',
                    )}>
                      {u.isActive ? 'Actif' : 'Inactif'}
                    </span>
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">{formatDate(u.createdDate)}</TableCell>
                  <TableCell className="pr-6">
                    <div className="flex items-center justify-end gap-1">
                      {u.isDeleted ? (
                        <button
                          onClick={() => restoreMutation.mutate(u.id)}
                          aria-label="Restaurer"
                          className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-emerald-50 hover:text-emerald-600 dark:hover:bg-emerald-500/10 dark:hover:text-emerald-400"
                          title="Restaurer"
                          disabled={restoreMutation.isPending}
                        >
                          <RotateCcw className="h-4 w-4" />
                        </button>
                      ) : (
                        <>
                          <button
                            onClick={() => setDialog({ type: 'role', user: u })}
                            aria-label="Changer le rôle"
                            className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                            title="Changer le rôle"
                          >
                            <UserCog className="h-4 w-4" />
                          </button>
                          {u.isActive ? (
                            <button
                              onClick={() => deactivateMutation.mutate(u.id)}
                              aria-label="Désactiver"
                              className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-amber-50 hover:text-amber-600 dark:hover:bg-amber-500/10 dark:hover:text-amber-400"
                              title="Désactiver"
                              disabled={deactivateMutation.isPending}
                            >
                              <UserX className="h-4 w-4" />
                            </button>
                          ) : (
                            <button
                              onClick={() => activateMutation.mutate(u.id)}
                              aria-label="Activer"
                              className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-emerald-50 hover:text-emerald-600 dark:hover:bg-emerald-500/10 dark:hover:text-emerald-400"
                              title="Activer"
                              disabled={activateMutation.isPending}
                            >
                              <UserCheck className="h-4 w-4" />
                            </button>
                          )}
                          <button
                            onClick={() => setDialog({ type: 'delete', user: u })}
                            aria-label="Supprimer"
                            className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-red-50 hover:text-red-500 dark:hover:bg-red-500/10 dark:hover:text-red-400"
                            title="Supprimer"
                          >
                            <Trash2 className="h-4 w-4" />
                          </button>
                        </>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {/* Pagination */}
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">Page {page}</p>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" disabled={!hasPrev} onClick={() => setPage(p => Math.max(1, p - 1))}>
            <ChevronLeft className="mr-1 h-4 w-4" />Précédent
          </Button>
          <Button variant="outline" size="sm" disabled={!hasNext} onClick={() => setPage(p => p + 1)}>
            Suivant<ChevronRight className="ml-1 h-4 w-4" />
          </Button>
        </div>
      </div>

      {dialog.type === 'role' && <ChangeRoleModal user={dialog.user} onClose={() => setDialog({ type: 'none' })} />}
      {dialog.type === 'delete' && (
        <DeleteModal
          user={dialog.user}
          onConfirm={() => deleteMutation.mutate(dialog.user.id)}
          onCancel={() => setDialog({ type: 'none' })}
          isPending={deleteMutation.isPending}
        />
      )}
    </div>
  )
}
