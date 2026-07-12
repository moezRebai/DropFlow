import { useState, useEffect, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  ScrollText, Search, RefreshCw, ChevronLeft, ChevronRight,
  Eye, Info, AlertTriangle, ShieldAlert, X, Building2, User, Clock,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { cn } from '@/lib/utils'
import {
  adminApi, adminKeys, SEVERITY_LABELS, SEVERITY_COLORS,
} from '@/api/admin'
import type { AuditLogDto, AuditLogFilter, AuditSeverity } from '@/api/admin'

const PAGE_SIZE = 50

const SEVERITY_ICON: Record<string, React.ElementType> = {
  Info,
  Warning: AlertTriangle,
  Critical: ShieldAlert,
}

function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString('fr-FR', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', second: '2-digit',
  })
}

function prettyJson(raw?: string): string | null {
  if (!raw) return null
  try {
    return JSON.stringify(JSON.parse(raw), null, 2)
  } catch {
    return raw
  }
}

function SeverityBadge({ severity }: { severity: string }) {
  const Icon = SEVERITY_ICON[severity] ?? Info
  return (
    <span className={cn('inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium', SEVERITY_COLORS[severity] ?? 'bg-muted text-muted-foreground')}>
      <Icon className="h-3 w-3" />
      {SEVERITY_LABELS[severity] ?? severity}
    </span>
  )
}

// ─── Details modal ──────────────────────────────────────────────────────────

