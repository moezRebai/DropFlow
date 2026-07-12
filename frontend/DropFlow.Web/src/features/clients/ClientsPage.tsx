import { useState, useEffect, useCallback } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import {
  Users, Plus, Search, X, RefreshCw, Eye, Pencil, Trash2,
  Star, MapPin, Phone, Mail, ChevronLeft, ChevronRight,
  AlertTriangle, Package, Euro,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table'
import { cn } from '@/lib/utils'
import { clientsApi, clientKeys } from '@/api/clients'
import type { ClientDto, ClientFilterDto } from '@/api/clients'
import { ClientFormDialog } from './ClientFormDialog'
import { ClientDetailDialog } from './ClientDetailDialog'

// ─── Helpers ──────────────────────────────────────────────────────────────────

function getInitials(name: string): string {
  const parts = name.trim().split(' ').filter(Boolean)
  if (parts.length >= 2) return `${parts[0][0]}${parts[1][0]}`.toUpperCase()
  return (parts[0]?.substring(0, 2) ?? '?').toUpperCase()
}

function formatRevenue(n: number): string {
  return n.toLocaleString('fr-FR', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 })
}

const PAGE_SIZE = 20

// ─── Dialog state ─────────────────────────────────────────────────────────────

type DialogState =
  | { type: 'none' }
  | { type: 'create' }
  | { type: 'edit'; client: ClientDto }
  | { type: 'detail'; client: ClientDto }
  | { type: 'delete'; client: ClientDto }

// ─── Delete confirmation modal ────────────────────────────────────────────────

function DeleteConfirmModal({
  client,
  onConfirm,
  onCancel,
  isPending,
}: {
  client: ClientDto
  onConfirm: () => void
  onCancel: () => void
  isPending: boolean
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-card shadow-2xl">
        <div className="p-6">
          <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100 dark:bg-red-500/15">
            <AlertTriangle className="h-6 w-6 text-red-600 dark:text-red-400" />
          </div>
          <h3 className="mb-1 text-base font-semibold text-foreground">Supprimer ce client ?</h3>
          <p className="text-sm text-muted-foreground">
            <strong>{client.displayName}</strong> sera définitivement supprimé. Cette action est irréversible.
          </p>
          {client.totalDeliveries > 0 && (
            <p className="mt-2 rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-700 dark:bg-amber-500/10 dark:text-amber-300">
              Ce client a {client.totalDeliveries} livraison(s) associée(s). La suppression peut échouer.
            </p>
          )}
        </div>
        <div className="flex gap-3 border-t px-6 py-4">
          <Button variant="outline" className="flex-1" onClick={onCancel}>
            Annuler
          </Button>
          <Button
            variant="destructive"
            className="flex-1"
            onClick={onConfirm}
            disabled={isPending}
          >
            {isPending ? (
              <span className="flex items-center gap-2">
                <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                Suppression…
              </span>
            ) : 'Supprimer'}
          </Button>
        </div>
      </div>
    </div>
  )
}

// ─── ClientsPage ──────────────────────────────────────────────────────────────

