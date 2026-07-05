import { useState, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import {
  Building2, Search, RefreshCw, Users, Power,
  Trash2, Eye, AlertTriangle, CheckCircle2, XCircle, Ban,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { cn } from '@/lib/utils'
import { adminApi, adminKeys, PLAN_LABELS, PLAN_COLORS } from '@/api/admin'
import type { AdminTenantDto } from '@/api/admin'

type StatusFilter = 'all' | 'active' | 'inactive'

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

// ─── Delete confirmation ────────────────────────────────────────────────────

function DeleteModal({ tenant, onConfirm, onCancel, isPending }: {
  tenant: AdminTenantDto; onConfirm: () => void; onCancel: () => void; isPending: boolean
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-white shadow-2xl">
        <div className="p-6">
          <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100">
            <AlertTriangle className="h-6 w-6 text-red-600" />
          </div>
          <h3 className="mb-1 text-base font-semibold text-slate-800">Supprimer cette entreprise ?</h3>
          <p className="text-sm text-slate-500">
            <strong>{tenant.name}</strong> sera désactivée et archivée. Cette action est réversible côté serveur mais retire l'accès immédiatement.
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

// ─── Page ───────────────────────────────────────────────────────────────────

export default function TenantsPage() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<StatusFilter>('all')
  const [toDelete, setToDelete] = useState<AdminTenantDto | null>(null)

  const { data: tenants = [], isLoading, refetch } = useQuery({
    queryKey: adminKeys.tenants.list(),
    queryFn: adminApi.getTenants,
  })

  function invalidate() {
    qc.invalidateQueries({ queryKey: adminKeys.tenants.all })
    qc.invalidateQueries({ queryKey: adminKeys.stats() })
  }

  const activateMutation = useMutation({
    mutationFn: (id: number) => adminApi.activateTenant(id),
    onSuccess: () => { invalidate(); toast.success('Entreprise activée') },
    onError: (e) => toast.error(errMsg(e, 'Activation impossible')),
  })
  const deactivateMutation = useMutation({
    mutationFn: (id: number) => adminApi.deactivateTenant(id),
    onSuccess: () => { invalidate(); toast.success('Entreprise désactivée') },
    onError: (e) => toast.error(errMsg(e, 'Désactivation impossible')),
  })
  const deleteMutation = useMutation({
    mutationFn: (id: number) => adminApi.deleteTenant(id),
    onSuccess: () => { invalidate(); toast.success('Entreprise supprimée'); setToDelete(null) },
    onError: (e) => toast.error(errMsg(e, 'Suppression impossible')),
  })

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    return tenants.filter(t => {
      if (status === 'active' && !t.isActive) return false
      if (status === 'inactive' && t.isActive) return false
      if (q && !(t.name.toLowerCase().includes(q) || (t.subDomain ?? '').toLowerCase().includes(q))) return false
      return true
    })
  }, [tenants, search, status])

  const activeCount = tenants.filter(t => t.isActive).length
  const inactiveCount = tenants.length - activeCount

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Hero */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-violet-600 to-indigo-700 p-6 shadow-lg">
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }} />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="mb-1 flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
                <Building2 className="h-5 w-5 text-white" />
              </div>
              <h1 className="text-2xl font-bold tracking-tight text-white">Entreprises</h1>
            </div>
            <p className="text-sm text-violet-200">Gérez les entreprises clientes de la plateforme</p>
          </div>
          {!isLoading && (
            <div className="flex flex-wrap items-center gap-3">
              <StatChip icon={<Building2 className="h-3.5 w-3.5" />} label={`${tenants.length} total`} />
              <StatChip icon={<CheckCircle2 className="h-3.5 w-3.5" />} label={`${activeCount} active${activeCount > 1 ? 's' : ''}`} />
              <StatChip icon={<Ban className="h-3.5 w-3.5" />} label={`${inactiveCount} inactive${inactiveCount > 1 ? 's' : ''}`} />
            </div>
          )}
        </div>
      </div>

      {/* Toolbar */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="relative w-full sm:max-w-xs">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Rechercher une entreprise…" className="pl-9" />
        </div>
        <div className="flex items-center gap-2">
          <div className="flex gap-1 rounded-xl bg-slate-100 p-1">
            {(['all', 'active', 'inactive'] as StatusFilter[]).map(s => (
              <button
                key={s}
                onClick={() => setStatus(s)}
                className={cn(
                  'rounded-lg px-3 py-1.5 text-sm font-medium transition-all',
                  status === s ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500 hover:text-slate-700',
                )}
              >
                {s === 'all' ? 'Toutes' : s === 'active' ? 'Actives' : 'Inactives'}
              </button>
            ))}
          </div>
          <Button variant="outline" size="icon" onClick={() => refetch()} title="Actualiser"><RefreshCw className="h-4 w-4" /></Button>
        </div>
      </div>

      {/* Table */}
      <div className="overflow-hidden rounded-2xl border bg-white shadow-sm">
        <Table>
          <TableHeader>
            <TableRow className="hover:bg-transparent">
              <TableHead className="pl-6 text-xs font-semibold uppercase tracking-wider text-slate-400">Entreprise</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Plan</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Utilisateurs</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Créée le</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-slate-400">Statut</TableHead>
              <TableHead className="w-32 pr-6" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              [...Array(6)].map((_, i) => (
                <TableRow key={i}>
                  <TableCell className="pl-6"><Skeleton className="h-9 w-48" /></TableCell>
                  <TableCell><Skeleton className="h-6 w-16 rounded-full" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-12" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                  <TableCell><Skeleton className="h-6 w-16 rounded-full" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                </TableRow>
              ))
            ) : filtered.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6}>
                  <div className="flex flex-col items-center gap-3 py-16">
                    <div className="flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
                      <Building2 className="h-6 w-6" />
                    </div>
                    <p className="text-sm font-medium text-slate-500">Aucune entreprise trouvée</p>
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              filtered.map(t => (
                <TableRow
                  key={t.id}
                  className={cn('cursor-pointer hover:bg-violet-50/40', !t.isActive && 'opacity-60')}
                  onClick={() => navigate(`/admin/tenants/${t.id}`)}
                >
                  <TableCell className="py-3 pl-6">
                    <div className="flex items-center gap-3">
                      <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br from-violet-500 to-indigo-600 text-sm font-bold text-white">
                        {t.name.charAt(0).toUpperCase()}
                      </div>
                      <div>
                        <p className="font-semibold text-slate-800">{t.name}</p>
                        {t.subDomain && <p className="text-xs text-slate-400">{t.subDomain}</p>}
                      </div>
                    </div>
                  </TableCell>
                  <TableCell>
                    <span className={cn('inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium', PLAN_COLORS[t.planType] ?? 'bg-slate-100 text-slate-600')}>
                      {PLAN_LABELS[t.planType] ?? t.planType}
                    </span>
                  </TableCell>
                  <TableCell>
                    <span className="inline-flex items-center gap-1 text-sm text-slate-600">
                      <Users className="h-3.5 w-3.5 text-slate-400" />
                      {t.activeUserCount}/{t.userCount}
                    </span>
                  </TableCell>
                  <TableCell className="text-sm text-slate-500">{formatDate(t.createdDate)}</TableCell>
                  <TableCell>
                    <span className={cn(
                      'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium',
                      t.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500',
                    )}>
                      {t.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </TableCell>
                  <TableCell className="pr-6" onClick={e => e.stopPropagation()}>
                    <div className="flex items-center justify-end gap-1">
                      <button
                        onClick={() => navigate(`/admin/tenants/${t.id}`)}
                        className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
                        title="Détails"
                      >
                        <Eye className="h-4 w-4" />
                      </button>
                      {t.isActive ? (
                        <button
                          onClick={() => deactivateMutation.mutate(t.id)}
                          className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-amber-50 hover:text-amber-600"
                          title="Désactiver"
                          disabled={deactivateMutation.isPending}
                        >
                          <XCircle className="h-4 w-4" />
                        </button>
                      ) : (
                        <button
                          onClick={() => activateMutation.mutate(t.id)}
                          className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-emerald-50 hover:text-emerald-600"
                          title="Activer"
                          disabled={activateMutation.isPending}
                        >
                          <Power className="h-4 w-4" />
                        </button>
                      )}
                      <button
                        onClick={() => setToDelete(t)}
                        className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-red-50 hover:text-red-500"
                        title="Supprimer"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {toDelete && (
        <DeleteModal
          tenant={toDelete}
          onConfirm={() => deleteMutation.mutate(toDelete.id)}
          onCancel={() => setToDelete(null)}
          isPending={deleteMutation.isPending}
        />
      )}
    </div>
  )
}
