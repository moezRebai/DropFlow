import { useState, useCallback } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import {
  Clock, Plus, RefreshCw, Pencil, Trash2, AlertTriangle,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { timeslotsApi, timeslotKeys } from '@/api/timeslots'
import type { TimeSlotDto } from '@/api/timeslots'

// ─── Schema ──────────────────────────────────────────────────────────────────

const schema = z.object({
  name: z.string().min(1, 'Nom requis'),
  startTime: z.string().min(1, 'Heure de début requise'),
  endTime: z.string().min(1, 'Heure de fin requise'),
  displayOrder: z.coerce.number().int().min(0),
}).refine(data => data.startTime < data.endTime, {
  message: 'L\'heure de fin doit être après l\'heure de début',
  path: ['endTime'],
})

type FormValues = z.infer<typeof schema>

// ─── Dialog state ─────────────────────────────────────────────────────────────

type DialogState =
  | { type: 'none' }
  | { type: 'create' }
  | { type: 'edit'; slot: TimeSlotDto }
  | { type: 'delete'; slot: TimeSlotDto }

// ─── StatChip ─────────────────────────────────────────────────────────────────

function StatChip({ icon, label }: { icon: React.ReactNode; label: string }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white">
      {icon}{label}
    </span>
  )
}

// ─── TimeSlotFormModal ────────────────────────────────────────────────────────

