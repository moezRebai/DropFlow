import { useParams, useNavigate, Link } from 'react-router-dom'
import { useQuery, useMutation } from '@tanstack/react-query'
import {
  ArrowLeft, Edit, MapPin, Phone, Mail, Package, Wrench,
  Trash2, Store, FileText, CalendarDays, Clock,
  Route, Euro, CreditCard, Building2, StickyNote, AlertTriangle, Check,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table'
import {
  deliveriesApi, deliveryKeys, DeliveryStatus, DeliveryType,
  STATUS_LABELS, URGENT_BADGE_CLASS, type DeliveryDto,
} from '@/api/deliveries'
import { DeliveryStatusBadge } from './DeliveryStatusBadge'
import { cn } from '@/lib/utils'

// ─── Status hero config ───────────────────────────────────────────────────────

const STATUS_HERO: Record<DeliveryStatus, { bg: string; border: string; text: string; accent: string }> = {
  [DeliveryStatus.ToBePlanned]: {
    bg: 'bg-amber-50 dark:bg-amber-950/30',
    border: 'border-amber-200 dark:border-amber-800',
    text: 'text-amber-800 dark:text-amber-200',
    accent: 'bg-amber-500',
  },
  [DeliveryStatus.Confirmed]: {
    bg: 'bg-blue-50 dark:bg-blue-950/30',
    border: 'border-blue-200 dark:border-blue-800',
    text: 'text-blue-800 dark:text-blue-200',
    accent: 'bg-blue-500',
  },
  [DeliveryStatus.InProgress]: {
    bg: 'bg-purple-50 dark:bg-purple-950/30',
    border: 'border-purple-200 dark:border-purple-800',
    text: 'text-purple-800 dark:text-purple-200',
    accent: 'bg-purple-500',
  },
  [DeliveryStatus.Delivered]: {
    bg: 'bg-emerald-50 dark:bg-emerald-950/30',
    border: 'border-emerald-200 dark:border-emerald-800',
    text: 'text-emerald-800 dark:text-emerald-200',
    accent: 'bg-emerald-500',
  },
  [DeliveryStatus.Canceled]: {
    bg: 'bg-red-50 dark:bg-red-950/30',
    border: 'border-red-200 dark:border-red-800',
    text: 'text-red-700 dark:text-red-300',
    accent: 'bg-red-500',
  },
}

// ─── Status steps ─────────────────────────────────────────────────────────────

const STATUS_STEPS = [
  DeliveryStatus.ToBePlanned,
  DeliveryStatus.Confirmed,
  DeliveryStatus.InProgress,
  DeliveryStatus.Delivered,
]

// ─── Helpers ──────────────────────────────────────────────────────────────────

function formatDate(iso?: string) {
  if (!iso) return null
  return new Date(iso).toLocaleDateString('fr-FR', { day: '2-digit', month: 'long', year: 'numeric' })
}

function formatDateTime(iso?: string) {
  if (!iso) return null
  return new Date(iso).toLocaleDateString('fr-FR', {
    day: '2-digit', month: 'short', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}

function formatPrice(n?: number | null) {
  if (n == null) return null
  return n.toLocaleString('fr-FR', { style: 'currency', currency: 'EUR' })
}

// ─── Stat tile ────────────────────────────────────────────────────────────────

function StatTile({
  icon: Icon, label, value, colorClass,
}: {
  icon: React.ElementType
  label: string
  value: string
  colorClass: string
}) {
  return (
    <div className="flex items-center gap-3 rounded-xl border bg-card p-4">
      <div className={cn('flex h-10 w-10 shrink-0 items-center justify-center rounded-lg', colorClass)}>
        <Icon className="h-5 w-5 text-white" />
      </div>
      <div className="min-w-0">
        <p className="text-xs text-muted-foreground">{label}</p>
        <p className="truncate font-semibold">{value}</p>
      </div>
    </div>
  )
}

// ─── Info row ─────────────────────────────────────────────────────────────────

function InfoRow({
  icon: Icon, label, value, iconClass = 'text-muted-foreground',
}: {
  icon: React.ElementType
  label: string
  value?: string | null
  iconClass?: string
}) {
  if (!value) return null
  return (
    <div className="flex items-start gap-3 py-2 text-sm">
      <Icon className={cn('mt-0.5 h-4 w-4 shrink-0', iconClass)} />
      <div className="flex flex-1 justify-between gap-4">
        <span className="text-muted-foreground">{label}</span>
        <span className="text-right font-medium">{value}</span>
      </div>
    </div>
  )
}

// ─── Main ─────────────────────────────────────────────────────────────────────

export default function DeliveryDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const deliveryId = Number(id)

  const { data: result, isLoading } = useQuery({
    queryKey: deliveryKeys.detail(deliveryId),
    queryFn: () => deliveriesApi.getById(deliveryId),
    enabled: !isNaN(deliveryId),
  })

  const delivery: DeliveryDto | undefined = result?.data

  const deleteMutation = useMutation({
    mutationFn: () => deliveriesApi.delete(deliveryId),
    onSuccess: () => { toast.success('Livraison supprimée'); navigate('/deliveries', { replace: true }) },
    onError: () => toast.error('Impossible de supprimer cette livraison'),
  })

  if (isLoading) {
    return (
      <div className="space-y-4 p-6">
        <Skeleton className="h-36 rounded-2xl" />
        <div className="grid grid-cols-3 gap-3">
          {[1, 2, 3].map(i => <Skeleton key={i} className="h-16 rounded-xl" />)}
        </div>
        <div className="grid gap-4 lg:grid-cols-2">
          <Skeleton className="h-56 rounded-xl" />
          <Skeleton className="h-56 rounded-xl" />
        </div>
      </div>
    )
  }

  if (!delivery) {
    return (
      <div className="flex h-64 flex-col items-center justify-center gap-3 p-6">
        <p className="text-muted-foreground">Livraison introuvable</p>
        <Button variant="outline" asChild><Link to="/deliveries">Retour à la liste</Link></Button>
      </div>
    )
  }

  const hero = STATUS_HERO[delivery.status]
  const isCanceled = delivery.status === DeliveryStatus.Canceled
  const isDelivered = delivery.status === DeliveryStatus.Delivered
  const currentStep = STATUS_STEPS.indexOf(delivery.status)

  return (
    <div className="flex flex-col gap-5 p-6">

      {/* ── Hero banner ─────────────────────────────────────────────────────── */}
      <div className={cn('relative overflow-hidden rounded-2xl border p-6', hero.bg, hero.border)}>
        {/* Decorative accent bar */}
        <div className={cn('absolute left-0 top-0 h-full w-1.5', hero.accent)} />

        <div className="flex flex-col gap-4 pl-3 sm:flex-row sm:items-start sm:justify-between">
          <div className="flex items-start gap-3">
            <Button variant="ghost" size="icon" className="mt-0.5 shrink-0" onClick={() => navigate(-1)}>
              <ArrowLeft className="h-5 w-5" />
            </Button>
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <h1 className={cn('text-2xl font-bold', hero.text)}>{delivery.reference}</h1>
                <DeliveryStatusBadge status={delivery.status} />
                {delivery.type === DeliveryType.Urgent && (
                  <Badge variant="outline" className={cn('gap-1', URGENT_BADGE_CLASS)}>
                    <AlertTriangle className="h-3 w-3" />
                    Urgente
                  </Badge>
                )}
                {delivery.withAssembly && (
                  <Badge variant="outline" className="gap-1">
                    <Wrench className="h-3 w-3" />
                    Montage
                  </Badge>
                )}
              </div>
              <p className={cn('mt-1 text-sm', hero.text, 'opacity-70')}>
                #{delivery.sequentialNumber} · Créée le {formatDateTime(delivery.createdDate)} par {delivery.createdBy}
              </p>
            </div>
          </div>

          <div className="flex shrink-0 items-center gap-2 pl-10 sm:pl-0">
            {!isDelivered && (
              <Button size="sm" asChild>
                <Link to={`/deliveries/${delivery.id}/edit`}>
                  <Edit className="mr-1.5 h-4 w-4" />
                  Modifier
                </Link>
              </Button>
            )}
            {!isDelivered && (
              <>
                <div className="h-6 w-px bg-border" aria-hidden="true" />
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={() => deleteMutation.mutate()}
                  disabled={deleteMutation.isPending}
                  aria-label="Supprimer la livraison"
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </>
            )}
          </div>
        </div>

        {/* Status progress bar (hidden if canceled) */}
        {!isCanceled && (
          <div className="mt-5 flex items-center gap-0 pl-3">
            {STATUS_STEPS.map((step, i) => {
              const done = i <= currentStep
              const last = i === STATUS_STEPS.length - 1
              return (
                <div key={step} className="flex flex-1 items-center">
                  <div className="flex flex-col items-center gap-1">
                    <div className={cn(
                      'flex h-4 w-4 items-center justify-center rounded-full border-2 transition-all',
                      done
                        ? cn(hero.accent, 'border-transparent')
                        : 'border-muted-foreground/30 bg-background',
                    )}>
                      {done && <Check className="h-2.5 w-2.5 text-white" strokeWidth={3} />}
                    </div>
                    <span className={cn(
                      'text-center text-[9px] leading-tight sm:text-[10px]',
                      done ? hero.text : 'text-muted-foreground/50',
                    )}>
                      {STATUS_LABELS[step]}
                    </span>
                  </div>
                  {!last && (
                    <div className={cn(
                      'mb-3.5 h-0.5 flex-1',
                      i < currentStep ? hero.accent : 'bg-muted-foreground/20',
                    )} />
                  )}
                </div>
              )
            })}
          </div>
        )}
      </div>

      {/* ── Stat tiles ──────────────────────────────────────────────────────── */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <StatTile
          icon={Euro}
          label="Prix"
          value={formatPrice(delivery.price) ?? '—'}
          colorClass="bg-green-500"
        />
        <StatTile
          icon={Package}
          label="Colis"
          value={delivery.totalPackages > 0 ? String(delivery.totalPackages) : 'Aucun'}
          colorClass="bg-blue-500"
        />
        <StatTile
          icon={CalendarDays}
          label="Date de livraison"
          value={formatDate(delivery.scheduledDate) ?? 'Non définie'}
          colorClass="bg-amber-500"
        />
        <StatTile
          icon={Store}
          label="Dépôt"
          value={delivery.storeName}
          colorClass="bg-violet-500"
        />
      </div>

      {/* ── Body ────────────────────────────────────────────────────────────── */}
      <div className="grid gap-4 lg:grid-cols-2">

        {/* Left column */}
        <div className="flex flex-col gap-4">

          {/* Client card */}
          <Card className="overflow-hidden">
            <div className="flex items-center gap-4 bg-gradient-to-r from-primary/5 to-primary/10 p-5 pb-4">
              <div className="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-primary text-xl font-bold text-primary-foreground shadow-sm">
                {delivery.clientName.charAt(0).toUpperCase()}
              </div>
              <div>
                <p className="text-lg font-bold">{delivery.clientName}</p>
                <div className="mt-0.5 flex flex-col gap-0.5">
                  {delivery.clientPhone && (
                    <a href={`tel:${delivery.clientPhone}`} className="flex items-center gap-1.5 text-sm text-muted-foreground hover:text-primary">
                      <Phone className="h-3.5 w-3.5" />
                      {delivery.clientPhone}
                    </a>
                  )}
                  {delivery.clientEmail && (
                    <a href={`mailto:${delivery.clientEmail}`} className="flex items-center gap-1.5 text-sm text-muted-foreground hover:text-primary">
                      <Mail className="h-3.5 w-3.5" />
                      {delivery.clientEmail}
                    </a>
                  )}
                </div>
              </div>
            </div>
            <CardContent className="pt-4">
              <div className="flex items-start gap-3 rounded-lg bg-muted/40 p-3 text-sm">
                <MapPin className="mt-0.5 h-4 w-4 shrink-0 text-primary" />
                <div>
                  {delivery.addressLabel && (
                    <p className="mb-0.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                      {delivery.addressLabel}
                    </p>
                  )}
                  <p className="font-medium">{delivery.address}</p>
                  {delivery.addressComplement && <p className="text-muted-foreground">{delivery.addressComplement}</p>}
                  <p className="text-muted-foreground">{delivery.zipCode} {delivery.city}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Items card */}
          <Card>
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <CardTitle className="flex items-center gap-2 text-base">
                  <Package className="h-4 w-4 text-blue-500" />
                  Articles
                </CardTitle>
                {delivery.totalPackages > 0 && (
                  <Badge className="bg-blue-100 text-blue-700 hover:bg-blue-100 dark:bg-blue-500/15 dark:text-blue-400 dark:hover:bg-blue-500/15">
                    {delivery.totalPackages} colis
                  </Badge>
                )}
              </div>
            </CardHeader>
            <CardContent>
              {delivery.items.length === 0 ? (
                <div className="flex flex-col items-center gap-1 py-6 text-muted-foreground">
                  <Package className="h-8 w-8 opacity-20" />
                  <p className="text-sm">Aucun article</p>
                </div>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow className="hover:bg-transparent">
                      <TableHead className="py-2 text-xs">Désignation</TableHead>
                      <TableHead className="py-2 text-right text-xs">Qté</TableHead>
                      <TableHead className="py-2 text-xs">Référence</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {delivery.items.map(item => (
                      <TableRow key={item.id}>
                        <TableCell className="py-2">
                          <p className="font-medium">{item.designation}</p>
                          {item.information && <p className="text-xs text-muted-foreground">{item.information}</p>}
                        </TableCell>
                        <TableCell className="py-2 text-right">
                          <Badge variant="secondary" className="font-mono">{item.quantity}</Badge>
                        </TableCell>
                        <TableCell className="py-2 font-mono text-xs text-muted-foreground">
                          {item.reference ?? '—'}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Right column */}
        <div className="flex flex-col gap-4">

          {/* Delivery details */}
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="flex items-center gap-2 text-base">
                <FileText className="h-4 w-4 text-violet-500" />
                Détails
              </CardTitle>
            </CardHeader>
            <CardContent className="divide-y">
              <InfoRow icon={Store} label="Enseigne" value={delivery.storeName} iconClass="text-violet-500" />
              <InfoRow icon={FileText} label="N° dossier" value={delivery.fileNumber} />
              <InfoRow icon={CalendarDays} label="Date de livraison" value={formatDate(delivery.scheduledDate)} iconClass="text-amber-500" />
              <InfoRow icon={Clock} label="Durée estimée" value={delivery.estimatedDurationMinutes != null ? `${delivery.estimatedDurationMinutes} min` : null} iconClass="text-blue-500" />
              {delivery.timeSlot && (
                <InfoRow
                  icon={Clock}
                  label="Créneau d'intervention"
                  value={`${delivery.timeSlot.label} (${delivery.timeSlot.startTime}–${delivery.timeSlot.endTime})`}
                  iconClass="text-teal-500"
                />
              )}
              {delivery.type === DeliveryType.Urgent && delivery.urgentDriverName && (
                <InfoRow icon={AlertTriangle} label="Chauffeur urgent" value={delivery.urgentDriverName} iconClass="text-destructive" />
              )}
              {delivery.routeReference && (
                <InfoRow icon={Route} label="Tournée" value={delivery.routeReference} iconClass="text-green-600 dark:text-green-400" />
              )}
            </CardContent>
          </Card>

          {/* Financial card */}
          {(delivery.price > 0 || delivery.clientPaymentAmount != null || delivery.storePaymentAmount != null) && (
            <Card className="overflow-hidden">
              <CardHeader className="pb-2">
                <CardTitle className="flex items-center gap-2 text-base">
                  <Euro className="h-4 w-4 text-green-500" />
                  Financier
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-2.5">
                {delivery.price > 0 && (
                  <div className="flex items-center justify-between rounded-lg bg-green-50 px-3 py-2.5 dark:bg-green-950/30">
                    <div className="flex items-center gap-2 text-sm text-green-700 dark:text-green-300">
                      <Euro className="h-4 w-4" />
                      Prix total
                    </div>
                    <span className="font-bold text-green-700 dark:text-green-300">{formatPrice(delivery.price)}</span>
                  </div>
                )}
                {delivery.clientPaymentAmount != null && (
                  <div className="flex items-center justify-between rounded-lg bg-blue-50 px-3 py-2.5 dark:bg-blue-950/30">
                    <div className="flex items-center gap-2 text-sm text-blue-700 dark:text-blue-300">
                      <CreditCard className="h-4 w-4" />
                      Paiement client
                    </div>
                    <span className="font-semibold text-blue-700 dark:text-blue-300">{formatPrice(delivery.clientPaymentAmount)}</span>
                  </div>
                )}
                {delivery.storePaymentAmount != null && (
                  <div className="flex items-center justify-between rounded-lg bg-violet-50 px-3 py-2.5 dark:bg-violet-950/30">
                    <div className="flex items-center gap-2 text-sm text-violet-700 dark:text-violet-300">
                      <Building2 className="h-4 w-4" />
                      Paiement dépôt
                    </div>
                    <span className="font-semibold text-violet-700 dark:text-violet-300">{formatPrice(delivery.storePaymentAmount)}</span>
                  </div>
                )}
              </CardContent>
            </Card>
          )}

          {/* Notes */}
          {(delivery.deliveryNotes || delivery.internalNotes) && (
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="flex items-center gap-2 text-base">
                  <StickyNote className="h-4 w-4 text-amber-500" />
                  Notes
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                {delivery.deliveryNotes && (
                  <div>
                    <div className="mb-1.5 flex items-center gap-1.5">
                      <div className="h-2 w-2 rounded-full bg-blue-400" />
                      <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Note chauffeur</p>
                    </div>
                    <p className="rounded-lg border border-blue-100 bg-blue-50 p-3 text-sm dark:border-blue-900 dark:bg-blue-950/30">
                      {delivery.deliveryNotes}
                    </p>
                  </div>
                )}
                {delivery.internalNotes && (
                  <div>
                    <div className="mb-1.5 flex items-center gap-1.5">
                      <div className="h-2 w-2 rounded-full bg-amber-400" />
                      <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Note interne</p>
                    </div>
                    <p className="rounded-lg border border-amber-100 bg-amber-50 p-3 text-sm dark:border-amber-900 dark:bg-amber-950/30">
                      {delivery.internalNotes}
                    </p>
                  </div>
                )}
              </CardContent>
            </Card>
          )}

        </div>
      </div>
    </div>
  )
}
