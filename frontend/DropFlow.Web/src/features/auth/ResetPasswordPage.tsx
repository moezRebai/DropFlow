import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import { CheckCircle2, Eye, EyeOff, Lock, XCircle } from 'lucide-react'
import { DropflowLogo } from '@/components/shared/DropflowLogo'
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
    <div className="w-full max-w-lg px-4">
      <div className="mb-10 flex flex-col items-center gap-2">
        <div className="flex items-center gap-3">
          <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-gradient-to-br from-sky-500 to-blue-600">
            <DropflowLogo className="h-5 w-5 text-white" />
          </div>
          <span className="text-3xl font-bold tracking-tight">DropFlow</span>
        </div>
      </div>

      <Card className="motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-bottom-4 motion-safe:duration-500 overflow-hidden border-none shadow-xl shadow-slate-900/10">
        <div className="h-1.5 w-full bg-gradient-to-r from-sky-500 via-blue-600 to-indigo-600" />
        {!isValidParams ? (
          <>
            <CardHeader className="p-8 pb-6 sm:p-10 sm:pb-7">
              <div className="mb-3 flex justify-center">
                <XCircle className="h-14 w-14 text-destructive" />
              </div>
              <CardTitle className="text-center text-3xl">Lien invalide</CardTitle>
              <CardDescription className="text-center text-base">
                Ce lien de réinitialisation est invalide ou a expiré.
              </CardDescription>
            </CardHeader>
            <CardContent className="p-8 pt-3 sm:p-10 sm:pt-3">
              <Button
                asChild
                className="h-12 w-full bg-gradient-to-r from-sky-500 to-blue-600 text-base shadow-md shadow-blue-600/20 transition-all duration-200 hover:shadow-lg hover:shadow-blue-600/30 active:scale-[0.98]"
              >
                <Link to="/forgot-password">Demander un nouveau lien</Link>
              </Button>
            </CardContent>
          </>
        ) : success ? (
          <>
            <CardHeader className="p-8 pb-6 sm:p-10 sm:pb-7">
              <div className="mb-3 flex justify-center">
                <CheckCircle2 className="h-14 w-14 text-green-500 dark:text-green-400" />
              </div>
              <CardTitle className="text-center text-3xl">Mot de passe modifié !</CardTitle>
              <CardDescription className="text-center text-base">
                Votre mot de passe a été réinitialisé avec succès.
              </CardDescription>
            </CardHeader>
            <CardContent className="p-8 pt-3 sm:p-10 sm:pt-3">
              <Button
                asChild
                className="h-12 w-full bg-gradient-to-r from-sky-500 to-blue-600 text-base shadow-md shadow-blue-600/20 transition-all duration-200 hover:shadow-lg hover:shadow-blue-600/30 active:scale-[0.98]"
              >
                <Link to="/login">Se connecter</Link>
              </Button>
            </CardContent>
          </>
        ) : (
          <>
            <CardHeader className="p-8 pb-6 sm:p-10 sm:pb-7">
              <CardTitle className="text-3xl">Nouveau mot de passe</CardTitle>
              <CardDescription className="text-base">
                Compte : <strong>{email}</strong>
              </CardDescription>
            </CardHeader>
            <CardContent className="p-8 pt-3 sm:p-10 sm:pt-3">
              <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-7">
                {error && (
                  <Alert
                    variant="destructive"
                    className="animate-shake motion-safe:animate-in motion-safe:fade-in-0"
                  >
                    <AlertDescription>{error}</AlertDescription>
                  </Alert>
                )}

                <div className="space-y-2">
                  <Label htmlFor="newPassword">Nouveau mot de passe</Label>
                  <div className="relative">
                    <Lock className="pointer-events-none absolute left-3.5 top-1/2 h-4.5 w-4.5 -translate-y-1/2 text-muted-foreground" />
                    <Input
                      id="newPassword"
                      type={showPassword ? 'text' : 'password'}
                      autoFocus
                      className="h-12 pl-11 pr-11 text-base transition-shadow duration-200"
                      {...form.register('newPassword')}
                    />
                    <button
                      type="button"
                      tabIndex={-1}
                      aria-label={showPassword ? 'Masquer le mot de passe' : 'Afficher le mot de passe'}
                      className="absolute right-2 top-1/2 -translate-y-1/2 rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                      onClick={() => setShowPassword(v => !v)}
                    >
                      {showPassword ? <EyeOff className="h-4.5 w-4.5" /> : <Eye className="h-4.5 w-4.5" />}
                    </button>
                  </div>
                  <div className="mt-2.5 grid grid-cols-2 gap-2">
                    {PASSWORD_CHECKS.map(check => (
                      <div
                        key={check.label}
                        className={cn(
                          'flex items-center gap-1.5 text-xs transition-colors',
                          check.test(watchedPassword) ? 'text-green-600 dark:text-green-400' : 'text-muted-foreground',
                        )}
                      >
                        <div
                          className={cn(
                            'h-1.5 w-1.5 rounded-full transition-colors',
                            check.test(watchedPassword) ? 'bg-green-600 dark:bg-green-500' : 'bg-muted-foreground/40',
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

                <div className="space-y-2">
                  <Label htmlFor="confirmNewPassword">Confirmer le mot de passe</Label>
                  <div className="relative">
                    <Lock className="pointer-events-none absolute left-3.5 top-1/2 h-4.5 w-4.5 -translate-y-1/2 text-muted-foreground" />
                    <Input
                      id="confirmNewPassword"
                      type={showConfirm ? 'text' : 'password'}
                      className="h-12 pl-11 pr-11 text-base transition-shadow duration-200"
                      {...form.register('confirmNewPassword')}
                    />
                    <button
                      type="button"
                      tabIndex={-1}
                      aria-label={showConfirm ? 'Masquer le mot de passe' : 'Afficher le mot de passe'}
                      className="absolute right-2 top-1/2 -translate-y-1/2 rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                      onClick={() => setShowConfirm(v => !v)}
                    >
                      {showConfirm ? <EyeOff className="h-4.5 w-4.5" /> : <Eye className="h-4.5 w-4.5" />}
                    </button>
                  </div>
                  {form.formState.errors.confirmNewPassword && (
                    <p className="text-xs text-destructive">
                      {form.formState.errors.confirmNewPassword.message}
                    </p>
                  )}
                </div>

                <div className="border-t border-border/70 pt-7">
                  <Button
                    type="submit"
                    className="group h-12 w-full bg-gradient-to-r from-sky-500 to-blue-600 text-base shadow-md shadow-blue-600/20 transition-all duration-200 hover:shadow-lg hover:shadow-blue-600/30 active:scale-[0.98]"
                    disabled={form.formState.isSubmitting}
                  >
                    {form.formState.isSubmitting ? (
                      <span className="flex items-center gap-2">
                        <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                        Enregistrement…
                      </span>
                    ) : (
                      'Enregistrer le nouveau mot de passe'
                    )}
                  </Button>
                </div>
              </form>
            </CardContent>
          </>
        )}
      </Card>
    </div>
  )
}
