import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { cn } from '@/lib/utils'
import type { ClientAddressDto, CreateClientAddressDto, UpdateClientAddressDto } from '@/api/clients'

const schema = z.object({
  label: z.string().optional(),
  address: z.string().min(2, 'Adresse requise'),
  zipCode: z.string().min(4, 'Code postal requis'),
  city: z.string().min(1, 'Ville requise'),
  complement: z.string().optional(),
})

type FormData = z.infer<typeof schema>

interface AddressFormDialogProps {
  open: boolean
  onClose: () => void
  address?: ClientAddressDto
  onSubmit: (data: CreateClientAddressDto | UpdateClientAddressDto) => Promise<void>
  isSubmitting: boolean
}

export function AddressFormDialog({ open, onClose, address, onSubmit, isSubmitting }: AddressFormDialogProps) {
  const isEdit = !!address

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { label: '', address: '', zipCode: '', city: '', complement: '' },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        label: address?.label ?? '',
        address: address?.address ?? '',
        zipCode: address?.zipCode ?? '',
        city: address?.city ?? '',
        complement: address?.complement ?? '',
      })
    }
  }, [open, address, form])

  async function handleSubmit(data: FormData) {
    await onSubmit({
      label: data.label || undefined,
      address: data.address,
      zipCode: data.zipCode,
      city: data.city,
      complement: data.complement || undefined,
    })
  }

  if (!open) return null

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />
      <div className="relative z-10 w-full max-w-md overflow-hidden rounded-2xl bg-white shadow-2xl">

        {/* Header */}
        <div className="flex items-center justify-between border-b px-6 py-4">
          <h2 className="text-base font-semibold text-slate-800">
            {isEdit ? "Modifier l'adresse" : 'Nouvelle adresse'}
          </h2>
          <button
            onClick={onClose}
            className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        {/* Form */}
        <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-4 p-6">

          <div className="space-y-1.5">
            <Label htmlFor="addr-label">Libellé <span className="text-slate-400">(optionnel)</span></Label>
            <Input
              id="addr-label"
              placeholder="Principal, Bureau, Entrepôt…"
              {...form.register('label')}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="addr-address">Adresse <span className="text-red-500">*</span></Label>
            <Input
              id="addr-address"
              placeholder="123 Rue de la Paix"
              {...form.register('address')}
            />
            {form.formState.errors.address && (
              <p className="text-xs text-red-500">{form.formState.errors.address.message}</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="addr-zip">Code postal <span className="text-red-500">*</span></Label>
              <Input
                id="addr-zip"
                placeholder="75001"
                {...form.register('zipCode')}
              />
              {form.formState.errors.zipCode && (
                <p className="text-xs text-red-500">{form.formState.errors.zipCode.message}</p>
              )}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="addr-city">Ville <span className="text-red-500">*</span></Label>
              <Input
                id="addr-city"
                placeholder="Paris"
                {...form.register('city')}
              />
              {form.formState.errors.city && (
                <p className="text-xs text-red-500">{form.formState.errors.city.message}</p>
              )}
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="addr-complement">Complément <span className="text-slate-400">(optionnel)</span></Label>
            <Input
              id="addr-complement"
              placeholder="Bâtiment B, 2ème étage…"
              {...form.register('complement')}
            />
          </div>

          <div className={cn('flex gap-3 pt-2', isEdit && 'justify-end')}>
            <Button type="button" variant="outline" className="flex-1" onClick={onClose}>
              Annuler
            </Button>
            <Button type="submit" className="flex-1" disabled={isSubmitting}>
              {isSubmitting ? (
                <span className="flex items-center gap-2">
                  <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                  Enregistrement…
                </span>
              ) : isEdit ? 'Enregistrer' : 'Ajouter'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
