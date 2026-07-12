import { useRef, useState, useImperativeHandle, forwardRef, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import {
  Truck, MapPin, Clock, Phone, Package, Wrench, CheckCircle2,
  PlayCircle, Flag, RefreshCw, ChevronRight, X, Navigation,
  Camera, Eraser, UserX, PenLine, Calendar, ClipboardList, CreditCard,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import { useAuthStore } from '@/store/authStore'
import {
  driverApi, driverKeys, ROUTE_STATUS,
} from '@/api/driver'
import type { DriverDeliveryListDto } from '@/api/driver'
import { DeliveryStatus, STATUS_LABELS, STATUS_COLORS } from '@/api/deliveries'

// ─── Helpers ────────────────────────────────────────────────────────────────

function errMsg(err: unknown, fallback: string) {
  if (isAxiosError(err)) return err.response?.data?.message ?? err.response?.data?.errors?.[0] ?? fallback
  return fallback
}

function formatTime(t?: string | null) {
  if (!t) return null
  return t.substring(0, 5)
}

function formatDateFr(iso: string) {
  return new Date(iso).toLocaleDateString('fr-FR', { weekday: 'long', day: 'numeric', month: 'long' })
}

function stripDataUrl(dataUrl: string) {
  const i = dataUrl.indexOf(',')
  return i >= 0 ? dataUrl.substring(i + 1) : dataUrl
}

function mapsUrl(destination: string) {
  return `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(destination)}`
}

function DeliveryStatusBadge({ status, label }: { status: number; label: string }) {
  const color = STATUS_COLORS[status as DeliveryStatus] ?? 'bg-muted text-muted-foreground border-border'
  return (
    <span className={cn('inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium', color)}>
      {label || STATUS_LABELS[status as DeliveryStatus] || status}
    </span>
  )
}

// ─── Signature pad ──────────────────────────────────────────────────────────

interface SignaturePadHandle {
  toDataURL: () => string
  clear: () => void
  isEmpty: () => boolean
}

const SignaturePad = forwardRef<SignaturePadHandle>((_props, ref) => {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const drawing = useRef(false)
  const dirty = useRef(false)
  const last = useRef<{ x: number; y: number } | null>(null)
  const [empty, setEmpty] = useState(true)

  useImperativeHandle(ref, () => ({
    toDataURL: () => canvasRef.current?.toDataURL('image/png') ?? '',
    clear: () => {
      const c = canvasRef.current
      if (!c) return
      const ctx = c.getContext('2d')
      ctx?.clearRect(0, 0, c.width, c.height)
      dirty.current = false
      setEmpty(true)
    },
    isEmpty: () => !dirty.current,
  }))

  // Size the canvas backing store to its rendered size (for crisp lines).
  useEffect(() => {
    const c = canvasRef.current
    if (!c) return
    const rect = c.getBoundingClientRect()
    c.width = rect.width
    c.height = rect.height
    const ctx = c.getContext('2d')
    if (ctx) {
      ctx.lineWidth = 2.2
      ctx.lineCap = 'round'
      ctx.lineJoin = 'round'
      ctx.strokeStyle = '#0f172a'
    }
  }, [])

  function pos(e: React.PointerEvent) {
    const c = canvasRef.current!
    const rect = c.getBoundingClientRect()
    return { x: e.clientX - rect.left, y: e.clientY - rect.top }
  }

  function down(e: React.PointerEvent) {
    e.preventDefault()
    drawing.current = true
    last.current = pos(e)
    canvasRef.current?.setPointerCapture(e.pointerId)
  }
  function move(e: React.PointerEvent) {
    if (!drawing.current) return
    const ctx = canvasRef.current?.getContext('2d')
    if (!ctx || !last.current) return
    const p = pos(e)
    ctx.beginPath()
    ctx.moveTo(last.current.x, last.current.y)
    ctx.lineTo(p.x, p.y)
    ctx.stroke()
    last.current = p
    dirty.current = true
    if (empty) setEmpty(false)
  }
  function up() {
    drawing.current = false
    last.current = null
  }

  return (
    <div className="relative">
      <canvas
        ref={canvasRef}
        onPointerDown={down}
        onPointerMove={move}
        onPointerUp={up}
        onPointerLeave={up}
        className="h-40 w-full touch-none rounded-xl border-2 border-dashed border-slate-300 bg-white"
      />
      {empty && (
        <p className="pointer-events-none absolute inset-0 flex items-center justify-center text-sm text-slate-300">
          Signez ici
        </p>
      )}
    </div>
  )
})
SignaturePad.displayName = 'SignaturePad'

// ─── Validate modal ─────────────────────────────────────────────────────────

function ValidateModal({ delivery, onClose }: {
  delivery: { id: number; reference: string; clientName: string }
  onClose: () => void
}) {
  const qc = useQueryClient()
  const sigRef = useRef<SignaturePadHandle>(null)
  const [isAbsent, setIsAbsent] = useState(false)
  const [comment, setComment] = useState('')
  const [photo, setPhoto] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () => {
      const signature = !isAbsent ? sigRef.current?.toDataURL() : null
      return driverApi.validateDelivery(delivery.id, {
        signatureBase64: signature ? stripDataUrl(signature) : null,
        photoBase64: photo ? stripDataUrl(photo) : null,
        comment: comment.trim() || null,
        isClientAbsent: isAbsent,
      })
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: driverKeys.all })
      toast.success('Livraison validée')
      onClose()
    },
    onError: (e) => toast.error(errMsg(e, 'Validation impossible')),
  })

  function handleSubmit() {
    if (!isAbsent && sigRef.current?.isEmpty()) {
      toast.error('Signature requise (ou cochez « client absent »)')
      return
    }
    mutation.mutate()
  }

  function handlePhoto(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    const reader = new FileReader()
    reader.onload = () => setPhoto(reader.result as string)
    reader.readAsDataURL(file)
  }

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center sm:items-center sm:p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
      <div className="relative z-10 flex max-h-[92vh] w-full flex-col overflow-hidden rounded-t-2xl bg-card shadow-2xl sm:max-w-md sm:rounded-2xl">
        <div className="flex items-center justify-between bg-gradient-to-br from-sky-500 to-blue-600 px-6 py-4">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
              <CheckCircle2 className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="font-bold text-white">Valider la livraison</h2>
              <p className="text-xs text-sky-200">{delivery.reference} · {delivery.clientName}</p>
            </div>
          </div>
          <button onClick={onClose} aria-label="Fermer" className="rounded-lg p-1.5 text-white/80 hover:bg-white/15 hover:text-white">
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="flex-1 space-y-4 overflow-y-auto p-6">
          {/* Client absent */}
          <label className="flex cursor-pointer items-center gap-3 rounded-xl border p-3 transition-colors hover:bg-muted">
            <Checkbox checked={isAbsent} onCheckedChange={c => setIsAbsent(c === true)} />
            <span className="flex items-center gap-1.5 text-sm font-medium text-foreground">
              <UserX className="h-4 w-4 text-muted-foreground" />Client absent (sans signature)
            </span>
          </label>

          {/* Signature */}
          {!isAbsent && (
            <div className="space-y-1.5">
              <div className="flex items-center justify-between">
                <Label className="flex items-center gap-1.5"><PenLine className="h-3.5 w-3.5" />Signature du client *</Label>
                <button
                  onClick={() => sigRef.current?.clear()}
                  className="-mr-2 flex min-h-11 items-center gap-1 px-2 text-xs font-medium text-muted-foreground hover:text-foreground"
                >
                  <Eraser className="h-3.5 w-3.5" />Effacer
                </button>
              </div>
              <SignaturePad ref={sigRef} />
            </div>
          )}

          {/* Photo */}
          <div className="space-y-1.5">
            <Label className="flex items-center gap-1.5"><Camera className="h-3.5 w-3.5" />Photo (optionnel)</Label>
            {photo ? (
              <div className="relative">
                <img src={photo} alt="preuve" className="h-40 w-full rounded-xl border object-cover" />
                <button onClick={() => setPhoto(null)} aria-label="Retirer la photo" className="absolute right-2 top-2 rounded-full bg-black/60 p-1 text-white hover:bg-black/80">
                  <X className="h-4 w-4" />
                </button>
              </div>
            ) : (
              <label className="flex h-24 cursor-pointer flex-col items-center justify-center gap-1.5 rounded-xl border-2 border-dashed border-slate-300 text-muted-foreground transition-colors hover:border-sky-400 hover:text-sky-500 dark:border-slate-700">
                <Camera className="h-6 w-6" />
                <span className="text-xs font-medium">Prendre / choisir une photo</span>
                <input type="file" accept="image/*" capture="environment" onChange={handlePhoto} className="hidden" />
              </label>
            )}
          </div>

          {/* Comment */}
          <div className="space-y-1.5">
            <Label htmlFor="comment">Commentaire (optionnel)</Label>
            <textarea
              id="comment"
              value={comment}
              onChange={e => setComment(e.target.value)}
              rows={2}
              placeholder="Remarque sur la livraison…"
              className="w-full rounded-xl border bg-background p-3 text-sm focus:border-sky-400 focus:outline-none focus:ring-1 focus:ring-sky-400"
            />
          </div>
        </div>

        <div className="flex gap-3 border-t p-4">
          <Button variant="outline" className="flex-1" onClick={onClose}>Annuler</Button>
          <Button className="flex-1" onClick={handleSubmit} disabled={mutation.isPending}>
            {mutation.isPending
              ? <span className="flex items-center gap-2"><span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />Validation…</span>
              : 'Confirmer la livraison'}
          </Button>
        </div>
      </div>
    </div>
  )
}

