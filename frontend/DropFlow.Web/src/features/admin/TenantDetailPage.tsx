import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import {
  ArrowLeft, Building2, Users, Package, Calendar, Clock,
  CreditCard, Power, XCircle, Trash2, AlertTriangle, Pencil, Mail,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import {
  adminApi, adminKeys, PLAN_TYPES, PLAN_LABELS, PLAN_COLORS, PLAN_DEFAULTS,
} from '@/api/admin'
import type { TenantDetailsDto } from '@/api/admin'
import { ROLE_LABELS } from '@/api/team'

function errMsg(err: unknown, fallback: string) {
  if (isAxiosError(err)) return err.response?.data?.message ?? err.response?.data?.errors?.[0] ?? fallback
  return fallback
}

function formatDate(iso?: string) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function formatDateTime(iso?: string) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

// ─── Plan dialog ────────────────────────────────────────────────────────────

const planSchema = z.object({
  planType: z.string().min(1, 'Plan requis'),
  maxUsers: z.coerce.number().int().min(1, 'Minimum 1'),
  maxDeliveries: z.coerce.number().int().min(1, 'Minimum 1'),
  expiryDate: z.string().optional(),
})
type PlanValues = z.infer<typeof planSchema>

function UpdatePlanModal({ tenant, onClose }: { tenant: TenantDetailsDto; onClose: () => void }) {
  const qc = useQueryClient()
  const form = useForm<PlanValues>({
    resolver: zodResolver(planSchema),
    defaultValues: {
      planType: tenant.planType,
      maxUsers: tenant.maxUsers,
      maxDeliveries: tenant.maxDeliveries,
      expiryDate: tenant.expiryDate ? tenant.expiryDate.substring(0, 10) : '',
    },
  })

  const selectedPlan = form.watch('planType')

  // When plan changes, pre-fill limits from defaults (still editable).
  useEffect(() => {
    if (selectedPlan && selectedPlan !== tenant.planType && PLAN_DEFAULTS[selectedPlan]) {
      form.setValue('maxUsers', PLAN_DEFAULTS[selectedPlan].maxUsers)
      form.setValue('maxDeliveries', PLAN_DEFAULTS[selectedPlan].maxDeliveries)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedPlan])

  const mutation = useMutation({
    mutationFn: (v: PlanValues) => adminApi.updateTenantPlan(tenant.id, {
      planType: v.planType,
      maxUsers: v.maxUsers,
      maxDeliveries: v.maxDeliveries,
      expiryDate: v.expiryDate ? new Date(v.expiryDate).toISOString() : null,
    }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: adminKeys.tenants.all })
      qc.invalidateQueries({ queryKey: adminKeys.stats() })
      toast.success('Plan mis à jour')
      onClose()
    },
    onError: (e) => toast.error(errMsg(e, 'Mise à jour impossible')),
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
      <div className="relative z-10 w-full max-w-md overflow-hidden rounded-2xl bg-white shadow-2xl">
        <div className="bg-gradient-to-br from-violet-600 to-indigo-700 px-6 py-5">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
              <CreditCard className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="font-bold text-white">Modifier le plan</h2>
              <p className="text-xs text-violet-200">{tenant.name}</p>
            </div>
          </div>
        </div>
        <form onSubmit={form.handleSubmit(v => mutation.mutate(v))} className="space-y-4 p-6">
          <div className="space-y-1.5">
            <Label>Plan</Label>
            <Select defaultValue={tenant.planType} onValueChange={val => form.setValue('planType', val)}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                {PLAN_TYPES.map(p => <SelectItem key={p} value={p}>{PLAN_LABELS[p]}</SelectItem>)}
              </SelectContent>
            </Select>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="maxUsers">Max utilisateurs</Label>
              <Input id="maxUsers" type="number" {...form.register('maxUsers')} />
              {form.formState.errors.maxUsers && <p className="text-xs text-red-500">{form.formState.errors.maxUsers.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="maxDeliveries">Max livraisons</Label>
              <Input id="maxDeliveries" type="number" {...form.register('maxDeliveries')} />
              {form.formState.errors.maxDeliveries && <p className="text-xs text-red-500">{form.formState.errors.maxDeliveries.message}</p>}
            </div>
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="expiryDate">Date d'expiration (optionnel)</Label>
            <Input id="expiryDate" type="date" {...form.register('expiryDate')} />
          </div>
          <div className="flex gap-3 pt-2">
            <Button type="button" variant="outline" className="flex-1" onClick={onClose}>Annuler</Button>
            <Button type="submit" className="flex-1" disabled={mutation.isPending}>
              {mutation.isPending
                ? <span className="flex items-center gap-2"><span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />Mise à jour…</span>
                : 'Enregistrer'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ─── Delete modal ───────────────────────────────────────────────────────────

function DeleteModal({ name, onConfirm, onCancel, isPending }: {
  name: string; onConfirm: () => void; onCancel: () => void; isPending: boolean
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
          <p className="text-sm text-slate-500"><strong>{name}</strong> sera désactivée et archivée.</p>
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

// ─── Info tile ──────────────────────────────────────────────────────────────

function InfoTile({ icon, label, value }: { icon: React.ReactNode; label: string; value: string }) {
  return (
    <div className="flex items-center gap-3 rounded-xl border bg-white p-4">
      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-slate-100 text-slate-500">
        {icon}
      </div>
      <div className="min-w-0">
        <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{label}</p>
        <p className="truncate text-sm font-semibold text-slate-800">{value}</p>
      </div>
    </div>
  )
}

// ─── Page ───────────────────────────────────────────────────────────────────

export default function TenantDetailPage() {
  const { id } = useParams<{ id: string }>()
  const tenantId = Number(id)
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [showPlan, setShowPlan] = useState(false)
  const [showDelete, setShowDelete] = useState(false)

  const { data: tenant, isLoading, isError } = useQuery({
    queryKey: adminKeys.tenants.detail(tenantId),
    queryFn: () => adminApi.getTenant(tenantId),
    enabled: Number.isFinite(tenantId),
  })

  function invalidate() {
    qc.invalidateQueries({ queryKey: adminKeys.tenants.all })
    qc.invalidateQueries({ queryKey: adminKeys.stats() })
  }

  const activateMutation = useMutation({
    mutationFn: () => adminApi.activateTenant(tenantId),
    onSuccess: () => { invalidate(); toast.success('Entreprise activée') },
    onError: (e) => toast.error(errMsg(e, 'Activation impossible')),
  })
  const deactivateMutation = useMutation({
    mutationFn: () => adminApi.deactivateTenant(tenantId),
    onSuccess: () => { invalidate(); toast.success('Entreprise désactivée') },
    onError: (e) => toast.error(errMsg(e, 'Désactivation impossible')),
  })
  const deleteMutation = useMutation({
    mutationFn: () => adminApi.deleteTenant(tenantId),
    onSuccess: () => { invalidate(); toast.success('Entreprise supprimée'); navigate('/admin/tenants') },
    onError: (e) => toast.error(errMsg(e, 'Suppression impossible')),
  })

  if (isLoading) {
    return (
      <div className="flex flex-col gap-6 p-6">
        <Skeleton className="h-32 rounded-2xl" />
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-20 rounded-xl" />)}
        </div>
        <Skeleton className="h-64 rounded-2xl" />
      </div>
    )
  }

  if (isError || !tenant) {
    return (
      <div className="flex flex-col items-center gap-4 p-16 text-center">
        <div className="flex h-12 w-12 items-center justify-center rounded-full bg-red-100 text-red-500">
          <AlertTriangle className="h-6 w-6" />
        </div>
        <p className="text-sm text-slate-500">Entreprise introuvable</p>
        <Button variant="outline" onClick={() => navigate('/admin/tenants')}>
          <ArrowLeft className="mr-1.5 h-4 w-4" />Retour à la liste
        </Button>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Back */}
      <button
        onClick={() => navigate('/admin/tenants')}
        className="flex w-fit items-center gap-1.5 text-sm font-medium text-slate-500 transition-colors hover:text-slate-700"
      >
        <ArrowLeft className="h-4 w-4" />Entreprises
      </button>

      {/* Hero */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-violet-600 to-indigo-700 p-6 shadow-lg">
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }} />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-4">
            <div className="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-white/20 text-2xl font-bold text-white">
              {tenant.name.charAt(0).toUpperCase()}
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h1 className="text-2xl font-bold tracking-tight text-white">{tenant.name}</h1>
                <span className={cn(
                  'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
                  tenant.isActive ? 'bg-emerald-400/25 text-emerald-100' : 'bg-white/20 text-white',
                )}>
                  {tenant.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>
              <div className="mt-1 flex items-center gap-2">
                <span className={cn('inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium', PLAN_COLORS[tenant.planType] ?? 'bg-white/20 text-white')}>
                  {PLAN_LABELS[tenant.planType] ?? tenant.planType}
                </span>
                {tenant.subDomain && <span className="text-xs text-violet-200">{tenant.subDomain}</span>}
              </div>
            </div>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <button onClick={() => setShowPlan(true)} className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-white/25">
              <Pencil className="h-3.5 w-3.5" />Modifier le plan
            </button>
            {tenant.isActive ? (
              <button onClick={() => deactivateMutation.mutate()} disabled={deactivateMutation.isPending} className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-white/25">
                <XCircle className="h-3.5 w-3.5" />Désactiver
              </button>
            ) : (
              <button onClick={() => activateMutation.mutate()} disabled={activateMutation.isPending} className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-white/25">
                <Power className="h-3.5 w-3.5" />Activer
              </button>
            )}
            <button onClick={() => setShowDelete(true)} className="flex items-center gap-1.5 rounded-xl bg-red-400/30 px-3 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-red-400/50">
              <Trash2 className="h-3.5 w-3.5" />Supprimer
            </button>
          </div>
        </div>
      </div>

      {/* Info tiles */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <InfoTile icon={<Users className="h-5 w-5" />} label="Utilisateurs" value={`${tenant.activeUserCount}/${tenant.userCount} (max ${tenant.maxUsers})`} />
        <InfoTile icon={<Package className="h-5 w-5" />} label="Livraisons max" value={String(tenant.maxDeliveries)} />
        <InfoTile icon={<Calendar className="h-5 w-5" />} label="Créée le" value={formatDate(tenant.createdDate)} />
        <InfoTile icon={<Clock className="h-5 w-5" />} label="Dernière activité" value={formatDateTime(tenant.lastActivityDate)} />
      </div>

      {tenant.expiryDate && (
        <div className="flex items-center gap-2 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-700">
          <Calendar className="h-4 w-4" />
          Plan expire le <strong>{formatDate(tenant.expiryDate)}</strong>
        </div>
      )}

      {/* Recent users */}
      <div className="overflow-hidden rounded-2xl border bg-white shadow-sm">
        <div className="flex items-center gap-2 border-b px-6 py-4">
          <Building2 className="h-4 w-4 text-slate-500" />
          <h2 className="font-semibold text-slate-800">Utilisateurs récents</h2>
          {tenant.recentUsers.length > 0 && (
            <span className="rounded-full bg-violet-100 px-2 py-0.5 text-xs font-semibold text-violet-600">
              {tenant.recentUsers.length}
            </span>
          )}
        </div>
        {tenant.recentUsers.length === 0 ? (
          <div className="flex flex-col items-center gap-2 py-12 text-center">
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-slate-100 text-slate-400">
              <Users className="h-5 w-5" />
            </div>
            <p className="text-sm text-slate-500">Aucun utilisateur</p>
          </div>
        ) : (
          <div className="divide-y">
            {tenant.recentUsers.map(u => (
              <div key={u.id} className="flex items-center gap-3 px-6 py-3 hover:bg-slate-50/60">
                <div className={cn(
                  'flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-sm font-bold text-white',
                  u.isActive ? 'bg-gradient-to-br from-violet-500 to-indigo-600' : 'bg-gradient-to-br from-slate-400 to-slate-500',
                )}>
                  {(u.firstName.charAt(0) + u.lastName.charAt(0)).toUpperCase()}
                </div>
                <div className="min-w-0 flex-1">
                  <p className="font-semibold text-slate-800">{u.fullName}</p>
                  <p className="flex items-center gap-1 text-xs text-slate-400"><Mail className="h-3 w-3" />{u.email}</p>
                </div>
                <span className="inline-flex items-center rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-600">
                  {ROLE_LABELS[u.role] ?? u.role}
                </span>
                <span className={cn(
                  'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
                  u.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500',
                )}>
                  {u.isActive ? 'Actif' : 'Inactif'}
                </span>
              </div>
            ))}
          </div>
        )}
      </div>

      {showPlan && <UpdatePlanModal tenant={tenant} onClose={() => setShowPlan(false)} />}
      {showDelete && (
        <DeleteModal
          name={tenant.name}
          onConfirm={() => deleteMutation.mutate()}
          onCancel={() => setShowDelete(false)}
          isPending={deleteMutation.isPending}
        />
      )}
    </div>
  )
}
