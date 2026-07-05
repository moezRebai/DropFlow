import { useState, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Search, X, Package, MapPin, Calendar, AlertTriangle, ChevronDown,
  Eye, Clock, FileText, Phone, Mail, Layers, ExternalLink, Timer, RefreshCw,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import { deliveriesApi, deliveryKeys, DeliveryType, TYPE_LABELS } from '@/api/deliveries'
import type { DeliveryDto } from '@/api/deliveries'
import { useWizardStore } from '@/store/wizardStore'
import { useDeliveryBroadcastListener } from '@/hooks/useDeliveryBroadcast'

// ─── DeliveryDetailModal ──────────────────────────────────────────────────────

function DeliveryDetailModal({ delivery, onClose }: { delivery: DeliveryDto; onClose: () => void }) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4" onClick={onClose}>
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" />
      <div
        className="relative z-10 w-full max-w-md overflow-hidden rounded-2xl bg-white shadow-2xl"
        onClick={e => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-start justify-between gap-3 border-b bg-slate-50 px-5 py-4">
          <div>
            <p className="font-mono text-xs text-slate-400">{delivery.reference}</p>
            <h3 className="text-base font-semibold text-slate-800">{delivery.clientName}</h3>
            {delivery.type === DeliveryType.Urgent && (
              <span className="mt-0.5 inline-block rounded-full bg-red-100 px-2 py-0.5 text-xs font-semibold text-red-600">
                {TYPE_LABELS[DeliveryType.Urgent]}
              </span>
            )}
          </div>
          <button onClick={onClose} className="rounded-lg p-1 text-slate-400 hover:bg-slate-200 hover:text-slate-600">
            <X className="h-4 w-4" />
          </button>
        </div>

        <div className="max-h-[60vh] overflow-y-auto px-5 py-4 space-y-4">
          {/* Address */}
          <div>
            <p className="mb-1 text-xs font-semibold uppercase tracking-wider text-slate-400">Adresse</p>
            <div className="flex items-start gap-2 text-sm text-slate-700">
              <MapPin className="mt-0.5 h-4 w-4 shrink-0 text-slate-400" />
              <div>
                <p>{delivery.address}</p>
                {delivery.addressComplement && <p className="text-xs text-slate-500">{delivery.addressComplement}</p>}
                <p>{delivery.zipCode} {delivery.city}</p>
              </div>
            </div>
          </div>

          {/* Contact */}
          {(delivery.clientPhone || delivery.clientEmail) && (
            <div>
              <p className="mb-1 text-xs font-semibold uppercase tracking-wider text-slate-400">Contact</p>
              <div className="space-y-1">
                {delivery.clientPhone && (
                  <div className="flex items-center gap-2 text-sm text-slate-700">
                    <Phone className="h-3.5 w-3.5 text-slate-400" />
                    <a href={`tel:${delivery.clientPhone}`} className="hover:text-sky-600">{delivery.clientPhone}</a>
                  </div>
                )}
                {delivery.clientEmail && (
                  <div className="flex items-center gap-2 text-sm text-slate-700">
                    <Mail className="h-3.5 w-3.5 text-slate-400" />
                    <span className="truncate">{delivery.clientEmail}</span>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Duration + packages */}
          <div className="flex flex-wrap gap-3">
            {delivery.estimatedDurationMinutes && (
              <div className="flex items-center gap-1.5 rounded-lg border border-amber-200 bg-amber-50 px-3 py-1.5">
                <Timer className="h-3.5 w-3.5 text-amber-500" />
                <span className="text-xs font-medium text-amber-700">{delivery.estimatedDurationMinutes} min prestation</span>
              </div>
            )}
            {delivery.totalPackages > 0 && (
              <div className="flex items-center gap-1.5 rounded-lg border border-slate-200 bg-slate-50 px-3 py-1.5">
                <Layers className="h-3.5 w-3.5 text-slate-500" />
                <span className="text-xs font-medium text-slate-700">{delivery.totalPackages} colis</span>
              </div>
            )}
            {delivery.withAssembly && (
              <div className="flex items-center gap-1.5 rounded-lg border border-purple-200 bg-purple-50 px-3 py-1.5">
                <span className="text-xs font-medium text-purple-700">Avec montage</span>
              </div>
            )}
          </div>

          {/* Items */}
          {delivery.items.length > 0 && (
            <div>
              <p className="mb-1.5 text-xs font-semibold uppercase tracking-wider text-slate-400">Articles ({delivery.items.length})</p>
              <div className="space-y-1 rounded-lg border bg-slate-50 divide-y">
                {delivery.items.map(item => (
                  <div key={item.id} className="flex items-center justify-between px-3 py-2 text-xs">
                    <span className="text-slate-700">{item.designation}</span>
                    <span className="font-semibold text-slate-800">× {item.quantity}</span>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Notes client */}
          {delivery.deliveryNotes && (
            <div>
              <p className="mb-1 text-xs font-semibold uppercase tracking-wider text-slate-400">Notes client</p>
              <div className="flex items-start gap-2 rounded-lg border border-sky-200 bg-sky-50 px-3 py-2">
                <FileText className="mt-0.5 h-3.5 w-3.5 shrink-0 text-sky-500" />
                <p className="text-xs text-sky-800">{delivery.deliveryNotes}</p>
              </div>
            </div>
          )}

          {/* Notes internes */}
          {delivery.internalNotes && (
            <div>
              <p className="mb-1 text-xs font-semibold uppercase tracking-wider text-slate-400">Notes internes</p>
              <div className="flex items-start gap-2 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
                <FileText className="mt-0.5 h-3.5 w-3.5 shrink-0 text-slate-400" />
                <p className="text-xs text-slate-600">{delivery.internalNotes}</p>
              </div>
            </div>
          )}

          {/* Store + créneau */}
          <div className="flex flex-wrap gap-x-6 gap-y-1 text-xs text-slate-500">
            {delivery.storeName && <span>Enseigne : <strong className="text-slate-700">{delivery.storeName}</strong></span>}
            {delivery.timeSlot && (
              <span className="flex items-center gap-1">
                <Clock className="h-3 w-3" />{delivery.timeSlot.label} ({delivery.timeSlot.startTime.substring(0, 5)} – {delivery.timeSlot.endTime.substring(0, 5)})
              </span>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between border-t px-5 py-3">
          <span className="text-sm font-semibold text-slate-800">{delivery.price.toFixed(2)} €</span>
          <a
            href={`/deliveries/${delivery.id}/edit`}
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center gap-1.5 rounded-lg border px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50"
            onClick={e => e.stopPropagation()}
          >
            <ExternalLink className="h-3.5 w-3.5" />Modifier la livraison
          </a>
        </div>
      </div>
    </div>
  )
}

// ─── Step2Deliveries ──────────────────────────────────────────────────────────

export function Step2Deliveries() {
  const wizard = useWizardStore()
  const [search, setSearch] = useState('')
  const [storeFilter, setStoreFilter] = useState<number | undefined>()
  const [error, setError] = useState('')
  const [detailDeliveryId, setDetailDeliveryId] = useState<number | null>(null)

  const editRouteId = wizard.editRouteId ?? undefined

  const { data: result, isLoading, isFetching, refetch } = useQuery({
    queryKey: deliveryKeys.availableForRoute(wizard.date, editRouteId),
    queryFn: () => deliveriesApi.availableForRoute(wizard.date, editRouteId),
    enabled: !!wizard.date,
  })

  const allDeliveries = result?.data ?? []

  // Dériver la livraison du détail depuis la liste fraîche (se met à jour auto après refetch)
  const detailDelivery = detailDeliveryId
    ? (allDeliveries.find(d => d.id === detailDeliveryId) ?? null)
    : null

  async function reloadDeliveries() {
    const { data: freshResult } = await refetch()
    if (!freshResult?.data) return
    const fresh = freshResult.data
    // Synchroniser les livraisons sélectionnées avec les données fraîches
    const updated = wizard.selectedDeliveries.map(sel => fresh.find(d => d.id === sel.id) ?? sel)
    wizard.setSelectedDeliveries(updated)
  }

  useDeliveryBroadcastListener(() => {
    reloadDeliveries()
  })

  const stores = useMemo(() => {
    const map = new Map<number, string>()
    allDeliveries.forEach(d => map.set(d.storeId, d.storeName))
    return Array.from(map.entries()).map(([id, name]) => ({ id, name }))
  }, [allDeliveries])

  const filtered = useMemo(() => {
    const q = search.toLowerCase()
    return allDeliveries.filter(d => {
      if (storeFilter && d.storeId !== storeFilter) return false
      if (!q) return true
      return (
        d.reference.toLowerCase().includes(q) ||
        d.clientName.toLowerCase().includes(q) ||
        d.city.toLowerCase().includes(q) ||
        d.address.toLowerCase().includes(q)
      )
    })
  }, [allDeliveries, search, storeFilter])

  const selectedIds = new Set(wizard.selectedDeliveries.map(d => d.id))

  function toggle(id: number) {
    const delivery = allDeliveries.find(d => d.id === id)
    if (!delivery) return
    const next = selectedIds.has(id)
      ? wizard.selectedDeliveries.filter(d => d.id !== id)
      : [...wizard.selectedDeliveries, delivery]
    wizard.setSelectedDeliveries(next)
    setError('')
  }

  function toggleAll() {
    const allFiltered = filtered.map(d => d.id)
    const allSelected = allFiltered.every(id => selectedIds.has(id))
    if (allSelected) {
      wizard.setSelectedDeliveries(wizard.selectedDeliveries.filter(d => !allFiltered.includes(d.id)))
    } else {
      const toAdd = filtered.filter(d => !selectedIds.has(d.id))
      wizard.setSelectedDeliveries([...wizard.selectedDeliveries, ...toAdd])
    }
  }

  function handleNext() {
    if (wizard.selectedDeliveries.length === 0) {
      setError('Sélectionnez au moins une livraison')
      return
    }
    wizard.next()
  }

  const allFilteredSelected = filtered.length > 0 && filtered.every(d => selectedIds.has(d.id))

  return (
    <div className="flex flex-col gap-4">

      {/* Toolbar */}
      <div className="flex items-center gap-3 flex-wrap">
        <div className="relative flex-1 min-w-48">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <Input
            className="pl-9 pr-9"
            placeholder="Référence, client, ville…"
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
          {search && (
            <button onClick={() => setSearch('')} className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
              <X className="h-4 w-4" />
            </button>
          )}
        </div>
        {stores.length > 1 && (
          <div className="relative">
            <select
              value={storeFilter ?? ''}
              onChange={e => setStoreFilter(e.target.value ? Number(e.target.value) : undefined)}
              className="appearance-none rounded-md border bg-white px-3 py-2 text-sm pr-7 focus:outline-none focus:ring-2 focus:ring-sky-500"
            >
              <option value="">Toutes les enseignes</option>
              {stores.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select>
            <ChevronDown className="pointer-events-none absolute right-2 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-slate-400" />
          </div>
        )}
      </div>

      {/* Counter */}
      <div className="flex items-center justify-between">
        <p className="text-sm text-slate-500">
          {isLoading ? '…' : `${filtered.length} livraison${filtered.length > 1 ? 's' : ''} disponible${filtered.length > 1 ? 's' : ''}`}
        </p>
        {wizard.selectedDeliveries.length > 0 && (
          <span className="rounded-full bg-sky-100 px-3 py-0.5 text-xs font-semibold text-sky-700">
            {wizard.selectedDeliveries.length} sélectionnée{wizard.selectedDeliveries.length > 1 ? 's' : ''}
          </span>
        )}
      </div>

      {/* Table */}
      <div className="overflow-hidden rounded-xl border bg-white shadow-sm">
        <div className="flex items-center gap-3 border-b bg-slate-50 px-4 py-2.5">
          <input
            type="checkbox"
            checked={allFilteredSelected}
            onChange={toggleAll}
            disabled={filtered.length === 0}
            className="h-4 w-4 rounded border-slate-300 text-sky-600 focus:ring-sky-500"
          />
          <span className="text-xs font-semibold uppercase tracking-wider text-slate-400">Tout sélectionner</span>
        </div>

        <div className="divide-y max-h-[420px] overflow-y-auto">
          {isLoading ? (
            [...Array(5)].map((_, i) => (
              <div key={i} className="flex items-center gap-3 px-4 py-3">
                <Skeleton className="h-4 w-4 rounded" />
                <Skeleton className="h-4 flex-1" />
              </div>
            ))
          ) : filtered.length === 0 ? (
            <div className="flex flex-col items-center gap-3 py-10">
              <Package className="h-8 w-8 text-slate-300" />
              <p className="text-sm text-slate-400">
                {allDeliveries.length === 0
                  ? `Aucune livraison disponible pour le ${new Date(wizard.date).toLocaleDateString('fr-FR')}`
                  : 'Aucun résultat pour ce filtre'}
              </p>
            </div>
          ) : (
            filtered.map(delivery => {
              const selected = selectedIds.has(delivery.id)
              return (
                <div
                  key={delivery.id}
                  onClick={() => toggle(delivery.id)}
                  className={cn(
                    'flex cursor-pointer items-start gap-3 px-4 py-3 transition-colors hover:bg-sky-50/50',
                    selected && 'bg-sky-50'
                  )}
                >
                  <input
                    type="checkbox"
                    checked={selected}
                    onChange={() => toggle(delivery.id)}
                    onClick={e => e.stopPropagation()}
                    className="mt-0.5 h-4 w-4 shrink-0 rounded border-slate-300 text-sky-600 focus:ring-sky-500"
                  />
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="font-mono text-xs text-slate-400">{delivery.reference}</span>
                      <span className="text-sm font-medium text-slate-800">{delivery.clientName}</span>
                      {delivery.type === DeliveryType.Urgent && (
                        <span className="rounded-full bg-red-100 px-1.5 py-0.5 text-xs font-semibold text-red-600">
                          {TYPE_LABELS[DeliveryType.Urgent]}
                        </span>
                      )}
                    </div>
                    <div className="mt-0.5 flex items-center gap-3 text-xs text-slate-500 flex-wrap">
                      <span className="flex items-center gap-0.5">
                        <MapPin className="h-3 w-3" />{delivery.city}
                      </span>
                      <span className="truncate max-w-48">{delivery.address}</span>
                      {delivery.storeName && (
                        <span className="shrink-0 text-slate-400">{delivery.storeName}</span>
                      )}
                    </div>
                    {/* Duration + notes */}
                    <div className="mt-1 flex flex-wrap items-center gap-2">
                      {delivery.estimatedDurationMinutes && (
                        <span className="flex items-center gap-1 rounded-md border border-amber-200 bg-amber-50 px-1.5 py-0.5 text-xs text-amber-700">
                          <Timer className="h-3 w-3" />{delivery.estimatedDurationMinutes} min
                        </span>
                      )}
                      {delivery.totalPackages > 0 && (
                        <span className="flex items-center gap-1 text-xs text-slate-400">
                          <Layers className="h-3 w-3" />{delivery.totalPackages} colis
                        </span>
                      )}
                      {delivery.deliveryNotes && (
                        <span className="flex items-center gap-1 text-xs text-sky-600 max-w-xs truncate">
                          <FileText className="h-3 w-3 shrink-0" />
                          {delivery.deliveryNotes.length > 60
                            ? delivery.deliveryNotes.substring(0, 60) + '…'
                            : delivery.deliveryNotes}
                        </span>
                      )}
                    </div>
                    {delivery.scheduledDate && (
                      <div className="mt-0.5 flex items-center gap-0.5 text-xs text-amber-600">
                        <Calendar className="h-3 w-3" />
                        Prévue le {new Date(delivery.scheduledDate).toLocaleDateString('fr-FR')}
                      </div>
                    )}
                  </div>
                  <div className="flex shrink-0 flex-col items-end gap-1.5">
                    <div className="flex items-center gap-1">
                      <button
                        onClick={e => { e.stopPropagation(); setDetailDeliveryId(delivery.id) }}
                        className="flex items-center gap-1 rounded-lg border border-sky-200 bg-sky-50 px-2 py-1 text-xs font-medium text-sky-600 hover:bg-sky-100 hover:border-sky-300 transition-colors"
                        title="Voir le détail"
                      >
                        <Eye className="h-3.5 w-3.5" />Détail
                      </button>
                      <button
                        onClick={e => { e.stopPropagation(); reloadDeliveries() }}
                        disabled={isFetching}
                        className="flex items-center justify-center rounded-lg border border-slate-200 bg-white p-1 text-slate-400 hover:border-slate-300 hover:bg-slate-50 hover:text-slate-600 disabled:opacity-50 transition-colors"
                        title="Actualiser cette livraison"
                      >
                        <RefreshCw className={cn('h-3.5 w-3.5', isFetching && 'animate-spin')} />
                      </button>
                    </div>
                    <p className="text-sm font-semibold text-slate-700">{delivery.price.toFixed(2)} €</p>
                    {delivery.totalPackages > 0 && (
                      <p className="text-xs text-slate-400">{delivery.totalPackages} colis</p>
                    )}
                  </div>
                </div>
              )
            })
          )}
        </div>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-600">
          <AlertTriangle className="h-4 w-4 shrink-0" />{error}
        </div>
      )}

      {/* Nav */}
      <div className="flex justify-between pt-2">
        <Button variant="outline" onClick={wizard.prev}>← Précédent</Button>
        <Button onClick={handleNext} className="bg-sky-600 hover:bg-sky-700 text-white px-6">
          Étape suivante →
        </Button>
      </div>

      {/* Detail modal */}
      {detailDelivery && (
        <DeliveryDetailModal delivery={detailDelivery} onClose={() => setDetailDeliveryId(null)} />
      )}
    </div>
  )
}