function TimeSlotFormModal({ slot, nextOrder, onClose }: {
  slot?: TimeSlotDto; nextOrder: number; onClose: () => void
}) {
  const qc = useQueryClient()
  const isEdit = !!slot

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: slot
      ? { name: slot.name, startTime: slot.startTime, endTime: slot.endTime, displayOrder: slot.displayOrder }
      : { name: '', startTime: '08:00', endTime: '12:00', displayOrder: nextOrder },
  })

  const createMutation = useMutation({
    mutationFn: timeslotsApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: timeslotKeys.all })
      toast.success('Créneau créé')
      onClose()
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur de création') : 'Erreur'),
  })

  const updateMutation = useMutation({
    mutationFn: (data: FormValues) => timeslotsApi.update(slot!.id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: timeslotKeys.all })
      toast.success('Créneau mis à jour')
      onClose()
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Erreur de mise à jour') : 'Erreur'),
  })

  function onSubmit(values: FormValues) {
    if (isEdit) {
      updateMutation.mutate(values)
    } else {
      createMutation.mutate({ name: values.name, startTime: values.startTime, endTime: values.endTime })
    }
  }

  const isPending = createMutation.isPending || updateMutation.isPending

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-card shadow-2xl">
        <div className="bg-gradient-to-br from-sky-500 to-blue-600 px-6 py-5">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/20">
              <Clock className="h-5 w-5 text-white" />
            </div>
            <div>
              <h2 className="font-bold text-white">{isEdit ? 'Modifier le créneau' : 'Nouveau créneau'}</h2>
              <p className="text-xs text-sky-200">{isEdit ? slot.name : 'Définir un créneau horaire'}</p>
            </div>
          </div>
        </div>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 p-6">
          <div className="space-y-1.5">
            <Label htmlFor="name">Nom du créneau *</Label>
            <Input id="name" {...form.register('name')} placeholder="Matin, Après-midi…" />
            {form.formState.errors.name && <p className="text-xs text-red-500">{form.formState.errors.name.message}</p>}
          </div>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label htmlFor="startTime">Début *</Label>
              <Input id="startTime" type="time" {...form.register('startTime')} />
              {form.formState.errors.startTime && <p className="text-xs text-red-500">{form.formState.errors.startTime.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="endTime">Fin *</Label>
              <Input id="endTime" type="time" {...form.register('endTime')} />
              {form.formState.errors.endTime && <p className="text-xs text-red-500">{form.formState.errors.endTime.message}</p>}
            </div>
          </div>
          {isEdit && (
            <div className="space-y-1.5">
              <Label htmlFor="displayOrder">Ordre d'affichage</Label>
              <Input id="displayOrder" type="number" min={0} {...form.register('displayOrder')} />
            </div>
          )}
          <div className="flex gap-3 pt-2">
            <Button type="button" variant="outline" className="flex-1" onClick={onClose}>Annuler</Button>
            <Button type="submit" className="flex-1" disabled={isPending}>
              {isPending
                ? <span className="flex items-center gap-2"><span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />{isEdit ? 'Mise à jour…' : 'Création…'}</span>
                : isEdit ? 'Enregistrer' : 'Créer'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ─── DeleteConfirmModal ────────────────────────────────────────────────────────

function DeleteConfirmModal({ slot, onConfirm, onCancel, isPending }: {
  slot: TimeSlotDto; onConfirm: () => void; onCancel: () => void; isPending: boolean
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative z-10 w-full max-w-sm overflow-hidden rounded-2xl bg-card shadow-2xl">
        <div className="p-6">
          <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100 dark:bg-red-500/15">
            <AlertTriangle className="h-6 w-6 text-red-600 dark:text-red-400" />
          </div>
          <h3 className="mb-1 text-base font-semibold text-foreground">Supprimer ce créneau ?</h3>
          <p className="text-sm text-muted-foreground">
            Le créneau <strong>«{slot.name}»</strong> sera définitivement supprimé.
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

// ─── TimeSlotsPage ────────────────────────────────────────────────────────────

export default function TimeSlotsPage() {
  const qc = useQueryClient()
  const [dialog, setDialog] = useState<DialogState>({ type: 'none' })

  const { data: slots = [], isLoading, refetch } = useQuery({
    queryKey: timeslotKeys.all,
    queryFn: timeslotsApi.getAll,
  })

  const sorted = [...slots].sort((a, b) => a.displayOrder - b.displayOrder || a.startTime.localeCompare(b.startTime))

  const deleteMutation = useMutation({
    mutationFn: (id: number) => timeslotsApi.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: timeslotKeys.all })
      toast.success('Créneau supprimé')
      setDialog({ type: 'none' })
    },
    onError: (err) => toast.error(isAxiosError(err) ? (err.response?.data?.message ?? 'Suppression impossible') : 'Erreur'),
  })

  const closeDialog = useCallback(() => setDialog({ type: 'none' }), [])

  const nextOrder = sorted.length > 0 ? Math.max(...sorted.map(s => s.displayOrder)) + 1 : 0

  function formatTime(t: string) {
    return t.substring(0, 5)
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
                <Clock className="h-5 w-5 text-white" />
              </div>
              <h1 className="text-2xl font-bold tracking-tight text-white">Créneaux horaires</h1>
            </div>
            <p className="text-sm text-sky-200">Définissez les créneaux d'intervention disponibles</p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            {!isLoading && (
              <StatChip icon={<Clock className="h-3.5 w-3.5" />} label={`${slots.length} créneau${slots.length > 1 ? 'x' : ''}`} />
            )}
            <button
              onClick={() => setDialog({ type: 'create' })}
              className="flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white backdrop-blur-sm transition-colors hover:bg-white/25"
            >
              <Plus className="h-3.5 w-3.5" />Nouveau créneau
            </button>
          </div>
        </div>
      </div>

      {/* Toolbar */}
      <div className="flex items-center justify-end">
        <Button variant="outline" size="icon" onClick={() => refetch()} title="Actualiser"><RefreshCw className="h-4 w-4" /></Button>
      </div>

      {/* Table */}
      <div className="overflow-hidden rounded-2xl border bg-card shadow-sm">
        <Table>
          <TableHeader>
            <TableRow className="bg-muted hover:bg-muted">
              <TableHead className="pl-6 text-xs font-semibold uppercase tracking-wider text-muted-foreground">Créneau</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Début</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Fin</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Durée</TableHead>
              <TableHead className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Ordre</TableHead>
              <TableHead className="w-20 pr-6" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              [...Array(4)].map((_, i) => (
                <TableRow key={i}>
                  <TableCell className="pl-6"><Skeleton className="h-6 w-32" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-16" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-16" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-16" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-8" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-12" /></TableCell>
                </TableRow>
              ))
            ) : sorted.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="py-16 text-center">
                  <div className="flex flex-col items-center gap-3">
                    <div className="flex h-12 w-12 items-center justify-center rounded-full bg-muted text-muted-foreground">
                      <Clock className="h-6 w-6" />
                    </div>
                    <p className="text-sm font-medium text-muted-foreground">Aucun créneau horaire défini</p>
                    <Button size="sm" onClick={() => setDialog({ type: 'create' })}>
                      <Plus className="mr-1.5 h-3.5 w-3.5" />Créer le premier créneau
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              sorted.map(slot => {
                const [sh, sm] = slot.startTime.split(':').map(Number)
                const [eh, em] = slot.endTime.split(':').map(Number)
                const durationMin = (eh * 60 + em) - (sh * 60 + sm)
                const durationLabel = durationMin >= 60
                  ? `${Math.floor(durationMin / 60)}h${durationMin % 60 > 0 ? String(durationMin % 60).padStart(2, '0') : ''}`
                  : `${durationMin}min`
                return (
                  <TableRow key={slot.id} className="hover:bg-sky-50/40 dark:hover:bg-sky-500/5">
                    <TableCell className="py-3 pl-6">
                      <div className="flex items-center gap-2">
                        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-sky-100 dark:bg-sky-500/15">
                          <Clock className="h-4 w-4 text-sky-600 dark:text-sky-400" />
                        </div>
                        <span className="font-semibold text-foreground">{slot.name}</span>
                      </div>
                    </TableCell>
                    <TableCell>
                      <span className="rounded-lg bg-muted px-2.5 py-1 font-mono text-sm font-medium text-foreground">
                        {formatTime(slot.startTime)}
                      </span>
                    </TableCell>
                    <TableCell>
                      <span className="rounded-lg bg-muted px-2.5 py-1 font-mono text-sm font-medium text-foreground">
                        {formatTime(slot.endTime)}
                      </span>
                    </TableCell>
                    <TableCell>
                      <span className="text-sm text-muted-foreground">{durationLabel}</span>
                    </TableCell>
                    <TableCell>
                      <span className="text-sm text-muted-foreground">#{slot.displayOrder}</span>
                    </TableCell>
                    <TableCell className="pr-6" onClick={e => e.stopPropagation()}>
                      <div className="flex items-center justify-end gap-1">
                        <button onClick={() => setDialog({ type: 'edit', slot })} aria-label="Modifier" className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground" title="Modifier">
                          <Pencil className="h-4 w-4" />
                        </button>
                        <button onClick={() => setDialog({ type: 'delete', slot })} aria-label="Supprimer" className="rounded-lg p-1.5 text-muted-foreground transition-colors hover:bg-red-50 hover:text-red-500 dark:hover:bg-red-500/10 dark:hover:text-red-400" title="Supprimer">
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

      {/* Modals */}
      {(dialog.type === 'create' || dialog.type === 'edit') && (
        <TimeSlotFormModal
          slot={dialog.type === 'edit' ? dialog.slot : undefined}
          nextOrder={nextOrder}
          onClose={closeDialog}
        />
      )}
      {dialog.type === 'delete' && (
        <DeleteConfirmModal
          slot={dialog.slot}
          onConfirm={() => deleteMutation.mutate(dialog.slot.id)}
          onCancel={closeDialog}
          isPending={deleteMutation.isPending}
        />
      )}
    </div>
  )
}