// ─── Delivery detail modal ──────────────────────────────────────────────────

function DetailModal({ id, onClose, onValidate }: {
  id: number
  onClose: () => void
  onValidate: (d: { id: number; reference: string; clientName: string }) => void
}) {
  const { data: d, isLoading } = useQuery({
    queryKey: driverKeys.delivery(id),
    queryFn: () => driverApi.getDelivery(id),
  })

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center sm:items-center sm:p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
      <div className="relative z-10 flex max-h-[92vh] w-full flex-col overflow-hidden rounded-t-2xl bg-card shadow-2xl sm:max-w-lg sm:rounded-2xl">
        <div className="flex items-center justify-between border-b px-6 py-4">
          <div className="flex items-center gap-2">
            <ClipboardList className="h-5 w-5 text-muted-foreground" />
            <h2 className="font-semibold text-foreground">Détail de la livraison</h2>
          </div>
          <button onClick={onClose} aria-label="Fermer" className="rounded-lg p-1.5 text-muted-foreground hover:bg-muted hover:text-foreground">
            <X className="h-5 w-5" />
          </button>
        </div>

        {isLoading || !d ? (
          <div className="space-y-3 p-6">
            <Skeleton className="h-6 w-40" />
            <Skeleton className="h-24 w-full rounded-xl" />
            <Skeleton className="h-16 w-full rounded-xl" />
          </div>
        ) : (
          <>
            <div className="flex-1 space-y-4 overflow-y-auto p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-lg font-bold text-foreground">{d.clientName}</p>
                  <p className="text-sm text-muted-foreground">{d.reference} · Arrêt #{d.sequenceOrder}</p>
                </div>
                <DeliveryStatusBadge status={d.status} label={d.statusDisplay} />
              </div>

              {/* Address + actions */}
              <div className="rounded-xl border bg-muted/50 p-4">
                <p className="flex items-start gap-2 text-sm text-foreground">
                  <MapPin className="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
                  <span>{d.fullAddress}{d.addressComplement ? ` (${d.addressComplement})` : ''}</span>
                </p>
                <div className="mt-3 flex flex-wrap gap-2">
                  <a href={mapsUrl(d.fullAddress)} target="_blank" rel="noreferrer"
                     className="flex items-center gap-1.5 rounded-lg bg-sky-600 px-3 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-sky-700">
                    <Navigation className="h-3.5 w-3.5" />Itinéraire
                  </a>
                  {d.clientPhone && (
                    <a href={`tel:${d.clientPhone}`}
                       className="flex items-center gap-1.5 rounded-lg border px-3 py-1.5 text-xs font-semibold text-foreground transition-colors hover:bg-muted">
                      <Phone className="h-3.5 w-3.5" />{d.clientPhone}
                    </a>
                  )}
                </div>
              </div>

              {/* Meta */}
              <div className="grid grid-cols-2 gap-3">
                {formatTime(d.estimatedArrivalTime) && (
                  <div className="flex items-center gap-2 rounded-xl border p-3">
                    <Clock className="h-4 w-4 text-muted-foreground" />
                    <div><p className="text-xs text-muted-foreground">Arrivée estimée</p><p className="text-sm font-semibold text-foreground">{formatTime(d.estimatedArrivalTime)}</p></div>
                  </div>
                )}
                {d.timeSlotName && (
                  <div className="flex items-center gap-2 rounded-xl border p-3">
                    <Calendar className="h-4 w-4 text-muted-foreground" />
                    <div><p className="text-xs text-muted-foreground">Créneau</p><p className="text-sm font-semibold text-foreground">{d.timeSlotName}</p></div>
                  </div>
                )}
                <div className="flex items-center gap-2 rounded-xl border p-3">
                  <Package className="h-4 w-4 text-muted-foreground" />
                  <div><p className="text-xs text-muted-foreground">Colis</p><p className="text-sm font-semibold text-foreground">{d.totalPackages}</p></div>
                </div>
                {d.withAssembly && (
                  <div className="flex items-center gap-2 rounded-xl border border-amber-200 bg-amber-50 p-3 dark:border-amber-500/30 dark:bg-amber-500/10">
                    <Wrench className="h-4 w-4 text-amber-500 dark:text-amber-400" />
                    <div><p className="text-xs text-amber-500 dark:text-amber-400">Service</p><p className="text-sm font-semibold text-amber-700 dark:text-amber-300">Montage</p></div>
                  </div>
                )}
                {d.clientPaymentAmount != null && d.clientPaymentAmount > 0 && (
                  <div className="flex items-center gap-2 rounded-xl border border-emerald-200 bg-emerald-50 p-3 dark:border-emerald-500/30 dark:bg-emerald-500/10">
                    <CreditCard className="h-4 w-4 text-emerald-500 dark:text-emerald-400" />
                    <div><p className="text-xs text-emerald-500 dark:text-emerald-400">À encaisser</p><p className="text-sm font-semibold text-emerald-700 dark:text-emerald-300">{d.clientPaymentAmount.toLocaleString('fr-FR', { style: 'currency', currency: 'EUR' })}</p></div>
                  </div>
                )}
              </div>

              {/* Items */}
              {d.items.length > 0 && (
                <div>
                  <p className="mb-1.5 text-xs font-medium uppercase tracking-wide text-muted-foreground">Articles</p>
                  <div className="divide-y rounded-xl border">
                    {d.items.map((it, i) => (
                      <div key={i} className="flex items-center justify-between px-3 py-2">
                        <div className="min-w-0">
                          <p className="truncate text-sm font-medium text-foreground">{it.designation}</p>
                          {it.reference && <p className="text-xs text-muted-foreground">{it.reference}</p>}
                        </div>
                        <span className="shrink-0 rounded-full bg-muted px-2 py-0.5 text-xs font-semibold text-muted-foreground">×{it.quantity}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {d.deliveryNotes && (
                <div className="rounded-xl border border-blue-200 bg-blue-50 p-3 text-sm text-blue-800 dark:border-blue-500/30 dark:bg-blue-500/10 dark:text-blue-300">
                  {d.deliveryNotes}
                </div>
              )}

              {d.isValidated && (
                <div className="flex items-center gap-2 rounded-xl border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-700 dark:border-emerald-500/30 dark:bg-emerald-500/10 dark:text-emerald-300">
                  <CheckCircle2 className="h-4 w-4" />
                  Validée{d.deliveredDateTime ? ` le ${new Date(d.deliveredDateTime).toLocaleString('fr-FR')}` : ''}
                  {d.isClientAbsent ? ' · client absent' : ''}
                </div>
              )}
            </div>

            {!d.isValidated && (
              <div className="border-t p-4">
                <Button className="w-full" onClick={() => onValidate({ id: d.id, reference: d.reference, clientName: d.clientName })}>
                  <CheckCircle2 className="mr-1.5 h-4 w-4" />Valider la livraison
                </Button>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}

// ─── Delivery card ──────────────────────────────────────────────────────────

function DeliveryCard({ d, onOpen, onValidate }: {
  d: DriverDeliveryListDto
  onOpen: () => void
  onValidate: () => void
}) {
  return (
    <div className={cn('rounded-2xl border bg-card p-4 shadow-sm transition-all', d.isValidated && 'opacity-70')}>
      <div className="flex items-start gap-3">
        <div className={cn(
          'flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-sm font-bold text-white',
          d.isValidated ? 'bg-emerald-500' : 'bg-gradient-to-br from-sky-500 to-blue-600',
        )}>
          {d.isValidated ? <CheckCircle2 className="h-5 w-5" /> : d.sequenceOrder}
        </div>
        <button onClick={onOpen} className="min-w-0 flex-1 text-left">
          <div className="flex items-center gap-2">
            <p className="truncate font-semibold text-foreground">{d.clientName}</p>
            {d.isClientAbsent && <span className="rounded-full bg-red-100 px-1.5 py-0.5 text-xs font-medium text-red-600 dark:bg-red-500/15 dark:text-red-400">absent</span>}
          </div>
          <p className="flex items-center gap-1 text-xs text-muted-foreground">
            <MapPin className="h-3 w-3" />{d.zipCode} {d.city}
          </p>
          <div className="mt-1.5 flex flex-wrap items-center gap-1.5">
            <DeliveryStatusBadge status={d.status} label={d.statusDisplay} />
            {formatTime(d.estimatedArrivalTime) && (
              <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground">
                <Clock className="h-3 w-3" />{formatTime(d.estimatedArrivalTime)}
              </span>
            )}
            {d.timeSlotName && <span className="rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground">{d.timeSlotName}</span>}
            <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground">
              <Package className="h-3 w-3" />{d.totalPackages}
            </span>
            {d.withAssembly && (
              <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-700 dark:bg-amber-500/15 dark:text-amber-400">
                <Wrench className="h-3 w-3" />Montage
              </span>
            )}
          </div>
        </button>
        <ChevronRight className="mt-1 h-4 w-4 shrink-0 text-muted-foreground/50" />
      </div>
      {!d.isValidated && (
        <button
          onClick={onValidate}
          className="mt-3 flex w-full items-center justify-center gap-1.5 rounded-xl bg-sky-50 py-2 text-sm font-semibold text-sky-600 transition-colors hover:bg-sky-100 dark:bg-sky-500/10 dark:text-sky-400 dark:hover:bg-sky-500/20"
        >
          <CheckCircle2 className="h-4 w-4" />Valider
        </button>
      )}
    </div>
  )
}

// ─── Page ───────────────────────────────────────────────────────────────────

type Validate = { id: number; reference: string; clientName: string }

export default function PersonalDeliveriesPage() {
  const user = useAuthStore(s => s.user)
  const qc = useQueryClient()
  const [detailId, setDetailId] = useState<number | null>(null)
  const [validate, setValidate] = useState<Validate | null>(null)

  const { data, isLoading, refetch, isFetching } = useQuery({
    queryKey: driverKeys.dashboard(),
    queryFn: driverApi.getDashboard,
  })

  const today = data?.todayRoute
  const route = today?.route
  const upcoming = data?.upcomingRoutes ?? []

  function invalidate() {
    qc.invalidateQueries({ queryKey: driverKeys.all })
  }

  const startMutation = useMutation({
    mutationFn: (id: number) => driverApi.startRoute(id),
    onSuccess: () => { invalidate(); toast.success('Tournée démarrée') },
    onError: (e) => toast.error(errMsg(e, 'Démarrage impossible')),
  })
  const completeMutation = useMutation({
    mutationFn: (id: number) => driverApi.completeRoute(id),
    onSuccess: () => { invalidate(); toast.success('Tournée terminée') },
    onError: (e) => toast.error(errMsg(e, 'Clôture impossible')),
  })

  const validatedCount = route?.deliveries.filter(d => d.isValidated).length ?? 0

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-6 p-4 sm:p-6">

      {/* Hero */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 p-6 shadow-lg">
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }} />
        <div className="relative flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-white">
              Bonjour, {user?.firstName ?? 'Chauffeur'}
            </h1>
            <p className="text-sm capitalize text-sky-200">{formatDateFr(new Date().toISOString())}</p>
          </div>
          <button onClick={() => refetch()} aria-label="Actualiser" className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/15 text-white transition-colors hover:bg-white/25" title="Actualiser">
            <RefreshCw className={cn('h-5 w-5', isFetching && 'animate-spin')} />
          </button>
        </div>
      </div>

      {isLoading ? (
        <div className="flex flex-col gap-4">
          <Skeleton className="h-32 rounded-2xl" />
          {[...Array(3)].map((_, i) => <Skeleton key={i} className="h-28 rounded-2xl" />)}
        </div>
      ) : !route ? (
        /* No route today */
        <div className="flex flex-col items-center gap-3 rounded-2xl border border-dashed border-border bg-card py-16 text-center">
          <div className="flex h-14 w-14 items-center justify-center rounded-full bg-muted text-muted-foreground">
            <Truck className="h-7 w-7" />
          </div>
          <p className="font-medium text-foreground">Aucune tournée prévue aujourd'hui</p>
          <p className="text-sm text-muted-foreground">{today?.message ?? 'Profitez de votre journée !'}</p>
        </div>
      ) : (
        <>
          {/* Route summary */}
          <div className="rounded-2xl border bg-card p-5 shadow-sm">
            <div className="flex items-start justify-between">
              <div>
                <div className="flex items-center gap-2">
                  <Truck className="h-5 w-5 text-sky-600 dark:text-sky-400" />
                  <h2 className="text-lg font-bold text-foreground">{route.reference}</h2>
                </div>
                <p className="mt-0.5 text-sm text-muted-foreground">{route.vehicleName}</p>
              </div>
              <span className="rounded-full bg-sky-100 px-2.5 py-0.5 text-xs font-semibold text-sky-700 dark:bg-sky-500/15 dark:text-sky-400">{route.statusDisplay}</span>
            </div>

            <div className="mt-4 grid grid-cols-3 gap-3 text-center">
              <div className="rounded-xl bg-muted p-3">
                <p className="text-lg font-extrabold text-foreground">{validatedCount}/{route.totalDeliveries}</p>
                <p className="text-xs text-muted-foreground">Livrées</p>
              </div>
              <div className="rounded-xl bg-muted p-3">
                <p className="text-lg font-extrabold text-foreground">{route.totalDistanceKm.toFixed(0)} km</p>
                <p className="text-xs text-muted-foreground">Distance</p>
              </div>
              <div className="rounded-xl bg-muted p-3">
                <p className="text-lg font-extrabold text-foreground">{formatTime(route.startTime) ?? '—'}</p>
                <p className="text-xs text-muted-foreground">Départ</p>
              </div>
            </div>

            {route.status === ROUTE_STATUS.Confirmed && (
              <Button className="mt-4 w-full" onClick={() => startMutation.mutate(route.routeId)} disabled={startMutation.isPending}>
                <PlayCircle className="mr-1.5 h-4 w-4" />Démarrer la tournée
              </Button>
            )}
            {route.status === ROUTE_STATUS.InProgress && (
              <Button className="mt-4 w-full" variant="outline" onClick={() => completeMutation.mutate(route.routeId)} disabled={completeMutation.isPending}>
                <Flag className="mr-1.5 h-4 w-4" />Terminer la tournée
              </Button>
            )}
          </div>

          {/* Deliveries */}
          <div className="flex flex-col gap-3">
            <div className="flex items-center gap-2 px-1">
              <MapPin className="h-4 w-4 text-muted-foreground" />
              <h3 className="font-semibold text-foreground">Mes arrêts</h3>
              <span className="rounded-full bg-muted px-2 py-0.5 text-xs font-semibold text-muted-foreground">{route.deliveries.length}</span>
            </div>
            {route.deliveries.map(d => (
              <DeliveryCard
                key={d.id}
                d={d}
                onOpen={() => setDetailId(d.id)}
                onValidate={() => setValidate({ id: d.id, reference: d.reference, clientName: d.clientName })}
              />
            ))}
          </div>
        </>
      )}

      {/* Upcoming routes */}
      {!isLoading && upcoming.length > 0 && (
        <div className="flex flex-col gap-3">
          <div className="flex items-center gap-2 px-1">
            <Calendar className="h-4 w-4 text-muted-foreground" />
            <h3 className="font-semibold text-foreground">Tournées à venir</h3>
          </div>
          {upcoming.map(r => (
            <div key={r.routeId} className="flex items-center gap-3 rounded-2xl border bg-card p-4 shadow-sm">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-muted text-muted-foreground">
                <Truck className="h-5 w-5" />
              </div>
              <div className="min-w-0 flex-1">
                <p className="font-semibold text-foreground">{r.reference}</p>
                <p className="text-xs text-muted-foreground">
                  {new Date(r.date).toLocaleDateString('fr-FR', { weekday: 'short', day: 'numeric', month: 'short' })} · {r.vehicleName} · {r.totalDeliveries} arrêts
                </p>
              </div>
              <span className="shrink-0 rounded-full bg-muted px-2.5 py-0.5 text-xs font-medium text-muted-foreground">{r.statusDisplay}</span>
            </div>
          ))}
        </div>
      )}

      {detailId != null && (
        <DetailModal
          id={detailId}
          onClose={() => setDetailId(null)}
          onValidate={(v) => { setDetailId(null); setValidate(v) }}
        />
      )}
      {validate && <ValidateModal delivery={validate} onClose={() => setValidate(null)} />}
    </div>
  )
}
