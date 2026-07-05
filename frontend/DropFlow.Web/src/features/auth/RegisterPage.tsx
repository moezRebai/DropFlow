import { Fragment, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import { Check, ChevronLeft, Eye, EyeOff, Truck } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { cn } from '@/lib/utils'
import { authApi, mapAuthResultToUser, getRedirectPath } from '@/api/auth'
import { useAuthStore } from '@/store/authStore'

const PASSWORD_CHECKS = [
  { label: '8 caractères minimum', test: (p: string) => p.length >= 8 },
  { label: 'Une majuscule', test: (p: string) => /[A-Z]/.test(p) },
  { label: 'Un chiffre', test: (p: string) => /[0-9]/.test(p) },
  { label: 'Un caractère spécial', test: (p: string) => /[!@#$%^&*()_+\-=[\]{}|;:,.<>?]/.test(p) },
]

const schema = z
  .object({
    companyName: z.string().min(2, 'Au moins 2 caractères'),
    firstName: z.string().min(1, 'Prénom requis'),
    lastName: z.string().min(1, 'Nom requis'),
    email: z.string().email('Email invalide'),
    password: z
      .string()
      .min(8, 'Au moins 8 caractères')
      .regex(/[A-Z]/, 'Au moins une majuscule')
      .regex(/[0-9]/, 'Au moins un chiffre')
      .regex(/[!@#$%^&*()_+\-=[\]{}|;:,.<>?]/, 'Au moins un caractère spécial'),
    confirmPassword: z.string().min(1, 'Confirmation requise'),
  })
  .refine(d => d.password === d.confirmPassword, {
    message: 'Les mots de passe ne correspondent pas',
    path: ['confirmPassword'],
  })

type FormData = z.infer<typeof schema>

const STEP_FIELDS: Record<number, (keyof FormData)[]> = {
  1: ['companyName'],
  2: ['firstName', 'lastName', 'email'],
  3: ['password', 'confirmPassword'],
}

const STEP_LABELS = ['Entreprise', 'Compte', 'Sécurité']

export default function RegisterPage() {
  const navigate = useNavigate()
  const setAuth = useAuthStore(s => s.setAuth)

  const [step, setStep] = useState(1)
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    mode: 'onTouched',
    defaultValues: {
      companyName: '',
      firstName: '',
      lastName: '',
      email: '',
      password: '',
      confirmPassword: '',
    },
  })

  const watchedPassword = form.watch('password')

  async function handleNext() {
    const valid = await form.trigger(STEP_FIELDS[step])
    if (valid) setStep(s => Math.min(s + 1, 3))
  }

  async function onSubmit(data: FormData) {
    setError(null)
    try {
      const result = await authApi.register(data)
      if (!result.success || !result.token || !result.refreshToken) {
        setError(result.message ?? "L'inscription a échoué")
        return
      }
      const user = mapAuthResultToUser(result)
      if (!user) {
        setError('Réponse invalide du serveur')
        return
      }
      setAuth(result.token, result.refreshToken, user)
      navigate(getRedirectPath(user.role, user.tenantId), { replace: true })
    } catch (err) {
      if (isAxiosError(err) && err.response?.data?.message) {
        setError(err.response.data.message)
      } else {
        setError('Une erreur est survenue. Veuillez réessayer.')
      }
    }
  }

  const isSubmitting = form.formState.isSubmitting

  return (
    <div className="w-full max-w-md px-4">
      <div className="mb-8 flex flex-col items-center gap-1.5">
        <div className="flex items-center gap-2.5">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-sky-500 to-blue-600">
            <Truck className="h-5 w-5 text-white" />
          </div>
          <span className="text-2xl font-bold tracking-tight">DropFlow</span>
        </div>
      </div>

      <Card>
        <CardHeader className="pb-2">
          <CardTitle>Créer un compte</CardTitle>
          <CardDescription>Inscription en {STEP_LABELS.length} étapes</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {/* Step indicator */}
          <div className="flex items-center justify-center gap-2">
            {STEP_LABELS.map((label, i) => (
              <Fragment key={i}>
                <div className="flex flex-col items-center gap-1">
                  <div
                    className={cn(
                      'flex h-8 w-8 items-center justify-center rounded-full border-2 text-sm font-semibold transition-colors',
                      i + 1 < step
                        ? 'border-primary bg-primary text-primary-foreground'
                        : i + 1 === step
                        ? 'border-primary text-primary'
                        : 'border-muted text-muted-foreground',
                    )}
                  >
                    {i + 1 < step ? <Check className="h-4 w-4" /> : i + 1}
                  </div>
                  <span
                    className={cn(
                      'text-xs',
                      i + 1 === step ? 'font-medium text-primary' : 'text-muted-foreground',
                    )}
                  >
                    {label}
                  </span>
                </div>
                {i < STEP_LABELS.length - 1 && (
                  <div
                    className={cn('mb-4 h-0.5 w-10', i + 1 < step ? 'bg-primary' : 'bg-muted')}
                  />
                )}
              </Fragment>
            ))}
          </div>

          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            {/* Step 1: Company */}
            {step === 1 && (
              <div className="space-y-1.5">
                <Label htmlFor="companyName">Nom de l&apos;entreprise</Label>
                <Input
                  id="companyName"
                  placeholder="Acme Livraisons"
                  autoFocus
                  {...form.register('companyName')}
                />
                {form.formState.errors.companyName && (
                  <p className="text-xs text-destructive">
                    {form.formState.errors.companyName.message}
                  </p>
                )}
              </div>
            )}

            {/* Step 2: User info */}
            {step === 2 && (
              <>
                <div className="grid grid-cols-2 gap-3">
                  <div className="space-y-1.5">
                    <Label htmlFor="firstName">Prénom</Label>
                    <Input id="firstName" autoFocus {...form.register('firstName')} />
                    {form.formState.errors.firstName && (
                      <p className="text-xs text-destructive">
                        {form.formState.errors.firstName.message}
                      </p>
                    )}
                  </div>
                  <div className="space-y-1.5">
                    <Label htmlFor="lastName">Nom</Label>
                    <Input id="lastName" {...form.register('lastName')} />
                    {form.formState.errors.lastName && (
                      <p className="text-xs text-destructive">
                        {form.formState.errors.lastName.message}
                      </p>
                    )}
                  </div>
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="email">Email</Label>
                  <Input
                    id="email"
                    type="email"
                    placeholder="vous@exemple.com"
                    {...form.register('email')}
                  />
                  {form.formState.errors.email && (
                    <p className="text-xs text-destructive">
                      {form.formState.errors.email.message}
                    </p>
                  )}
                </div>
              </>
            )}

            {/* Step 3: Password */}
            {step === 3 && (
              <>
                <div className="space-y-1.5">
                  <Label htmlFor="password">Mot de passe</Label>
                  <div className="relative">
                    <Input
                      id="password"
                      type={showPassword ? 'text' : 'password'}
                      autoFocus
                      className="pr-10"
                      {...form.register('password')}
                    />
                    <button
                      type="button"
                      tabIndex={-1}
                      className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                      onClick={() => setShowPassword(v => !v)}
                    >
                      {showPassword ? (
                        <EyeOff className="h-4 w-4" />
                      ) : (
                        <Eye className="h-4 w-4" />
                      )}
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
                            check.test(watchedPassword)
                              ? 'bg-green-600'
                              : 'bg-muted-foreground/40',
                          )}
                        />
                        {check.label}
                      </div>
                    ))}
                  </div>
                  {form.formState.errors.password && (
                    <p className="text-xs text-destructive">
                      {form.formState.errors.password.message}
                    </p>
                  )}
                </div>

                <div className="space-y-1.5">
                  <Label htmlFor="confirmPassword">Confirmer le mot de passe</Label>
                  <div className="relative">
                    <Input
                      id="confirmPassword"
                      type={showConfirm ? 'text' : 'password'}
                      className="pr-10"
                      {...form.register('confirmPassword')}
                    />
                    <button
                      type="button"
                      tabIndex={-1}
                      className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                      onClick={() => setShowConfirm(v => !v)}
                    >
                      {showConfirm ? (
                        <EyeOff className="h-4 w-4" />
                      ) : (
                        <Eye className="h-4 w-4" />
                      )}
                    </button>
                  </div>
                  {form.formState.errors.confirmPassword && (
                    <p className="text-xs text-destructive">
                      {form.formState.errors.confirmPassword.message}
                    </p>
                  )}
                </div>
              </>
            )}

            <div className="flex gap-3 pt-1">
              {step > 1 && (
                <Button
                  type="button"
                  variant="outline"
                  className="flex-1"
                  onClick={() => setStep(s => s - 1)}
                >
                  <ChevronLeft className="mr-1 h-4 w-4" />
                  Précédent
                </Button>
              )}
              {step < 3 ? (
                <Button type="button" className="flex-1" onClick={handleNext}>
                  Suivant
                </Button>
              ) : (
                <Button type="submit" className="flex-1" disabled={isSubmitting}>
                  {isSubmitting ? (
                    <span className="flex items-center gap-2">
                      <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                      Inscription…
                    </span>
                  ) : (
                    "S'inscrire"
                  )}
                </Button>
              )}
            </div>
          </form>

          <p className="text-center text-sm text-muted-foreground">
            Déjà un compte ?{' '}
            <Link to="/login" className="font-medium text-primary hover:underline">
              Se connecter
            </Link>
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
