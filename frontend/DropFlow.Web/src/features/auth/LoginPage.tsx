import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import { AlertCircle, ArrowRight, Eye, EyeOff, Lock, Mail } from 'lucide-react'
import { DropflowLogo } from '@/components/shared/DropflowLogo'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { authApi, type TenantInfo, mapAuthResultToUser, getRedirectPath } from '@/api/auth'
import { useAuthStore } from '@/store/authStore'

const schema = z.object({
  email: z.string().email('Adresse email invalide'),
  password: z.string().min(1, 'Mot de passe requis'),
})
type FormData = z.infer<typeof schema>

export default function LoginPage() {
  const navigate = useNavigate()
  const setAuth = useAuthStore(s => s.setAuth)

  const [showPassword, setShowPassword] = useState(false)
  const [tenants, setTenants] = useState<TenantInfo[]>([])
  const [tenantId, setTenantId] = useState(0)
  const [isLoadingTenants, setIsLoadingTenants] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', password: '' },
  })

  const emailField = form.register('email')

  async function handleEmailBlur(e: React.FocusEvent<HTMLInputElement>) {
    emailField.onBlur(e)
    const email = form.getValues('email')
    if (!email || !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)) return

    setIsLoadingTenants(true)
    try {
      const result = await authApi.getTenants(email)
      setTenants(result)
      setTenantId(result.length === 1 ? result[0].tenantId : 0)
    } catch {
      // silently ignore — server will return an error on submit
    } finally {
      setIsLoadingTenants(false)
    }
  }

  async function onSubmit(data: FormData) {
    setError(null)
    if (tenants.length > 1 && tenantId === 0) {
      setError('Veuillez sélectionner une organisation')
      return
    }
    const resolvedTenantId = tenants.length === 1 ? tenants[0].tenantId : tenantId
    try {
      const result = await authApi.login({ ...data, tenantId: resolvedTenantId })
      if (!result.success || !result.token || !result.refreshToken) {
        setError(result.message ?? 'Identifiants incorrects')
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
        <p className="text-sm text-muted-foreground">Gestion de livraisons</p>
      </div>

      <Card className="motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-bottom-4 motion-safe:duration-500 overflow-hidden border-none shadow-xl shadow-slate-900/10">
        <div className="h-1.5 w-full bg-gradient-to-r from-sky-500 via-blue-600 to-indigo-600" />
        <CardHeader className="p-8 pb-6 sm:p-10 sm:pb-7">
          <CardTitle className="text-3xl">Connexion</CardTitle>
          <CardDescription className="text-base">Connectez-vous à votre espace DropFlow</CardDescription>
        </CardHeader>
        <CardContent className="p-8 pt-3 sm:p-10 sm:pt-3">
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

            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <div className="relative">
                <Mail className="pointer-events-none absolute left-3.5 top-1/2 h-4.5 w-4.5 -translate-y-1/2 text-muted-foreground" />
                <Input
                  id="email"
                  type="email"
                  placeholder="vous@exemple.com"
                  autoComplete="email"
                  className="h-12 pl-11 text-base transition-shadow duration-200"
                  {...emailField}
                  onBlur={handleEmailBlur}
                />
              </div>
              {form.formState.errors.email && (
                <p className="text-xs text-destructive">{form.formState.errors.email.message}</p>
              )}
            </div>

            {tenants.length > 1 && (
              <div className="space-y-2">
                <Label>Organisation</Label>
                <Select
                  value={tenantId > 0 ? String(tenantId) : ''}
                  onValueChange={v => setTenantId(Number(v))}
                  disabled={isLoadingTenants}
                >
                  <SelectTrigger className="h-12 text-base">
                    <SelectValue placeholder="Sélectionner une organisation" />
                  </SelectTrigger>
                  <SelectContent>
                    {tenants.map(t => (
                      <SelectItem key={t.tenantId} value={String(t.tenantId)}>
                        {t.tenantName}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <Label htmlFor="password">Mot de passe</Label>
                <Link to="/forgot-password" className="text-xs text-primary hover:underline">
                  Oublié ?
                </Link>
              </div>
              <div className="relative">
                <Lock className="pointer-events-none absolute left-3.5 top-1/2 h-4.5 w-4.5 -translate-y-1/2 text-muted-foreground" />
                <Input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
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
                  {showPassword ? <EyeOff className="h-4.5 w-4.5" /> : <Eye className="h-4.5 w-4.5" />}
                </button>
              </div>
              {form.formState.errors.password && (
                <p className="text-xs text-destructive">{form.formState.errors.password.message}</p>
              )}
            </div>

            <div className="border-t border-border/70 pt-7">
              <Button
                type="submit"
                className="group h-12 w-full bg-gradient-to-r from-sky-500 to-blue-600 text-base shadow-md shadow-blue-600/20 transition-all duration-200 hover:shadow-lg hover:shadow-blue-600/30 active:scale-[0.98]"
                disabled={isSubmitting}
              >
                {isSubmitting ? (
                  <span className="flex items-center gap-2">
                    <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                    Connexion…
                  </span>
                ) : (
                  <span className="flex items-center gap-1.5">
                    Se connecter
                    <ArrowRight className="h-4 w-4 transition-transform duration-200 group-hover:translate-x-0.5" />
                  </span>
                )}
              </Button>
            </div>
          </form>

          <p className="mt-8 text-center text-sm text-muted-foreground">
            Pas encore de compte ?{' '}
            <Link to="/register" className="font-medium text-primary hover:underline">
              S&apos;inscrire
            </Link>
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
