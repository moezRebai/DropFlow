import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import {
  X, MapPin, Phone, Mail, Package, Euro, Star,
  Plus, Pencil, Trash2, CheckCircle,
  Clock, Truck,
} from 'lucide-react'
import { toast } from 'sonner'
import { Sheet, SheetContent } from '@/components/ui/sheet'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import { clientsApi, clientKeys } from '@/api/clients'
import type { ClientDto, ClientAddressDto } from '@/api/clients'
import { AddressFormDialog } from './AddressFormDialog'
import { STATUS_LABELS, STATUS_COLORS } from '@/api/deliveries'

// ─── Helpers ──────────────────────────────────────────────────────────────────

function getInitials(name: string): string {
  const parts = name.trim().split(' ').filter(Boolean)
  if (parts.length >= 2) return `${parts[0][0]}${parts[1][0]}`.toUpperCase()
  return (parts[0]?.substring(0, 2) ?? '?').toUpperCase()
}

function formatDate(iso?: string): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function formatPrice(n: number): string {
  return n.toLocaleString('fr-FR', { style: 'currency', currency: 'EUR' })
}

type AddressDialog =
  | { type: 'none' }
  | { type: 'add' }
  | { type: 'edit'; address: ClientAddressDto }

// ─── Component ────────────────────────────────────────────────────────────────

interface ClientDetailDialogProps {
  open: boolean
  onClose: () => void
  client: ClientDto | undefined
  onEdit: (client: ClientDto) => void
}

