import { useState, useCallback } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import {
  Users, Plus, RefreshCw, AlertTriangle, Mail, UserCog,
  UserX, UserCheck, Trash2, Shield,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { cn } from '@/lib/utils'
import { teamApi, teamKeys, ROLES, ROLE_LABELS } from '@/api/team'
import type { TeamUserDto } from '@/api/team'
import { driversApi, driverKeys } from '@/api/drivers'
import { useAuthStore } from '@/store/authStore'

// ─── Schemas ──────────────────────────────────────────────────────────────────

const inviteSchema = z.object({
  email: z.string().email('Email invalide'),
  role: z.string().min(1, 'Rôle requis'),
})

const roleSchema = z.object({
  newRole: z.string().min(1, 'Rôle requis'),
})

type InviteValues = z.infer<typeof inviteSchema>
type RoleValues = z.infer<typeof roleSchema>

// ─── Dialog state ─────────────────────────────────────────────────────────────

type DialogState =
  | { type: 'none' }
  | { type: 'invite' }
  | { type: 'changeRole'; user: TeamUserDto }
  | { type: 'delete'; user: TeamUserDto }

// ─── Role badge ───────────────────────────────────────────────────────────────

const ROLE_COLORS: Record<string, string> = {
  Admin: 'bg-purple-100 text-purple-700 dark:bg-purple-500/15 dark:text-purple-400',
  Manager: 'bg-blue-100 text-blue-700 dark:bg-blue-500/15 dark:text-blue-400',
  Driver: 'bg-sky-100 text-sky-700 dark:bg-sky-500/15 dark:text-sky-400',
  Accountant: 'bg-amber-100 text-amber-700 dark:bg-amber-500/15 dark:text-amber-400',
  ReadOnly: 'bg-muted text-muted-foreground',
}

function RoleBadge({ role }: { role: string }) {
  return (
    <span className={cn('inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium', ROLE_COLORS[role] ?? 'bg-muted text-muted-foreground')}>
      {ROLE_LABELS[role] ?? role}
    </span>
  )
}

// ─── StatChip ─────────────────────────────────────────────────────────────────

function StatChip({ icon, label }: { icon: React.ReactNode; label: string }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white">
      {icon}{label}
    </span>
  )
}

// ─── InviteModal ──────────────────────────────────────────────────────────────

