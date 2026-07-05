import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import { X, MapPin, User } from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Separator } from '@/components/ui/separator'
import { clientsApi, clientKeys } from '@/api/clients'
import type { ClientDto } from '@/api/clients'

// ─── Schemas ──────────────────────────────────────────────────────────────────

const createSchema = z.object({
  firstName: z.string().min(1, 'Prénom requis'),
  lastName: z.string().min(1, 'Nom requis'),
  phone: z.string().min(6, 'Téléphone requis'),
  email: z.string().email('Email invalide').optional().or(z.literal('')),
  addressLabel: z.string().optional(),
  address: z.string().min(2, 'Adresse requise'),
  zipCode: z.string().min(4, 'Code postal requis'),
  city: z.string().min(1, 'Ville requise'),
  complement: z.string().optional(),
})

const editSchema = z.object({
  firstName: z.string().min(1, 'Prénom requis'),
  lastName: z.string().min(1, 'Nom requis'),
  phone: z.string().min(6, 'Téléphone requis'),
  email: z.string().email('Email invalide').optional().or(z.literal('')),
  isActive: z.boolean(),
})

type CreateData = z.infer<typeof createSchema>
type EditData = z.infer<typeof editSchema>

// ─── Component ────────────────────────────────────────────────────────────────

interface ClientFormDialogProps {
  open: boolean
  onClose: () => void
  client?: ClientDto
}