export function ClientDetailDialog({ open, onClose, client, onEdit }: ClientDetailDialogProps) {
  const qc = useQueryClient()
  const [tab, setTab] = useState<'addresses' | 'deliveries'>('addresses')
  const [addrDialog, setAddrDialog] = useState<AddressDialog>({ type: 'none' })

  const clientId = client?.id ?? 0

  const { data: addresses = client?.addresses ?? [], isLoading: addrLoading } = useQuery({
    queryKey: clientKeys.addresses(clientId),
    queryFn: () => clientsApi.getAddresses(clientId),
    enabled: open && clientId > 0,
    initialData: client?.addresses,
  })

  const { data: deliveries = [], isLoading: delivLoading } = useQuery({
    queryKey: clientKeys.deliveries(clientId),
    queryFn: () => clientsApi.getDeliveries(clientId),
    enabled: open && clientId > 0 && tab === 'deliveries',
  })

  function invalidate() {
    qc.invalidateQueries({ queryKey: clientKeys.addresses(clientId) })
    qc.invalidateQueries({ queryKey: clientKeys.lists() })
  }

  // ── Address mutations
  const addAddrMutation = useMutation({
    mutationFn: (data: Parameters<typeof clientsApi.addAddress>[1]) =>
      clientsApi.addAddress(clientId, data),
    onSuccess: () => { toast.success('Adresse ajoutée'); invalidate(); setAddrDialog({ type: 'none' }) },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur') : 'Erreur'),
  })

  const editAddrMutation = useMutation({
    mutationFn: ({ addressId, data }: { addressId: number; data: Parameters<typeof clientsApi.updateAddress>[2] }) =>
      clientsApi.updateAddress(clientId, addressId, data),
    onSuccess: () => { toast.success('Adresse mise à jour'); invalidate(); setAddrDialog({ type: 'none' }) },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur') : 'Erreur'),
  })

  const deleteAddrMutation = useMutation({
    mutationFn: (addressId: number) => clientsApi.deleteAddress(clientId, addressId),
    onSuccess: () => { toast.success('Adresse supprimée'); invalidate() },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Suppression impossible') : 'Erreur'),
  })

  const setDefaultMutation = useMutation({
    mutationFn: (addressId: number) => clientsApi.setDefaultAddress(clientId, addressId),
    onSuccess: () => { toast.success('Adresse par défaut mise à jour'); invalidate() },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur') : 'Erreur'),
  })

  if (!client) return null

  const isVip = client.totalDeliveries >= 3
  const defaultAddr = addresses.find(a => a.isDefault) ?? addresses[0]
  const addrIsPending = addrDialog.type === 'add'
    ? addAddrMutation.isPending
    : editAddrMutation.isPending

  return (
    <>
      <Sheet open={open} onOpenChange={v => !v && onClose()}>
        <SheetContent side="right" className="flex w-full max-w-2xl flex-col gap-0 p-0 [&>button]:hidden">

          {/* Gradient header */}
          <div className="bg-gradient-to-br from-sky-500 to-blue-600 px-6 py-5">
            <div className="flex items-start justify-between">
              <div className="flex items-center gap-4">
                <div className="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-white/20 text-xl font-bold text-white ring-2 ring-white/30">
                  {getInitials(client.displayName)}
                </div>
                <div>
                  <div className="flex items-center gap-2">
                    <h2 className="text-lg font-bold text-white">{client.displayName}</h2>
                    {isVip && (
                      <span className="flex items-center gap-1 rounded-full bg-yellow-300/25 px-2 py-0.5 text-xs font-bold text-yellow-200 ring-1 ring-yellow-300/40">
                        <Star className="h-3 w-3 fill-current" /> VIP
                      </span>
                    )}
                  </div>
                  <p className="text-xs text-sky-200">{defaultAddr?.fullAddress ?? 'Aucune adresse'}</p>
                </div>
              </div>
              <button
                onClick={onClose}
                className="rounded-lg p-1.5 text-white/70 transition-colors hover:bg-white/15 hover:text-white"
              >
                <X className="h-5 w-5" />
              </button>
            </div>

            {/* Stats strip */}
            <div className="mt-4 grid grid-cols-3 gap-3">
              {[
                { icon: <Phone className="h-3.5 w-3.5" />, value: client.phone },
                { icon: <Package className="h-3.5 w-3.5" />, value: `${client.totalDeliveries} livraisons` },
                { icon: <Euro className="h-3.5 w-3.5" />, value: formatPrice(client.totalRevenue) },
              ].map((s, i) => (
                <div key={i} className="flex items-center gap-2 rounded-xl bg-white/10 px-3 py-2">
                  <span className="text-sky-200">{s.icon}</span>
                  <span className="truncate text-xs font-medium text-white">{s.value}</span>
                </div>
              ))}
            </div>
            {client.email && (
              <div className="mt-2 flex items-center gap-2 rounded-xl bg-white/10 px-3 py-2">
                <Mail className="h-3.5 w-3.5 text-sky-200" />
                <span className="text-xs text-white">{client.email}</span>
              </div>
            )}
          </div>

          {/* Action bar */}
          <div className="flex items-center gap-2 border-b px-6 py-3">
            <Button
              size="sm"
              variant="outline"
              className="gap-1.5"
              onClick={() => onEdit(client)}
            >
              <Pencil className="h-3.5 w-3.5" />
              Modifier
            </Button>
            <span className={cn(
              'ml-auto rounded-full px-2.5 py-0.5 text-xs font-semibold',
              client.isActive
                ? 'bg-emerald-100 text-emerald-700'
                : 'bg-slate-100 text-slate-500',
            )}>
              {client.isActive ? 'Actif' : 'Inactif'}
            </span>
          </div>

          {/* Tabs */}
          <div className="flex border-b px-6">
            {(['addresses', 'deliveries'] as const).map(t => (
              <button
                key={t}
                onClick={() => setTab(t)}
                className={cn(
                  'flex items-center gap-1.5 border-b-2 px-1 py-3 text-sm font-medium transition-colors mr-6',
                  tab === t
                    ? 'border-sky-600 text-sky-600'
                    : 'border-transparent text-slate-500 hover:text-slate-700',
                )}
              >
                {t === 'addresses' ? (
                  <><MapPin className="h-3.5 w-3.5" /> Adresses ({addresses.length})</>
                ) : (
                  <><Truck className="h-3.5 w-3.5" /> Livraisons ({client.totalDeliveries})</>
                )}
              </button>
            ))}
          </div>

          {/* Tab content */}
          <div className="flex-1 overflow-y-auto p-6">

            {/* Addresses tab */}
            {tab === 'addresses' && (
              <div className="space-y-3">
                <Button
                  size="sm"
                  variant="outline"
                  className="mb-2 gap-1.5 border-dashed"
                  onClick={() => setAddrDialog({ type: 'add' })}
                >
                  <Plus className="h-3.5 w-3.5" />
                  Ajouter une adresse
                </Button>

                {addrLoading ? (
                  <div className="space-y-3">
                    {[...Array(2)].map((_, i) => <Skeleton key={i} className="h-24 rounded-xl" />)}
                  </div>
                ) : addresses.length === 0 ? (
                  <div className="flex flex-col items-center gap-2 py-10 text-center">
                    <div className="flex h-10 w-10 items-center justify-center rounded-full bg-slate-100 text-slate-400">
                      <MapPin className="h-5 w-5" />
                    </div>
                    <p className="text-sm text-slate-500">Aucune adresse</p>
                  </div>
                ) : (
                  addresses.map(addr => (
                    <div
                      key={addr.id}
                      className={cn(
                        'group rounded-xl border p-4 transition-colors',
                        addr.isDefault ? 'border-sky-200 bg-sky-50' : 'border-slate-200 bg-white hover:border-slate-300',
                      )}
                    >
                      <div className="flex items-start gap-3">
                        <div className={cn(
                          'flex h-9 w-9 shrink-0 items-center justify-center rounded-lg',
                          addr.isDefault ? 'bg-sky-100 text-sky-600' : 'bg-slate-100 text-slate-500',
                        )}>
                          <MapPin className="h-4 w-4" />
                        </div>
                        <div className="min-w-0 flex-1">
                          <div className="flex flex-wrap items-center gap-2">
                            {addr.label && (
                              <span className="text-sm font-semibold text-slate-800">{addr.label}</span>
                            )}
                            {addr.isDefault && (
                              <span className="inline-flex items-center gap-1 rounded-full bg-sky-100 px-2 py-0.5 text-xs font-semibold text-sky-600">
                                <CheckCircle className="h-3 w-3" /> Par défaut
                              </span>
                            )}
                          </div>
                          <p className="mt-0.5 text-sm text-slate-600">{addr.address}</p>
                          <p className="text-sm text-slate-500">{addr.zipCode} {addr.city}</p>
                          {addr.complement && (
                            <p className="text-xs text-slate-400">{addr.complement}</p>
                          )}
                        </div>
                        <div className="flex shrink-0 items-center gap-1 opacity-0 transition-opacity group-hover:opacity-100">
                          {!addr.isDefault && (
                            <button
                              onClick={() => setDefaultMutation.mutate(addr.id)}
                              disabled={setDefaultMutation.isPending}
                              className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-sky-600"
                              title="Définir par défaut"
                            >
                              <CheckCircle className="h-3.5 w-3.5" />
                            </button>
                          )}
                          <button
                            onClick={() => setAddrDialog({ type: 'edit', address: addr })}
                            className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-700"
                            title="Modifier"
                          >
                            <Pencil className="h-3.5 w-3.5" />
                          </button>
                          {!addr.isDefault && (
                            <button
                              onClick={() => deleteAddrMutation.mutate(addr.id)}
                              disabled={deleteAddrMutation.isPending}
                              className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-red-50 hover:text-red-500"
                              title="Supprimer"
                            >
                              <Trash2 className="h-3.5 w-3.5" />
                            </button>
                          )}
                        </div>
                      </div>
                    </div>
                  ))
                )}
              </div>
            )}

            {/* Deliveries tab */}
            {tab === 'deliveries' && (
              <div className="space-y-2">
                {delivLoading ? (
                  <div className="space-y-2">
                    {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-16 rounded-xl" />)}
                  </div>
                ) : deliveries.length === 0 ? (
                  <div className="flex flex-col items-center gap-2 py-10 text-center">
                    <div className="flex h-10 w-10 items-center justify-center rounded-full bg-slate-100 text-slate-400">
                      <Truck className="h-5 w-5" />
                    </div>
                    <p className="text-sm text-slate-500">Aucune livraison</p>
                  </div>
                ) : (
                  deliveries.map(d => (
                    <div key={d.id} className="flex items-center gap-3 rounded-xl border border-slate-200 bg-white p-3">
                      <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-slate-100 text-slate-500">
                        <Package className="h-4 w-4" />
                      </div>
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-semibold text-slate-800">{d.reference}</p>
                        <div className="flex items-center gap-2">
                          <p className="flex items-center gap-1 text-xs text-slate-400">
                            <Clock className="h-3 w-3" />
                            {formatDate(d.scheduledDate)}
                          </p>
                          {d.storeName && (
                            <p className="text-xs text-slate-400">· {d.storeName}</p>
                          )}
                        </div>
                      </div>
                      <div className="flex shrink-0 flex-col items-end gap-1">
                        <span className={cn(
                          'rounded-full border px-2 py-0.5 text-xs font-semibold',
                          STATUS_COLORS[d.status as keyof typeof STATUS_COLORS] ?? 'bg-slate-100 text-slate-500 border-slate-200',
                        )}>
                          {STATUS_LABELS[d.status as keyof typeof STATUS_LABELS] ?? d.status}
                        </span>
                        <span className="text-xs font-semibold text-slate-700">{formatPrice(d.price)}</span>
                      </div>
                    </div>
                  ))
                )}
              </div>
            )}
          </div>
        </SheetContent>
      </Sheet>

      {/* Address form — renders on top of the sheet */}
      <AddressFormDialog
        open={addrDialog.type !== 'none'}
        onClose={() => setAddrDialog({ type: 'none' })}
        address={addrDialog.type === 'edit' ? addrDialog.address : undefined}
        isSubmitting={addrIsPending}
        onSubmit={async (data) => {
          if (addrDialog.type === 'add') {
            await addAddrMutation.mutateAsync(data)
          } else if (addrDialog.type === 'edit') {
            await editAddrMutation.mutateAsync({ addressId: addrDialog.address.id, data })
          }
        }}
      />
    </>
  )
}