function DetailsModal({ log, onClose }: { log: AuditLogDto; onClose: () => void }) {
  const changes = prettyJson(log.changes)
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
      <div className="relative z-10 flex max-h-[85vh] w-full max-w-lg flex-col overflow-hidden rounded-2xl bg-card shadow-2xl">
        <div className="flex items-center justify-between bg-gradient-to-br from-violet-600 to-indigo-700 px-6 py-5">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
              <ScrollText className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="font-bold text-white">{log.action}</h2>
              <p className="text-xs text-violet-200">#{log.id} · {formatDateTime(log.timestamp)}</p>
            </div>
          </div>
          <button onClick={onClose} aria-label="Fermer" className="rounded-lg p-1.5 text-white/80 transition-colors hover:bg-white/15 hover:text-white">
            <X className="h-5 w-5" />
          </button>
        </div>
        <div className="flex-1 space-y-4 overflow-y-auto p-6">
          <div className="flex flex-wrap gap-2">
            <SeverityBadge severity={log.severity} />
            <span className="inline-flex items-center rounded-full bg-muted px-2.5 py-0.5 text-xs font-medium text-muted-foreground">
              {log.entityName}{log.entityId != null ? ` #${log.entityId}` : ''}
            </span>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="rounded-xl border bg-muted p-3">
              <p className="flex items-center gap-1 text-xs font-medium uppercase tracking-wide text-muted-foreground"><User className="h-3 w-3" />Utilisateur</p>
              <p className="mt-1 text-sm font-semibold text-foreground">{log.userEmail ?? 'Système'}</p>
            </div>
            <div className="rounded-xl border bg-muted p-3">
              <p className="flex items-center gap-1 text-xs font-medium uppercase tracking-wide text-muted-foreground"><Building2 className="h-3 w-3" />Entreprise</p>
              <p className="mt-1 text-sm font-semibold text-foreground">{log.tenantName ?? (log.tenantId === 0 ? 'Plateforme' : `#${log.tenantId}`)}</p>
            </div>
          </div>
          <div>
            <p className="mb-1.5 text-xs font-medium uppercase tracking-wide text-muted-foreground">Détails des changements</p>
            {changes ? (
              <pre className="max-h-64 overflow-auto rounded-xl border bg-slate-900 p-4 text-xs leading-relaxed text-slate-100">{changes}</pre>
            ) : (
              <p className="rounded-xl border border-dashed border-border p-4 text-center text-sm text-muted-foreground">Aucun détail enregistré</p>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}

// ─── Page ───────────────────────────────────────────────────────────────────

export default function AuditLogsPage() {
  const [actionInput, setActionInput] = useState('')
  const [action, setAction] = useState('')
  const [severity, setSeverity] = useState<string>('all')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [page, setPage] = useState(1)
  const [selected, setSelected] = useState<AuditLogDto | null>(null)

  useEffect(() => {
    const t = setTimeout(() => { setAction(actionInput); setPage(1) }, 350)
    return () => clearTimeout(t)
  }, [actionInput])

  const filter: AuditLogFilter = useMemo(() => ({
    action: action || undefined,
    severity: severity === 'all' ? undefined : (severity as AuditSeverity),
    startDate: startDate ? new Date(startDate).toISOString() : undefined,
    endDate: endDate ? new Date(endDate + 'T23:59:59').toISOString() : undefined,
    pageNumber: page,
    pageSize: PAGE_SIZE,
  }), [action, severity, startDate, endDate, page])

  const { data: logs = [], isLoading, isFetching, refetch } = useQuery({
    queryKey: adminKeys.audit(filter),
    queryFn: () => adminApi.getAuditLogs(filter),
  })

  const hasNext = logs.length === PAGE_SIZE
  const hasPrev = page > 1

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Hero */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-violet-600 to-indigo-700 p-6 shadow-lg">
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }} />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="mb-1 flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
                <ScrollText className="h-5 w-5 text-white" />
              </div>
              <h1 className="text-2xl font-bold tracking-tight text-white">Logs d'audit</h1>
            </div>
            <p className="text-sm text-violet-200">Traçabilité des actions sur la plateforme</p>
          </div>
          <span className="inline-flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white">
            Page {page}{hasNext ? '' : ' (fin)'}
          </span>
        </div>
      </div>

      {/* Filters */}
      <div className="grid gap-3 rounded-2xl border bg-card p-4 shadow-sm sm:grid-cols-2 lg:grid-cols-5">
        <div className="space-y-1.5">
          <Label className="text-xs text-muted-foreground">Action</Label>
          <div className="relative">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input value={actionInput} onChange={e => setActionInput(e.target.value)} placeholder="ex. TenantPlanUpdated" className="pl-9" />
          </div>
        </div>
        <div className="space-y-1.5">
          <Label className="text-xs text-muted-foreground">Sévérité</Label>
          <Select value={severity} onValueChange={v => { setSeverity(v); setPage(1) }}>
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Toutes</SelectItem>
              <SelectItem value="Info">Info</SelectItem>
              <SelectItem value="Warning">Avertissement</SelectItem>
              <SelectItem value="Critical">Critique</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-1.5">
          <Label className="text-xs text-muted-foreground">Du</Label>
          <Input type="date" value={startDate} onChange={e => { setStartDate(e.target.value); setPage(1) }} />
        </div>
        <div className="space-y-1.5">
          <Label className="text-xs text-muted-foreground">Au</Label>
          <Input type="date" value={endDate} onChange={e => { setEndDate(e.target.value); setPage(1) }} />
        </div>
        <div className="flex items-end gap-2">
          <Button
            variant="outline"
            className="flex-1"
            onClick={() => { setActionInput(''); setAction(''); setSeverity('all'); setStartDate(''); setEndDate(''); setPage(1) }}
          >
            Réinitialiser
          </Button>
          <Button variant="outline" size="icon" onClick={() => refetch()} title="Actualiser"><RefreshCw className={cn('h-4 w-4', isFetching && 'animate-spin')} /></Button>
        </div>
      </div>

      {/* Table */}
      <div className="overflow-hidden rounded-2xl border bg-card shadow-sm">
        <Table>
          <TableHeader>
            <TableRow className="hover:bg-transparent">
              <TableHead className="pl-6 text-xs font-semibold uppercase tracking-wider text-muted-foreground">Sévérité</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Action</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Entité</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Utilisateur</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Date</TableHead>
              <TableHead className="w-16 pr-6" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              [...Array(10)].map((_, i) => (
                <TableRow key={i}>
                  <TableCell className="pl-6"><Skeleton className="h-6 w-20 rounded-full" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-8" /></TableCell>
                </TableRow>
              ))
            ) : logs.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6}>
                  <div className="flex flex-col items-center gap-3 py-16">
                    <div className="flex h-12 w-12 items-center justify-center rounded-full bg-muted text-muted-foreground">
                      <ScrollText className="h-6 w-6" />
                    </div>
                    <p className="text-sm font-medium text-muted-foreground">Aucun log trouvé</p>
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              logs.map(log => (
                <TableRow key={log.id} className="cursor-pointer hover:bg-violet-50/40 dark:hover:bg-violet-500/5" onClick={() => setSelected(log)}>
                  <TableCell className="py-3 pl-6"><SeverityBadge severity={log.severity} /></TableCell>
                  <TableCell className="font-medium text-foreground">{log.action}</TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {log.entityName}{log.entityId != null ? ` #${log.entityId}` : ''}
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">{log.userEmail ?? 'Système'}</TableCell>
                  <TableCell>
                    <span className="flex items-center gap-1 text-sm text-muted-foreground">
                      <Clock className="h-3.5 w-3.5 text-muted-foreground" />{formatDateTime(log.timestamp)}
                    </span>
                  </TableCell>
                  <TableCell className="pr-6" onClick={e => e.stopPropagation()}>
                    <button
                      onClick={() => setSelected(log)}
                      aria-label="Détails"
                      className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                      title="Détails"
                    >
                      <Eye className="h-4 w-4" />
                    </button>
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

      {selected && <DetailsModal log={selected} onClose={() => setSelected(null)} />}
    </div>
  )
}