export default function ClientsPage() {
  const qc = useQueryClient()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [dialog, setDialog] = useState<DialogState>({ type: 'none' })

  // Debounce search
  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(searchInput)
      setPage(1)
    }, 300)
    return () => clearTimeout(t)
  }, [searchInput])

  const filters: ClientFilterDto = {
    page,
    pageSize: PAGE_SIZE,
    searchTerm: debouncedSearch || undefined,
  }

  const { data, isLoading, refetch } = useQuery({
    queryKey: clientKeys.list(filters),
    queryFn: () => clientsApi.getList(filters),
    placeholderData: prev => prev,
  })

  const deleteMutation = useMutation({
    mutationFn: (id: number) => clientsApi.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: clientKeys.lists() })
      toast.success('Client supprimé')
      setDialog({ type: 'none' })
    },
    onError: (err) => {
      const msg = isAxiosError(err)
        ? (err.response?.data?.message ?? 'Suppression impossible — ce client a des livraisons associées')
        : 'Une erreur est survenue'
      toast.error(msg)
    },
  })

  const closeDialog = useCallback(() => setDialog({ type: 'none' }), [])

  const clients = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 1
  const from = Math.min((page - 1) * PAGE_SIZE + 1, totalCount)
  const to = Math.min(page * PAGE_SIZE, totalCount)

  // Compute page-level stats
  const vipCount = clients.filter(c => c.totalDeliveries >= 3).length
  const pageRevenue = clients.reduce((s, c) => s + c.totalRevenue, 0)

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* ── Hero ──────────────────────────────────────────────────────── */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 p-6 shadow-lg">
        <div
          className="absolute inset-0 opacity-10"
          style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }}
        />
        <div className="relative flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="mb-1 flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
                <Users className="h-5 w-5 text-white" />
              </div>
              <h1 className="text-2xl font-bold tracking-tight text-white">Clients</h1>
            </div>
            <p className="text-sm text-sky-200">Gérez et consultez votre base clients</p>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            {/* Stats chips */}
            {!isLoading && (
              <>
                <StatChip icon={<Users className="h-3.5 w-3.5" />} label={`${totalCount} clients`} />
                <StatChip icon={<Star className="h-3.5 w-3.5" />} label={`${vipCount} VIP`} gold />
                <StatChip icon={<Euro className="h-3.5 w-3.5" />} label={formatRevenue(pageRevenue)} />
              </>
            )}
            <button
              onClick={() => setDialog({ type: 'create' })}
              className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white backdrop-blur-sm transition-colors hover:bg-white/25"
            >
              <Plus className="h-3.5 w-3.5" />
              Nouveau client
            </button>
          </div>
        </div>
      </div>

      {/* ── Toolbar ───────────────────────────────────────────────────── */}
      <div className="flex items-center gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            className="pl-9 pr-9"
            placeholder="Rechercher un client, email, téléphone…"
            value={searchInput}
            onChange={e => setSearchInput(e.target.value)}
          />
          {searchInput && (
            <button
              onClick={() => setSearchInput('')}
              aria-label="Effacer la recherche"
              className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </div>
        <Button
          variant="outline"
          size="icon"
          onClick={() => refetch()}
          title="Actualiser"
        >
          <RefreshCw className="h-4 w-4" />
        </Button>
      </div>

      {/* ── Table ─────────────────────────────────────────────────────── */}
      <div className="overflow-hidden rounded-2xl border bg-card shadow-sm">
        <Table>
          <TableHeader>
            <TableRow className="bg-muted hover:bg-muted">
              <TableHead className="pl-6 text-xs font-semibold uppercase tracking-wider text-muted-foreground">Client</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Téléphone</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Ville</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Livraisons</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">CA total</TableHead>
              <TableHead className="w-24 pr-6" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              [...Array(6)].map((_, i) => (
                <TableRow key={i}>
                  <TableCell className="pl-6"><Skeleton className="h-10 w-48 rounded-lg" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-12" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-20" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-16" /></TableCell>
                </TableRow>
              ))
            ) : clients.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="py-16 text-center">
                  <div className="flex flex-col items-center gap-3">
                    <div className="flex h-12 w-12 items-center justify-center rounded-full bg-muted text-muted-foreground">
                      <Users className="h-6 w-6" />
                    </div>
                    <p className="text-sm font-medium text-muted-foreground">
                      {debouncedSearch ? 'Aucun client ne correspond à votre recherche' : 'Aucun client'}
                    </p>
                    {!debouncedSearch && (
                      <Button size="sm" onClick={() => setDialog({ type: 'create' })}>
                        <Plus className="mr-1.5 h-3.5 w-3.5" />
                        Créer le premier client
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              clients.map(client => {
                const isVip = client.totalDeliveries >= 3
                const defaultAddr = client.addresses.find(a => a.isDefault) ?? client.addresses[0]
                return (
                  <TableRow
                    key={client.id}
                    className={cn(
                      'cursor-pointer transition-colors hover:bg-sky-50/40 dark:hover:bg-sky-500/5',
                      !client.isActive && 'opacity-60',
                    )}
                    onClick={() => setDialog({ type: 'detail', client })}
                  >
                    {/* Client */}
                    <TableCell className="pl-6 py-3">
                      <div className="flex items-center gap-3">
                        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-sky-500 to-blue-600 text-sm font-bold text-white">
                          {getInitials(client.displayName)}
                        </div>
                        <div className="min-w-0">
                          <div className="flex items-center gap-1.5">
                            <p className="truncate font-semibold text-foreground">{client.displayName}</p>
                            {isVip && (
                              <span className="flex shrink-0 items-center gap-0.5 rounded-full bg-gradient-to-r from-amber-400 to-yellow-500 px-1.5 py-0.5 text-xs font-bold text-white shadow-sm">
                                <Star className="h-2.5 w-2.5 fill-current" /> VIP
                              </span>
                            )}
                            {!client.isActive && (
                              <span className="shrink-0 rounded-full bg-muted px-1.5 py-0.5 text-xs text-muted-foreground">
                                Inactif
                              </span>
                            )}
                          </div>
                          {client.email && (
                            <p className="flex items-center gap-1 truncate text-xs text-muted-foreground">
                              <Mail className="h-3 w-3 shrink-0" />
                              {client.email}
                            </p>
                          )}
                        </div>
                      </div>
                    </TableCell>

                    {/* Téléphone */}
                    <TableCell>
                      <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
                        <Phone className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
                        {client.phone}
                      </div>
                    </TableCell>

                    {/* Ville */}
                    <TableCell>
                      {defaultAddr ? (
                        <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
                          <MapPin className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
                          {defaultAddr.city}
                        </div>
                      ) : (
                        <span className="text-sm text-muted-foreground">—</span>
                      )}
                    </TableCell>

                    {/* Livraisons */}
                    <TableCell>
                      <div className="flex items-center gap-1.5">
                        <Package className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
                        <span className="text-sm font-medium text-foreground">{client.totalDeliveries}</span>
                      </div>
                    </TableCell>

                    {/* CA */}
                    <TableCell>
                      <span className="text-sm font-semibold text-foreground">
                        {formatRevenue(client.totalRevenue)}
                      </span>
                    </TableCell>

                    {/* Actions */}
                    <TableCell className="pr-6" onClick={e => e.stopPropagation()}>
                      <div className="flex items-center justify-end gap-1">
                        <button
                          onClick={() => setDialog({ type: 'detail', client })}
                          aria-label="Voir le détail"
                          className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                          title="Voir le détail"
                        >
                          <Eye className="h-4 w-4" />
                        </button>
                        <button
                          onClick={() => setDialog({ type: 'edit', client })}
                          aria-label="Modifier"
                          className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                          title="Modifier"
                        >
                          <Pencil className="h-4 w-4" />
                        </button>
                        <button
                          onClick={() => setDialog({ type: 'delete', client })}
                          aria-label="Supprimer"
                          className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-red-50 hover:text-red-500 dark:hover:bg-red-500/10 dark:hover:text-red-400"
                          title="Supprimer"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </div>
                    </TableCell>
                  </TableRow>
                )
              })
            )}
          </TableBody>
        </Table>
      </div>

      {/* ── Pagination ────────────────────────────────────────────────── */}
      {totalCount > 0 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Affichage de <strong>{from}</strong> à <strong>{to}</strong> sur{' '}
            <strong>{totalCount}</strong> client{totalCount > 1 ? 's' : ''}
          </p>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
            >
              <ChevronLeft className="h-4 w-4" />
              Précédent
            </Button>
            <span className="rounded-lg border bg-card px-3 py-1.5 text-sm font-medium text-foreground">
              {page} / {totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              disabled={page >= totalPages}
            >
              Suivant
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}

      {/* ── Dialogs ───────────────────────────────────────────────────── */}
      <ClientFormDialog
        open={dialog.type === 'create' || dialog.type === 'edit'}
        client={dialog.type === 'edit' ? dialog.client : undefined}
        onClose={closeDialog}
      />

      <ClientDetailDialog
        open={dialog.type === 'detail'}
        client={dialog.type === 'detail' ? dialog.client : undefined}
        onClose={closeDialog}
        onEdit={client => setDialog({ type: 'edit', client })}
      />

      {dialog.type === 'delete' && (
        <DeleteConfirmModal
          client={dialog.client}
          onConfirm={() => deleteMutation.mutate(dialog.client.id)}
          onCancel={closeDialog}
          isPending={deleteMutation.isPending}
        />
      )}
    </div>
  )
}

// ─── Stat chip ────────────────────────────────────────────────────────────────

function StatChip({ icon, label, gold = false }: { icon: React.ReactNode; label: string; gold?: boolean }) {
  return (
    <span className={cn(
      'inline-flex items-center gap-1.5 rounded-xl px-3 py-1.5 text-xs font-semibold',
      gold ? 'bg-yellow-300/25 text-yellow-200 ring-1 ring-yellow-300/40' : 'bg-white/15 text-white',
    )}>
      {icon}
      {label}
    </span>
  )
}