function InviteModal({ onClose }: { onClose: () => void }) {
  const qc = useQueryClient()

  const form = useForm<InviteValues>({
    resolver: zodResolver(inviteSchema),
    defaultValues: { email: '', role: '' },
  })

  const mutation = useMutation({
    mutationFn: teamApi.invite,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: teamKeys.users(false) })
      qc.invalidateQueries({ queryKey: teamKeys.users(true) })
      toast.success('Invitation envoyée')
      onClose()
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur d\'envoi') : 'Erreur'),
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-card shadow-2xl">
        <div className="bg-gradient-to-br from-sky-500 to-blue-600 px-6 py-5">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
              <Mail className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="font-bold text-white">Inviter un membre</h2>
              <p className="text-xs text-sky-200">Un email d'invitation sera envoyé</p>
            </div>
          </div>
        </div>
        <form onSubmit={form.handleSubmit(v => mutation.mutate(v))} className="space-y-4 p-6">
          <div className="space-y-1.5">
            <Label htmlFor="email">Adresse email *</Label>
            <Input id="email" type="email" {...form.register('email')} placeholder="prenom.nom@entreprise.fr" />
            {form.formState.errors.email && <p className="text-xs text-red-500">{form.formState.errors.email.message}</p>}
          </div>
          <div className="space-y-1.5">
            <Label>Rôle *</Label>
            <Select onValueChange={val => form.setValue('role', val)} defaultValue="">
              <SelectTrigger className={form.formState.errors.role ? 'border-red-400' : ''}>
                <SelectValue placeholder="Sélectionner un rôle" />
              </SelectTrigger>
              <SelectContent>
                {ROLES.filter(r => r !== 'Admin').map(r => (
                  <SelectItem key={r} value={r}>{ROLE_LABELS[r]}</SelectItem>
                ))}
              </SelectContent>
            </Select>
            {form.formState.errors.role && <p className="text-xs text-red-500">{form.formState.errors.role.message}</p>}
          </div>
          <div className="flex gap-3 pt-2">
            <Button type="button" variant="outline" className="flex-1" onClick={onClose}>Annuler</Button>
            <Button type="submit" className="flex-1" disabled={mutation.isPending}>
              {mutation.isPending
                ? <span className="flex items-center gap-2"><span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />Envoi…</span>
                : 'Envoyer l\'invitation'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ─── ChangeRoleModal ──────────────────────────────────────────────────────────

function ChangeRoleModal({ user, onClose }: { user: TeamUserDto; onClose: () => void }) {
  const qc = useQueryClient()

  const form = useForm<RoleValues>({
    resolver: zodResolver(roleSchema),
    defaultValues: { newRole: user.role },
  })

  const mutation = useMutation({
    mutationFn: (data: RoleValues) => teamApi.changeRole(user.id, data.newRole),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: teamKeys.all })
      qc.invalidateQueries({ queryKey: driverKeys.all })
      toast.success('Rôle mis à jour')
      onClose()
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur de mise à jour') : 'Erreur'),
  })

  const assignableRoles = ROLES.filter(r => r !== 'Admin')

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-card shadow-2xl">
        <div className="bg-gradient-to-br from-sky-500 to-blue-600 px-6 py-5">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
              <Shield className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="font-bold text-white">Changer le rôle</h2>
              <p className="text-xs text-sky-200">{user.firstName} {user.lastName}</p>
            </div>
          </div>
        </div>
        <form onSubmit={form.handleSubmit(v => mutation.mutate(v))} className="space-y-4 p-6">
          <div className="space-y-1.5">
            <Label>Nouveau rôle</Label>
            <Select onValueChange={val => form.setValue('newRole', val)} defaultValue={user.role}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {assignableRoles.map(r => (
                  <SelectItem key={r} value={r}>{ROLE_LABELS[r]}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex gap-3 pt-2">
            <Button type="button" variant="outline" className="flex-1" onClick={onClose}>Annuler</Button>
            <Button type="submit" className="flex-1" disabled={mutation.isPending}>
              {mutation.isPending
                ? <span className="flex items-center gap-2"><span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />Mise à jour…</span>
                : 'Confirmer'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ─── DeleteConfirmModal ────────────────────────────────────────────────────────

function DeleteConfirmModal({ user, onConfirm, onCancel, isPending }: {
  user: TeamUserDto; onConfirm: () => void; onCancel: () => void; isPending: boolean
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-card shadow-2xl">
        <div className="p-6">
          <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100 dark:bg-red-500/15">
            <AlertTriangle className="h-6 w-6 text-red-600 dark:text-red-400" />
          </div>
          <h3 className="mb-1 text-base font-semibold text-foreground">Supprimer cet utilisateur ?</h3>
          <p className="text-sm text-muted-foreground">
            Le compte de <strong>{user.firstName} {user.lastName}</strong> ({user.email}) sera définitivement supprimé.
          </p>
        </div>
        <div className="flex gap-3 border-t px-6 py-4">
          <Button variant="outline" className="flex-1" onClick={onCancel}>Annuler</Button>
          <Button variant="destructive" className="flex-1" onClick={onConfirm} disabled={isPending}>
            {isPending
              ? <span className="flex items-center gap-2"><span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />Suppression…</span>
              : 'Supprimer'}
          </Button>
        </div>
      </div>
    </div>
  )
}

// ─── TeamPage ─────────────────────────────────────────────────────────────────

export default function TeamPage() {
  const qc = useQueryClient()
  const currentUser = useAuthStore(s => s.user)
  const [showInactive, setShowInactive] = useState(false)
  const [dialog, setDialog] = useState<DialogState>({ type: 'none' })

  const { data: teamUsers = [], isLoading: loadingTeam, refetch: refetchTeam } = useQuery({
    queryKey: teamKeys.users(showInactive),
    queryFn: () => teamApi.getUsers(showInactive),
  })

  const { data: driversResult, isLoading: loadingDrivers, refetch: refetchDrivers } = useQuery({
    queryKey: [...driverKeys.lists(), { showInactive }],
    queryFn: () => driversApi.getList({ pageSize: 500, isActive: showInactive ? undefined : true }),
  })

  const driverUsers: TeamUserDto[] = (driversResult?.items ?? []).map(d => ({
    id: d.userId,
    email: d.email,
    firstName: d.firstName,
    lastName: d.lastName,
    phone: d.phone,
    role: 'Driver',
    isActive: d.isActive,
  }))

  // Driver userId set — used to exclude them from teamUsers (whose role might differ)
  const driverUserIds = new Set(driverUsers.map(d => d.id))
  const users = [
    ...teamUsers.filter(u => !driverUserIds.has(u.id)),
    ...driverUsers,
  ]

  const isLoading = loadingTeam || loadingDrivers

  function refetch() {
    refetchTeam()
    refetchDrivers()
  }

  function invalidateAll() {
    qc.invalidateQueries({ queryKey: teamKeys.all })
    qc.invalidateQueries({ queryKey: driverKeys.all })
  }

  const activateMutation = useMutation({
    mutationFn: (userId: string) => teamApi.activate(userId),
    onSuccess: () => { invalidateAll(); toast.success('Utilisateur réactivé') },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur') : 'Erreur'),
  })

  const deactivateMutation = useMutation({
    mutationFn: (userId: string) => teamApi.deactivate(userId),
    onSuccess: () => { invalidateAll(); toast.success('Utilisateur désactivé') },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur') : 'Erreur'),
  })

  const deleteMutation = useMutation({
    mutationFn: (userId: string) => teamApi.delete(userId),
    onSuccess: () => {
      invalidateAll()
      toast.success('Utilisateur supprimé')
      setDialog({ type: 'none' })
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Suppression impossible') : 'Erreur'),
  })

  const closeDialog = useCallback(() => setDialog({ type: 'none' }), [])

  const activeCount = users.filter(u => u.isActive).length

  const grouped = ROLES
    .map(role => ({ role, members: users.filter(u => u.role === role) }))
    .filter(g => g.members.length > 0)

  function getInitials(u: TeamUserDto) {
    return `${u.firstName.charAt(0)}${u.lastName.charAt(0)}`.toUpperCase()
  }

  function renderUserRow(user: TeamUserDto) {
    const isSelf = user.id === currentUser?.id
    return (
      <TableRow key={user.id} className={cn('hover:bg-sky-50/40 dark:hover:bg-sky-500/5', !user.isActive && 'opacity-50')}>
        <TableCell className="py-3 pl-6">
          <div className="flex items-center gap-3">
            <div className={cn(
              'flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-sm font-bold text-white',
              user.isActive
                ? 'bg-gradient-to-br from-sky-500 to-blue-600'
                : 'bg-gradient-to-br from-slate-400 to-slate-500',
            )}>
              {getInitials(user)}
            </div>
            <div>
              <div className="flex items-center gap-1.5">
                <p className="font-semibold text-foreground">{user.firstName} {user.lastName}</p>
                {isSelf && (
                  <span className="rounded-full bg-sky-100 px-1.5 py-0.5 text-xs font-medium text-sky-600 dark:bg-sky-500/15 dark:text-sky-400">vous</span>
                )}
              </div>
              <p className="flex items-center gap-1 text-xs text-muted-foreground">
                <Mail className="h-3 w-3" />{user.email}
              </p>
            </div>
          </div>
        </TableCell>
        <TableCell>
          <span className={cn(
            'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium',
            user.isActive ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-400' : 'bg-muted text-muted-foreground',
          )}>
            {user.isActive ? 'Actif' : 'Inactif'}
          </span>
        </TableCell>
        <TableCell className="pr-6" onClick={e => e.stopPropagation()}>
          {!isSelf && (
            <div className="flex items-center justify-end gap-1">
              <button
                onClick={() => setDialog({ type: 'changeRole', user })}
                aria-label="Changer le rôle"
                className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                title="Changer le rôle"
              >
                <UserCog className="h-4 w-4" />
              </button>
              {user.isActive ? (
                <button
                  onClick={() => deactivateMutation.mutate(user.id)}
                  aria-label="Désactiver"
                  className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-amber-50 hover:text-amber-600 dark:hover:bg-amber-500/10 dark:hover:text-amber-400"
                  title="Désactiver"
                  disabled={deactivateMutation.isPending}
                >
                  <UserX className="h-4 w-4" />
                </button>
              ) : (
                <button
                  onClick={() => activateMutation.mutate(user.id)}
                  aria-label="Réactiver"
                  className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-emerald-50 hover:text-emerald-600 dark:hover:bg-emerald-500/10 dark:hover:text-emerald-400"
                  title="Réactiver"
                  disabled={activateMutation.isPending}
                >
                  <UserCheck className="h-4 w-4" />
                </button>
              )}
              <button
                onClick={() => setDialog({ type: 'delete', user })}
                aria-label="Supprimer"
                className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-red-50 hover:text-red-500 dark:hover:bg-red-500/10 dark:hover:text-red-400"
                title="Supprimer"
              >
                <Trash2 className="h-4 w-4" />
              </button>
            </div>
          )}
        </TableCell>
      </TableRow>
    )
  }

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Hero */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 p-6 shadow-lg">
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }} />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="mb-1 flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
                <Users className="h-5 w-5 text-white" />
              </div>
              <h1 className="text-2xl font-bold tracking-tight text-white">Équipe</h1>
            </div>
            <p className="text-sm text-sky-200">Gérez les membres et leurs rôles</p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            {!isLoading && (
              <>
                <StatChip icon={<Users className="h-3.5 w-3.5" />} label={`${users.length} membre${users.length > 1 ? 's' : ''}`} />
                <StatChip icon={<UserCheck className="h-3.5 w-3.5" />} label={`${activeCount} actif${activeCount > 1 ? 's' : ''}`} />
              </>
            )}
            <button
              onClick={() => setDialog({ type: 'invite' })}
              className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white backdrop-blur-sm transition-colors hover:bg-white/25"
            >
              <Plus className="h-3.5 w-3.5" />Inviter un membre
            </button>
          </div>
        </div>
      </div>

      {/* Toolbar */}
      <div className="flex items-center justify-between gap-3">
        <label className="flex cursor-pointer items-center gap-2 text-sm text-muted-foreground">
          <input
            type="checkbox"
            checked={showInactive}
            onChange={e => setShowInactive(e.target.checked)}
            className="h-4 w-4 rounded border-input text-sky-600 focus:ring-sky-500"
          />
          Afficher les membres inactifs
        </label>
        <Button variant="outline" size="icon" onClick={() => refetch()} title="Actualiser"><RefreshCw className="h-4 w-4" /></Button>
      </div>

      {/* Grouped by role */}
      {isLoading ? (
        <div className="overflow-hidden rounded-2xl border bg-card shadow-sm">
          <Table>
            <TableBody>
              {[...Array(5)].map((_, i) => (
                <TableRow key={i}>
                  <TableCell className="pl-6"><Skeleton className="h-9 w-52" /></TableCell>
                  <TableCell><Skeleton className="h-6 w-16 rounded-full" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      ) : users.length === 0 ? (
        <div className="flex flex-col items-center gap-3 rounded-2xl border border-dashed border-border py-16">
          <div className="flex h-12 w-12 items-center justify-center rounded-full bg-muted text-muted-foreground">
            <Users className="h-6 w-6" />
          </div>
          <p className="text-sm font-medium text-muted-foreground">Aucun membre dans l'équipe</p>
          <Button size="sm" onClick={() => setDialog({ type: 'invite' })}>
            <Plus className="mr-1.5 h-3.5 w-3.5" />Inviter le premier membre
          </Button>
        </div>
      ) : (
        <div className="flex flex-col gap-4">
          {grouped.map(({ role, members }) => (
            <div key={role} className="overflow-hidden rounded-2xl border bg-card shadow-sm">
              {/* Group header */}
              <div className="flex items-center gap-3 border-b bg-muted px-5 py-3">
                <RoleBadge role={role} />
                <span className="text-xs text-muted-foreground font-medium">
                  {members.length} membre{members.length > 1 ? 's' : ''}
                </span>
              </div>
              <Table>
                <TableHeader>
                  <TableRow className="hover:bg-transparent">
                    <TableHead className="pl-6 text-xs font-semibold uppercase tracking-wider text-muted-foreground">Membre</TableHead>
                    <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Statut</TableHead>
                    <TableHead className="w-28 pr-6" />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {members.map(user => renderUserRow(user))}
                </TableBody>
              </Table>
            </div>
          ))}
        </div>
      )}

      {/* Modals */}
      {dialog.type === 'invite' && <InviteModal onClose={closeDialog} />}
      {dialog.type === 'changeRole' && <ChangeRoleModal user={dialog.user} onClose={closeDialog} />}
      {dialog.type === 'delete' && (
        <DeleteConfirmModal
          user={dialog.user}
          onConfirm={() => deleteMutation.mutate(dialog.user.id)}
          onCancel={closeDialog}
          isPending={deleteMutation.isPending}
        />
      )}
    </div>
  )
}
