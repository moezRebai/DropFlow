import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import { CheckCircle2, Eye, EyeOff, Truck, XCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { cn } from '@/lib/utils'
import { authApi } from '@/api/auth'

const PASSWORD_CHECKS = [
  { label: '8 caractères minimum', test: (p: string) => p.length >= 8 },
  { label: 'Une majuscule', test: (p: string) => /[A-Z]/.test(p) },
  { label: 'Un chiffre', test: (p: string) => /[0-9]/.test(p) },
  { label: 'Un caractère spécial', test: (p: string) => /[!@#$%^&*()_+\-=[\]{}|;:,.<>?]/.test(p) },
]

const schema = z
  .object({
    newPassword: z
      .string()
      .min(8, 'Au moins 8 caractères')
      .regex(/[A-Z]/, 'Au moins une majuscule')
      .regex(/[0-9]/, 'Au moins un chiffre')
      .regex(/[!@#$%^&*()_+\-=[\]{}|;:,.<>?]/, 'Au moins un caractère spécial'),
    confirmNewPassword: z.string().min(1, 'Confirmation requise'),
  })
  .refine(d => d.newPassword === d.confirmNewPassword, {
    message: 'Les mots de passe ne correspondent pas',
    path: ['confirmNewPassword'],
  })

type FormData = z.infer<typeof schema>

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const email = searchParams.get('email') ?? ''
  const token = searchParams.get('token') ?? ''
  const tenantId = parseInt(searchParams.get('tenantId') ?? '0', 10)
  const isValidParams = Boolean(email && token && tenantId > 0)

  const [success, setSuccess] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { newPassword: '', confirmNewPassword: '' },
  })

  const watchedPassword = form.watch('newPassword')

  async function onSubmit(data: FormData) {
    setError(null)
    try {
      const result = await authApi.resetPassword({ email, token, tenantId, ...data })
      if (result.success) {
        setSuccess(true)
      } else {
        setError(result.message ?? 'La réinitialisation a échoué')
      }
    } catch (err) {
      if (isAxiosError(err) && err.response?.data?.message) {
        setError(err.response.data.message)
      } else {
        setError('Une erreur est survenue. Veuillez réessayer.')
      }
    }
  }

  return (
    <div className="w-full max-w-md px-4">
      <div className="mb-8 flex flex-col items-center gap-1.5">
        <div className="flex items-center gap-2.5">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary">
            <Truck className="h-5 w-5 text-primary-foreground" />
          </div>
          <span className="text-2xl font-bold tracking-tight">DropFlow</span>
        </div>
      </div>

      <Card>
        {!isValidParams ? (
          <>
            <CardHeader className="pb-4">
              <div className="mb-3 flex justify-center">
                <XCircle className="h-12 w-12 text-destructive" />
              </div>
              <CardTitle className="text-center">Lien invalide</CardTitle>
              <CardDescription className="text-center">
                Ce lien de réinitialisation est invalide ou a expiré.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Button asChild className="w-full">
                <Link to="/forgot-password">Demander un nouveau lien</Link>
              </Button>
            </CardContent>
          </>
        ) : success ? (
          <>
            <CardHeader className="pb-4">
              <div className="mb-3 flex justify-center">
                <CheckCircle2 className="h-12 w-12 text-green-500" />
              </div>
              <CardTitle className="text-center">Mot de passe modifié !</CardTitle>
              <CardDescription className="text-center">
                Votre mot de passe a été réinitialisé avec succès.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Button asChild className="w-full">
                <Link to="/login">Se connecter</Link>
              </Button>
            </CardContent>
          </>
        ) : (
          <>
            <CardHeader className="pb-4">
              <CardTitle>Nouveau mot de passe</CardTitle>
              <CardDescription>
                Compte : <strong>{email}</strong>
              </CardDescription>
            </CardHeader>
            <CardContent>
              <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                {error && (
                  <Alert variant="destructive">
                    <AlertDescription>{error}</AlertDescription>
                  </Alert>
                )}

                <div className="space-y-1.5">
                  <Label htmlFor="newPassword">Nouveau mot de passe</Label>
                  <div className="relative">
                    <Input
                      id="newPassword"
                      type={showPassword ? 'text' : 'password'}
                      autoFocus
                      className="pr-10"
                      {...form.register('newPassword')}
                    />
                    <button
                      type="button"
                      tabIndex={-1}
                      className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                      onClick={() => setShowPassword(v => !v)}
                    >
                      {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                    </button>
                  </div>
                  <div className="mt-2 grid grid-cols-2 gap-1.5">
                    {PASSWORD_CHECKS.map(check => (
                      <div
                        key={check.label}
                        className={cn(
                          'flex items-center gap-1.5 text-xs transition-colors',
                          check.test(watchedPassword) ? 'text-green-600' : 'text-muted-foreground',
                        )}
                      >
                        <div
                          className={cn(
                            'h-1.5 w-1.5 rounded-full transition-colors',
                            check.test(watchedPassword) ? 'bg-green-600' : 'bg-muted-foreground/40',
                          )}
                        />
                        {check.label}
                      </div>
                    ))}
                  </div>
                  {form.formState.errors.newPassword && (
                    <p className="text-xs text-destructive">
                      {form.formState.errors.newPassword.message}
                    </p>
                  )}
                </div>

                <div className="space-y-1.5">
                  <Label htmlFor="confirmNewPassword">Confirmer le mot de passe</Label>
                  <div className="relative">
                    <Input
                      id="confirmNewPassword"
                      type={showConfirm ? 'text' : 'password'}
                      className="pr-10"
                      {...form.register('confirmNewPassword')}
                    />
                    <button
                      type="button"
                      tabIndex={-1}
                      className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                      onClick={() => setShowConfirm(v => !v)}
                    >
                      {showConfirm ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                    </button>
                  </div>
                  {form.formState.errors.confirmNewPassword && (
                    <p className="text-xs text-destructive">
                      {form.formState.errors.confirmNewPassword.message}
                    </p>
                  )}
                </div>

                <Button type="submit" className="w-full" disabled={form.formState.isSubmitting}>
                  {form.formState.isSubmitting ? (
                    <span className="flex items-center gap-2">
                      <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                      Enregistrement…
                    </span>
                  ) : (
                    'Enregistrer le nouveau mot de passe'
                  )}
                </Button>
              </form>
            </CardContent>
          </>
        )}
      </Card>
    </div>
  )
}
