import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import {
  User, Mail, Phone, MapPin, Building2, Shield, Eye, EyeOff,
  CheckCircle, AlertTriangle, Lock, Save,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import { useAuthStore } from '@/store/authStore'
import { profileApi, profileKeys } from '@/api/profile'
import type { UpdateProfileDto, ChangePasswordDto } from '@/api/profile'

// ─── Helpers ──────────────────────────────────────────────────────────────────

function getInitials(firstName: string, lastName: string): string {
  return `${firstName?.[0] ?? ''}${lastName?.[0] ?? ''}`.toUpperCase() || '?'
}

function getRoleLabel(role: string): string {
  const map: Record<string, string> = {
    Admin: 'Administrateur',
    Manager: 'Manager',
    Driver: 'Livreur',
    Accountant: 'Comptable',
    ReadOnly: 'Lecture seule',
  }
  return map[role] ?? role
}

// ─── Schemas ──────────────────────────────────────────────────────────────────

const generalSchema = z.object({
  firstName: z.string().min(1, 'Le prénom est requis'),
  lastName: z.string().min(1, 'Le nom est requis'),
  phoneNumber: z.string().optional(),
  address: z.string().optional(),
})

const passwordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Le mot de passe actuel est requis'),
    newPassword: z
      .string()
      .min(8, 'Minimum 8 caractères')
      .regex(/[A-Z]/, 'Doit contenir au moins 1 majuscule')
      .regex(/[0-9]/, 'Doit contenir au moins 1 chiffre')
      .regex(/[!@#$%^&*(),.?":{}|<>]/, 'Doit contenir au moins 1 symbole'),
    confirmNewPassword: z.string().min(1, 'La confirmation est requise'),
  })
  .refine(d => d.newPassword === d.confirmNewPassword, {
    message: 'Les mots de passe ne correspondent pas',
    path: ['confirmNewPassword'],
  })

type GeneralForm = z.infer<typeof generalSchema>
type PasswordForm = z.infer<typeof passwordSchema>

// ─── Password strength ────────────────────────────────────────────────────────

function getPasswordStrength(pwd: string): { score: number; label: string; color: string } {
  if (!pwd) return { score: 0, label: '', color: '' }
  let s = 0
  if (pwd.length >= 8) s += 25
  if (/[a-z]/.test(pwd) && /[A-Z]/.test(pwd)) s += 25
  if (/[0-9]/.test(pwd)) s += 25
  if (/[!@#$%^&*(),.?":{}|<>]/.test(pwd)) s += 25
  if (s < 50) return { score: s, label: 'Faible', color: 'bg-red-500' }
  if (s < 75) return { score: s, label: 'Moyen', color: 'bg-amber-500' }
  return { score: s, label: 'Fort', color: 'bg-emerald-500' }
}

// ─── General Tab ─────────────────────────────────────────────────────────────

function GeneralTab() {
  const queryClient = useQueryClient()

  const { data: profile, isLoading } = useQuery({
    queryKey: profileKeys.current(),
    queryFn: profileApi.getProfile,
  })

  const {
    register,
    handleSubmit,
    formState: { errors, isDirty },
    reset,
  } = useForm<GeneralForm>({
    resolver: zodResolver(generalSchema),
    values: profile
      ? {
          firstName: profile.firstName,
          lastName: profile.lastName,
          phoneNumber: profile.phoneNumber ?? '',
          address: profile.address ?? '',
        }
      : undefined,
  })

  const mutation = useMutation({
    mutationFn: (data: UpdateProfileDto) => profileApi.updateProfile(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: profileKeys.current() })
      toast.success('Profil mis à jour avec succès')
    },
    onError: err => {
      const msg = isAxiosError(err) ? err.response?.data?.message : undefined
      toast.error(msg ?? 'Erreur lors de la sauvegarde')
    },
  })

  function onSubmit(data: GeneralForm) {
    mutation.mutate({
      firstName: data.firstName,
      lastName: data.lastName,
      phoneNumber: data.phoneNumber || undefined,
      address: data.address || undefined,
    })
  }

  if (isLoading) {
    return (
      <div className="space-y-4 p-6">
        {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-12 rounded-lg" />)}
      </div>
    )
  }

  if (!profile) {
    return (
      <div className="m-6 flex items-start gap-3 rounded-xl border border-red-200 bg-red-50 p-4">
        <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0 text-red-600" />
        <p className="text-sm text-red-700">Impossible de charger les informations du profil</p>
      </div>
    )
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="p-6">
      <h3 className="mb-6 text-base font-semibold text-slate-800">Informations personnelles</h3>

      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
        {/* First name */}
        <div className="space-y-1.5">
          <Label htmlFor="firstName" className="text-sm font-medium text-slate-700">
            Prénom <span className="text-red-500">*</span>
          </Label>
          <div className="relative">
            <User className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <Input
              id="firstName"
              className={cn('pl-9', errors.firstName && 'border-red-400 focus-visible:ring-red-400')}
              placeholder="Jean"
              {...register('firstName')}
            />
          </div>
          {errors.firstName && (
            <p className="text-xs text-red-500">{errors.firstName.message}</p>
          )}
        </div>

        {/* Last name */}
        <div className="space-y-1.5">
          <Label htmlFor="lastName" className="text-sm font-medium text-slate-700">
            Nom <span className="text-red-500">*</span>
          </Label>
          <div className="relative">
            <User className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <Input
              id="lastName"
              className={cn('pl-9', errors.lastName && 'border-red-400 focus-visible:ring-red-400')}
              placeholder="Dupont"
              {...register('lastName')}
            />
          </div>
          {errors.lastName && (
            <p className="text-xs text-red-500">{errors.lastName.message}</p>
          )}
        </div>

        {/* Email — readonly */}
        <div className="space-y-1.5">
          <Label htmlFor="email" className="text-sm font-medium text-slate-700">Email</Label>
          <div className="relative">
            <Mail className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <Input
              id="email"
              value={profile.email}
              disabled
              className="cursor-not-allowed bg-slate-50 pl-9 text-slate-500"
            />
          </div>
          <p className="text-xs text-slate-400">L'email ne peut pas être modifié</p>
        </div>

        {/* Phone */}
        <div className="space-y-1.5">
          <Label htmlFor="phone" className="text-sm font-medium text-slate-700">Téléphone</Label>
          <div className="relative">
            <Phone className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <Input
              id="phone"
              className="pl-9"
              placeholder="+33 6 12 34 56 78"
              {...register('phoneNumber')}
            />
          </div>
          <p className="text-xs text-slate-400">Format : +33 6 12 34 56 78</p>
        </div>

        {/* Address */}
        <div className="space-y-1.5 sm:col-span-2">
          <Label htmlFor="address" className="text-sm font-medium text-slate-700">Adresse</Label>
          <div className="relative">
            <MapPin className="absolute left-3 top-3 h-4 w-4 text-slate-400" />
            <textarea
              id="address"
              rows={2}
              className={cn(
                'w-full resize-none rounded-md border border-input bg-background py-2 pl-9 pr-3 text-sm',
                'placeholder:text-muted-foreground',
                'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
              )}
              placeholder="Adresse complète"
              {...register('address')}
            />
          </div>
        </div>
      </div>

      {/* Read-only section */}
      <div className="mt-6 border-t pt-5">
        <p className="mb-4 text-xs font-semibold uppercase tracking-wider text-slate-400">
          Informations entreprise (lecture seule)
        </p>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="space-y-1.5">
            <Label className="text-sm font-medium text-slate-700">Entreprise</Label>
            <div className="relative">
              <Building2 className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <Input
                value={profile.tenantName ?? ''}
                disabled
                className="cursor-not-allowed bg-slate-50 pl-9 text-slate-500"
              />
            </div>
          </div>
          <div className="space-y-1.5">
            <Label className="text-sm font-medium text-slate-700">Rôle</Label>
            <div className="relative">
              <Shield className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <Input
                value={getRoleLabel(profile.role)}
                disabled
                className="cursor-not-allowed bg-slate-50 pl-9 text-slate-500"
              />
            </div>
          </div>
        </div>

        <div className="mt-4">
          <div
            className={cn(
              'flex items-center gap-2 rounded-xl border px-4 py-3 text-sm',
              profile.isActive
                ? 'border-emerald-200 bg-emerald-50 text-emerald-700'
                : 'border-amber-200 bg-amber-50 text-amber-700',
            )}
          >
            {profile.isActive ? (
              <CheckCircle className="h-4 w-4 shrink-0" />
            ) : (
              <AlertTriangle className="h-4 w-4 shrink-0" />
            )}
            {profile.isActive ? 'Votre compte est actif' : 'Votre compte est désactivé'}
          </div>
          <div className="mt-3 flex flex-wrap gap-4 text-xs text-slate-400">
            <span>
              <strong className="text-slate-500">Membre depuis :</strong>{' '}
              {new Date(profile.createdDate).toLocaleDateString('fr-FR')}
            </span>
            {profile.lastLoginDate && (
              <span>
                <strong className="text-slate-500">Dernière connexion :</strong>{' '}
                {new Date(profile.lastLoginDate).toLocaleString('fr-FR', {
                  dateStyle: 'short',
                  timeStyle: 'short',
                })}
              </span>
            )}
          </div>
        </div>
      </div>

      {/* Actions */}
      <div className="mt-6 flex justify-end gap-3">
        <Button
          type="button"
          variant="outline"
          onClick={() => reset()}
          disabled={!isDirty || mutation.isPending}
        >
          Annuler
        </Button>
        <Button type="submit" disabled={!isDirty || mutation.isPending}>
          {mutation.isPending ? (
            <>
              <span className="mr-2 h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
              Enregistrement...
            </>
          ) : (
            <>
              <Save className="mr-2 h-4 w-4" />
              Enregistrer
            </>
          )}
        </Button>
      </div>
    </form>
  )
}

// ─── Security Tab ─────────────────────────────────────────────────────────────

function SecurityTab() {
  const [showCurrent, setShowCurrent] = useState(false)
  const [showNew, setShowNew] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
    reset,
  } = useForm<PasswordForm>({
    resolver: zodResolver(passwordSchema),
  })

  const newPassword = watch('newPassword', '')
  const strength = getPasswordStrength(newPassword)

  const mutation = useMutation({
    mutationFn: (data: ChangePasswordDto) => profileApi.changePassword(data),
    onSuccess: () => {
      toast.success('Mot de passe modifié avec succès')
      reset()
    },
    onError: err => {
      const msg = isAxiosError(err) ? err.response?.data?.message : undefined
      toast.error(msg ?? 'Erreur lors du changement de mot de passe')
    },
  })

  function onSubmit(data: PasswordForm) {
    mutation.mutate({
      currentPassword: data.currentPassword,
      newPassword: data.newPassword,
      confirmNewPassword: data.confirmNewPassword,
    })
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="p-6">
      <h3 className="mb-2 text-base font-semibold text-slate-800">Changer le mot de passe</h3>

      <div className="mb-6 flex items-start gap-3 rounded-xl border border-sky-200 bg-sky-50 p-4">
        <Shield className="mt-0.5 h-4 w-4 shrink-0 text-sky-600" />
        <p className="text-sm text-sky-700">
          Pour des raisons de sécurité, vous devez saisir votre mot de passe actuel avant d'en définir un nouveau.
        </p>
      </div>

      <div className="space-y-5">
        {/* Current password */}
        <div className="space-y-1.5">
          <Label htmlFor="currentPassword" className="text-sm font-medium text-slate-700">
            Mot de passe actuel <span className="text-red-500">*</span>
          </Label>
          <div className="relative">
            <Lock className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <Input
              id="currentPassword"
              type={showCurrent ? 'text' : 'password'}
              className={cn('pl-9 pr-10', errors.currentPassword && 'border-red-400')}
              {...register('currentPassword')}
            />
            <button
              type="button"
              onClick={() => setShowCurrent(v => !v)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
            >
              {showCurrent ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          </div>
          {errors.currentPassword && (
            <p className="text-xs text-red-500">{errors.currentPassword.message}</p>
          )}
        </div>

        <div className="border-t" />

        {/* New password */}
        <div className="space-y-1.5">
          <Label htmlFor="newPassword" className="text-sm font-medium text-slate-700">
            Nouveau mot de passe <span className="text-red-500">*</span>
          </Label>
          <div className="relative">
            <Lock className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <Input
              id="newPassword"
              type={showNew ? 'text' : 'password'}
              className={cn('pl-9 pr-10', errors.newPassword && 'border-red-400')}
              placeholder="Min. 8 car., 1 majuscule, 1 chiffre, 1 symbole"
              {...register('newPassword')}
            />
            <button
              type="button"
              onClick={() => setShowNew(v => !v)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
            >
              {showNew ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          </div>
          {errors.newPassword && (
            <p className="text-xs text-red-500">{errors.newPassword.message}</p>
          )}

          {/* Strength indicator */}
          {newPassword && (
            <div className="mt-2 rounded-xl bg-slate-50 p-3">
              <p className="mb-2 text-xs font-medium text-slate-600">Force du mot de passe :</p>
              <div className="h-1.5 w-full overflow-hidden rounded-full bg-slate-200">
                <div
                  className={cn('h-full rounded-full transition-all duration-300', strength.color)}
                  style={{ width: `${strength.score}%` }}
                />
              </div>
              <p
                className={cn('mt-1 text-xs font-medium', {
                  'text-red-500': strength.score < 50,
                  'text-amber-500': strength.score >= 50 && strength.score < 75,
                  'text-emerald-500': strength.score >= 75,
                })}
              >
                {strength.label}
              </p>
            </div>
          )}
        </div>

        {/* Confirm password */}
        <div className="space-y-1.5">
          <Label htmlFor="confirmNewPassword" className="text-sm font-medium text-slate-700">
            Confirmer le nouveau mot de passe <span className="text-red-500">*</span>
          </Label>
          <div className="relative">
            <Lock className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <Input
              id="confirmNewPassword"
              type={showConfirm ? 'text' : 'password'}
              className={cn('pl-9 pr-10', errors.confirmNewPassword && 'border-red-400')}
              {...register('confirmNewPassword')}
            />
            <button
              type="button"
              onClick={() => setShowConfirm(v => !v)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
            >
              {showConfirm ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          </div>
          {errors.confirmNewPassword && (
            <p className="text-xs text-red-500">{errors.confirmNewPassword.message}</p>
          )}
        </div>
      </div>

      {/* Actions */}
      <div className="mt-6 flex justify-end gap-3">
        <Button
          type="button"
          variant="outline"
          onClick={() => reset()}
          disabled={mutation.isPending}
        >
          Annuler
        </Button>
        <Button type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? (
            <>
              <span className="mr-2 h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
              Modification...
            </>
          ) : (
            <>
              <Lock className="mr-2 h-4 w-4" />
              Changer le mot de passe
            </>
          )}
        </Button>
      </div>
    </form>
  )
}

// ─── ProfilePage ──────────────────────────────────────────────────────────────

type TabKey = 'general' | 'security'

const TABS: { key: TabKey; label: string; icon: React.ElementType }[] = [
  { key: 'general', label: 'Informations générales', icon: User },
  { key: 'security', label: 'Sécurité', icon: Lock },
]

export default function ProfilePage() {
  const [activeTab, setActiveTab] = useState<TabKey>('general')
  const user = useAuthStore(s => s.user)

  const { data: profile, isLoading } = useQuery({
    queryKey: profileKeys.current(),
    queryFn: profileApi.getProfile,
  })

  const firstName = profile?.firstName ?? user?.firstName ?? ''
  const lastName = profile?.lastName ?? user?.lastName ?? ''

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* ── Hero ─────────────────────────────────────────────────────── */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 p-6 shadow-lg">
        <div
          className="absolute inset-0 opacity-10"
          style={{
            backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)',
            backgroundSize: '24px 24px',
          }}
        />
        <div className="relative flex flex-col items-start gap-5 sm:flex-row sm:items-center">
          {/* Avatar */}
          <div className="flex h-20 w-20 shrink-0 items-center justify-center rounded-2xl bg-white/20 text-3xl font-bold text-white ring-2 ring-white/30 backdrop-blur-sm">
            {isLoading ? '?' : getInitials(firstName, lastName)}
          </div>

          {/* Info */}
          <div className="flex-1">
            {isLoading ? (
              <>
                <Skeleton className="mb-2 h-7 w-48 bg-white/20" />
                <Skeleton className="h-5 w-32 bg-white/20" />
              </>
            ) : (
              <>
                <h1 className="text-2xl font-bold tracking-tight text-white">
                  {firstName} {lastName}
                </h1>
                <div className="mt-1.5 flex flex-wrap items-center gap-2">
                  <span className="rounded-full border border-white/30 bg-white/20 px-3 py-0.5 text-xs font-semibold text-white">
                    {getRoleLabel(profile?.role ?? user?.role ?? '')}
                  </span>
                  <span className="text-sm text-sky-200">{profile?.email ?? user?.email}</span>
                </div>
                {profile?.tenantName && (
                  <p className="mt-1 flex items-center gap-1.5 text-sm text-sky-200">
                    <Building2 className="h-3.5 w-3.5" />
                    {profile.tenantName}
                  </p>
                )}
              </>
            )}
          </div>

          {/* Status chip */}
          {profile && (
            <div
              className={cn(
                'flex items-center gap-1.5 rounded-full border px-3 py-1.5 text-sm font-medium',
                profile.isActive
                  ? 'border-emerald-300/50 bg-emerald-500/20 text-emerald-100'
                  : 'border-amber-300/50 bg-amber-500/20 text-amber-100',
              )}
            >
              {profile.isActive ? (
                <CheckCircle className="h-4 w-4" />
              ) : (
                <AlertTriangle className="h-4 w-4" />
              )}
              {profile.isActive ? 'Actif' : 'Inactif'}
            </div>
          )}
        </div>
      </div>

      {/* ── Tabs + Content ─────────────────────────────────────────── */}
      <div className="overflow-hidden rounded-2xl border bg-white shadow-sm">
        {/* Tab bar */}
        <div className="flex border-b bg-slate-50">
          {TABS.map(({ key, label, icon: Icon }) => (
            <button
              key={key}
              onClick={() => setActiveTab(key)}
              className={cn(
                'flex items-center gap-2 border-b-2 px-6 py-4 text-sm font-medium transition-colors',
                activeTab === key
                  ? 'border-sky-600 bg-white text-sky-700'
                  : 'border-transparent text-slate-500 hover:bg-white/60 hover:text-slate-700',
              )}
            >
              <Icon className="h-4 w-4" />
              {label}
            </button>
          ))}
        </div>

        {activeTab === 'general' ? <GeneralTab /> : <SecurityTab />}
      </div>
    </div>
  )
}