export function ClientFormDialog({ open, onClose, client }: ClientFormDialogProps) {
  const isEdit = !!client
  const qc = useQueryClient()

  // ── Create form
  const createForm = useForm<CreateData>({
    resolver: zodResolver(createSchema),
    defaultValues: {
      firstName: '', lastName: '', phone: '', email: '',
      addressLabel: 'Principal', address: '', zipCode: '', city: '', complement: '',
    },
  })

  // ── Edit form
  const editForm = useForm<EditData>({
    resolver: zodResolver(editSchema),
    defaultValues: { firstName: '', lastName: '', phone: '', email: '', isActive: true },
  })

  useEffect(() => {
    if (!open) return
    if (isEdit && client) {
      editForm.reset({
        firstName: client.firstName,
        lastName: client.lastName,
        phone: client.phone,
        email: client.email ?? '',
        isActive: client.isActive,
      })
    } else {
      createForm.reset({
        firstName: '', lastName: '', phone: '', email: '',
        addressLabel: 'Principal', address: '', zipCode: '', city: '', complement: '',
      })
    }
  }, [open, client, isEdit, createForm, editForm])

  // ── Mutations
  const createMutation = useMutation({
    mutationFn: clientsApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: clientKeys.lists() })
      toast.success('Client créé avec succès')
      onClose()
    },
    onError: (err) => {
      const msg = isAxiosError(err) ? (err.response?.data?.message ?? 'Création impossible') : 'Une erreur est survenue'
      toast.error(msg)
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: Parameters<typeof clientsApi.update>[1] }) =>
      clientsApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: clientKeys.lists() })
      qc.invalidateQueries({ queryKey: clientKeys.detail(client!.id) })
      toast.success('Client mis à jour')
      onClose()
    },
    onError: (err) => {
      const msg = isAxiosError(err) ? (err.response?.data?.message ?? 'Mise à jour impossible') : 'Une erreur est survenue'
      toast.error(msg)
    },
  })

  // ── Submit handlers
  function onCreateSubmit(data: CreateData) {
    createMutation.mutate({
      firstName: data.firstName,
      lastName: data.lastName,
      phone: data.phone,
      email: data.email || undefined,
      address: {
        label: data.addressLabel || 'Principal',
        address: data.address,
        zipCode: data.zipCode,
        city: data.city,
        complement: data.complement || undefined,
      },
    })
  }

  function onEditSubmit(data: EditData) {
    if (!client) return
    updateMutation.mutate({
      id: client.id,
      data: {
        firstName: data.firstName,
        lastName: data.lastName,
        phone: data.phone,
        email: data.email || undefined,
        isActive: data.isActive,
      },
    })
  }

  const isPending = createMutation.isPending || updateMutation.isPending

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
      <div className="relative z-10 w-full max-w-lg overflow-hidden rounded-2xl bg-white shadow-2xl">

        {/* Header */}
        <div className="flex items-center justify-between border-b bg-gradient-to-r from-sky-50 to-blue-50 px-6 py-4">
          <div>
            <h2 className="font-semibold text-slate-800">
              {isEdit ? 'Modifier le client' : 'Nouveau client'}
            </h2>
            <p className="text-xs text-slate-500">
              {isEdit ? `Mise à jour de ${client.displayName}` : 'Remplissez les informations ci-dessous'}
            </p>
          </div>
          <button
            onClick={onClose}
            className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-white hover:text-slate-600"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        {/* Body */}
        <div className="max-h-[80vh] overflow-y-auto">
          {isEdit ? (
            <form onSubmit={editForm.handleSubmit(onEditSubmit)} className="space-y-5 p-6">
              <Section icon={<User className="h-4 w-4" />} title="Informations">
                <div className="grid grid-cols-2 gap-3">
                  <Field label="Prénom" error={editForm.formState.errors.firstName?.message}>
                    <Input placeholder="Jean" {...editForm.register('firstName')} />
                  </Field>
                  <Field label="Nom" error={editForm.formState.errors.lastName?.message}>
                    <Input placeholder="Dupont" {...editForm.register('lastName')} />
                  </Field>
                </div>
                <Field label="Téléphone" error={editForm.formState.errors.phone?.message}>
                  <Input type="tel" placeholder="06 00 00 00 00" {...editForm.register('phone')} />
                </Field>
                <Field label="Email" error={editForm.formState.errors.email?.message}>
                  <Input type="email" placeholder="jean@exemple.com" {...editForm.register('email')} />
                </Field>
                <div className="flex items-center gap-3 rounded-xl border bg-slate-50 px-4 py-3">
                  <input
                    type="checkbox"
                    id="isActive"
                    className="h-4 w-4 rounded border-slate-300 accent-sky-600"
                    {...editForm.register('isActive')}
                  />
                  <label htmlFor="isActive" className="cursor-pointer text-sm font-medium text-slate-700">
                    Client actif
                  </label>
                </div>
              </Section>

              <FormActions onClose={onClose} isPending={isPending} submitLabel="Enregistrer" />
            </form>
          ) : (
            <form onSubmit={createForm.handleSubmit(onCreateSubmit)} className="space-y-5 p-6">
              <Section icon={<User className="h-4 w-4" />} title="Informations client">
                <div className="grid grid-cols-2 gap-3">
                  <Field label="Prénom" error={createForm.formState.errors.firstName?.message}>
                    <Input placeholder="Jean" autoFocus {...createForm.register('firstName')} />
                  </Field>
                  <Field label="Nom" error={createForm.formState.errors.lastName?.message}>
                    <Input placeholder="Dupont" {...createForm.register('lastName')} />
                  </Field>
                </div>
                <Field label="Téléphone" error={createForm.formState.errors.phone?.message}>
                  <Input type="tel" placeholder="06 00 00 00 00" {...createForm.register('phone')} />
                </Field>
                <Field label="Email" error={createForm.formState.errors.email?.message}>
                  <Input type="email" placeholder="jean@exemple.com" {...createForm.register('email')} />
                </Field>
              </Section>

              <Separator />

              <Section icon={<MapPin className="h-4 w-4" />} title="Adresse principale">
                <Field label="Libellé">
                  <Input placeholder="Principal" {...createForm.register('addressLabel')} />
                </Field>
                <Field label="Adresse" error={createForm.formState.errors.address?.message}>
                  <Input placeholder="123 Rue de la Paix" {...createForm.register('address')} />
                </Field>
                <div className="grid grid-cols-2 gap-3">
                  <Field label="Code postal" error={createForm.formState.errors.zipCode?.message}>
                    <Input placeholder="75001" {...createForm.register('zipCode')} />
                  </Field>
                  <Field label="Ville" error={createForm.formState.errors.city?.message}>
                    <Input placeholder="Paris" {...createForm.register('city')} />
                  </Field>
                </div>
                <Field label="Complément">
                  <Input placeholder="Bâtiment B, 2ème étage…" {...createForm.register('complement')} />
                </Field>
              </Section>

              <FormActions onClose={onClose} isPending={isPending} submitLabel="Créer le client" />
            </form>
          )}
        </div>
      </div>
    </div>
  )
}

// ─── Local sub-components ─────────────────────────────────────────────────────

function Section({ icon, title, children }: { icon: React.ReactNode; title: string; children: React.ReactNode }) {
  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <span className="text-slate-400">{icon}</span>
        <p className="text-xs font-semibold uppercase tracking-wider text-slate-500">{title}</p>
      </div>
      {children}
    </div>
  )
}

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <Label className="text-slate-600">{label}</Label>
      {children}
      {error && <p className="text-xs text-red-500">{error}</p>}
    </div>
  )
}

function FormActions({ onClose, isPending, submitLabel }: { onClose: () => void; isPending: boolean; submitLabel: string }) {
  return (
    <div className="flex gap-3 border-t pt-4">
      <Button type="button" variant="outline" className="flex-1" onClick={onClose}>
        Annuler
      </Button>
      <Button type="submit" className="flex-1" disabled={isPending}>
        {isPending ? (
          <span className="flex items-center gap-2">
            <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
            Enregistrement…
          </span>
        ) : submitLabel}
      </Button>
    </div>
  )
}
