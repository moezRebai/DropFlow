import { Fragment, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import {
  AlertCircle, ArrowRight, Building2, Check, ChevronLeft,
  Eye, EyeOff, Lock, Mail, User,
} from 'lucide-react'
import { DropflowLogo } from '@/components/shared/DropflowLogo'
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
        <CardHeader className="p-8 pb-5 sm:p-10 sm:pb-6">
          <CardTitle className="text-3xl">Créer un compte</CardTitle>
          <CardDescription className="text-base">Inscription en {STEP_LABELS.length} étapes</CardDescription>
        </CardHeader>
        <CardContent className="p-8 pt-3 sm:p-10 sm:pt-3 space-y-9">
          {/* Step indicator */}
          <div className="flex items-center justify-center gap-2.5">
            {STEP_LABELS.map((label, i) => (
              <Fragment key={i}>
                <div className="flex flex-col items-center gap-1.5">
                  <div
                    className={cn(
                      'flex h-9 w-9 items-center justify-center rounded-full border-2 text-sm font-semibold transition-all duration-200',
                      i + 1 < step
                        ? 'border-primary bg-primary text-primary-foreground'
                        : i + 1 === step
                        ? 'scale-110 border-primary text-primary shadow-sm shadow-primary/30'
                        : 'border-muted text-muted-foreground',
                    )}
                  >
                    {i + 1 < step ? <Check className="h-4 w-4" /> : i + 1}
                  </div>
                  <span
                    className={cn(
                      'text-xs transition-colors duration-200',
                      i + 1 === step ? 'font-medium text-primary' : 'text-muted-foreground',
                    )}
                  >
                    {label}
                  </span>
                </div>
                {i < STEP_LABELS.length - 1 && (
                  <div
                    className={cn(
                      'mb-4 h-0.5 w-12 transition-colors duration-300',
                      i + 1 < step ? 'bg-primary' : 'bg-muted',
                    )}
                  />
                )}
              </Fragment>
            ))}
          </div>

          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-7" noValidate>
            {error && (
              <Alert
                variant="destructive"
                className="animate-shake motion-safe:animate-in motion-safe:fade-in-0"
              >
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            {/* Step 1: Company */}
            {step === 1 && (
              <div className="space-y-2 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-right-2 motion-safe:duration-300">
                <Label htmlFor="companyName">Nom de l&apos;entreprise</Label>
                <div className="relative">
                  <Building2 className="pointer-events-none absolute left-3.5 top-1/2 h-4.5 w-4.5 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    id="companyName"
                    placeholder="Acme Livraisons"
                    autoFocus
                    className="h-12 pl-11 text-base transition-shadow duration-200"
                    {...form.register('companyName')}
                  />
                </div>
                {form.formState.errors.companyName && (
                  <p className="text-xs text-destructive">
                    {form.formState.errors.companyName.message}
                  </p>
                )}
              </div>
            )}

            {/* Step 2: User info */}
            {step === 2 && (
              <div className="space-y-7 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-right-2 motion-safe:duration-300">
                <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="firstName">Prénom</Label>
                    <div className="relative">
                      <User className="pointer-events-none absolute left-3.5 top-1/2 h-4.5 w-4.5 -translate-y-1/2 text-muted-foreground" />
                      <Input
                        id="firstName"
                        autoFocus
                        className="h-12 pl-11 text-base transition-shadow duration-200"
                        {...form.register('firstName')}
                      />
                    </div>
                    {form.formState.errors.firstName && (
                      <p className="text-xs text-destructive">
                        {form.formState.errors.firstName.message}
                      </p>
                    )}
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="lastName">Nom</Label>
                    <div className="relative">
                      <User className="pointer-events-none absolute left-3.5 top-1/2 h-4.5 w-4.5 -translate-y-1/2 text-muted-foreground" />
                      <Input
                        id="lastName"
                        className="h-12 pl-11 text-base transition-shadow duration-200"
                        {...form.register('lastName')}
                      />
                    </div>
                    {form.formState.errors.lastName && (
                      <p className="text-xs text-destructive">
                        {form.formState.errors.lastName.message}
                      </p>
                    )}
                  </div>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="email">Email</Label>
                  <div className="relative">
                    <Mail className="pointer-events-none absolute left-3.5 top-1/2 h-4.5 w-4.5 -translate-y-1/2 text-muted-foreground" />
                    <Input
                      id="email"
                      type="email"
                      placeholder="vous@exemple.com"
                      className="h-12 pl-11 text-base transition-shadow duration-200"
                      {...form.register('email')}
                    />
                  </div>
                  {form.formState.errors.email && (
                    <p className="text-xs text-destructive">
                      {form.formState.errors.email.message}
                    </p>
                  )}
                </div>
              </div>
            )}

            {/* Step 3: Password */}
            {step === 3 && (
              <div className="space-y-7 motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-right-2 motion-safe:duration-300">
                <div className="space-y-2">
                  <Label htmlFor="password">Mot de passe</Label>
                  <div className="relative">
                    <Lock className="pointer-events-none absolute left-3.5 top-1/2 h-4.5 w-4.5 -translate-y-1/2 text-muted-foreground" />
                    <Input
                      id="password"
                      type={showPassword ? 'text' : 'password'}
                      autoFocus
                      className="h-12 pl-11 pr-11 text-base transition-shadow duration-200"
                      {...form.register('password')}
                    />
                    <button
                      type="button"
                      tabIndex={-1}
                      aria-label={showPassword ? 'Masquer le mot de passe' : 'Afficher le mot de passe'}
                      className="absolute right-2 top-1/2 -translate-y-1/2 rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                      onClick={() => setShowPassword(v => !v)}
                    >
                      {showPassword ? (
                        <EyeOff className="h-4.5 w-4.5" />
                      ) : (
                        <Eye className="h-4.5 w-4.5" />
                      )}
                    </button>
                  </div>
                  <div className="mt-2.5 grid grid-cols-2 gap-2">
                    {PASSWORD_CHECKS.map(check => {
                      const passed = check.test(watchedPassword)
                      return (
                        <div
                          key={check.label}
                          className={cn(
                            'flex items-center gap-1.5 text-xs transition-colors duration-200',
                            passed ? 'text-green-600 dark:text-green-400' : 'text-muted-foreground',
                          )}
                        >
                          <div
                            className={cn(
                              'flex h-3.5 w-3.5 items-center justify-center rounded-full transition-all duration-200',
                              passed ? 'scale-100 bg-green-600 dark:bg-green-500' : 'scale-90 bg-muted-foreground/30',
                            )}
                          >
                            {passed && <Check className="h-2.5 w-2.5 text-white" />}
                          </div>
                          {check.label}
                        </div>
                      )
                    })}
                  </div>
                  {form.formState.errors.password && (
                    <p className="text-xs text-destructive">
                      {form.formState.errors.password.message}
                    </p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="confirmPassword">Confirmer le mot de passe</Label>
                  <div className="relative">
                    <Lock className="pointer-events-none absolute left-3.5 top-1/2 h-4.5 w-4.5 -translate-y-1/2 text-muted-foreground" />
                    <Input
                      id="confirmPassword"
                      type={showConfirm ? 'text' : 'password'}
                      className="h-12 pl-11 pr-11 text-base transition-shadow duration-200"
                      {...form.register('confirmPassword')}
                    />
                    <button
                      type="button"
                      tabIndex={-1}
                      aria-label={showConfirm ? 'Masquer le mot de passe' : 'Afficher le mot de passe'}
                      className="absolute right-2 top-1/2 -translate-y-1/2 rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
                      onClick={() => setShowConfirm(v => !v)}
                    >
                      {showConfirm ? (
                        <EyeOff className="h-4.5 w-4.5" />
                      ) : (
                        <Eye className="h-4.5 w-4.5" />
                      )}
                    </button>
                  </div>
                  {form.formState.errors.confirmPassword && (
                    <p className="text-xs text-destructive">
                      {form.formState.errors.confirmPassword.message}
                    </p>
                  )}
                </div>
              </div>
            )}

            <div className="flex gap-3 border-t border-border/70 pt-7">
              {step > 1 && (
                <Button
                  type="button"
                  variant="outline"
                  className="group h-12 flex-1 text-base transition-all duration-200 active:scale-[0.98]"
                  onClick={() => setStep(s => s - 1)}
                >
                  <ChevronLeft className="h-4 w-4 transition-transform duration-200 group-hover:-translate-x-0.5" />
                  Précédent
                </Button>
              )}
              {step < 3 ? (
                <Button
                  type="button"
                  className="group h-12 flex-1 bg-gradient-to-r from-sky-500 to-blue-600 text-base shadow-md shadow-blue-600/20 transition-all duration-200 hover:shadow-lg hover:shadow-blue-600/30 active:scale-[0.98]"
                  onClick={handleNext}
                >
                  Suivant
                  <ArrowRight className="h-4 w-4 transition-transform duration-200 group-hover:translate-x-0.5" />
                </Button>
              ) : (
                <Button
                  type="submit"
                  className="group h-12 flex-1 bg-gradient-to-r from-sky-500 to-blue-600 text-base shadow-md shadow-blue-600/20 transition-all duration-200 hover:shadow-lg hover:shadow-blue-600/30 active:scale-[0.98]"
                  disabled={isSubmitting}
                >
                  {isSubmitting ? (
                    <span className="flex items-center gap-2">
                      <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                      Inscription…
                    </span>
                  ) : (
                    <>
                      S&apos;inscrire
                      <ArrowRight className="h-4 w-4 transition-transform duration-200 group-hover:translate-x-0.5" />
                    </>
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
