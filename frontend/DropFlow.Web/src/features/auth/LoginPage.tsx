import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import { Eye, EyeOff, Truck } from 'lucide-react'
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
    <div className="w-full max-w-md px-4">
      <div className="mb-8 flex flex-col items-center gap-1.5">
        <div className="flex items-center gap-2.5">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-sky-500 to-blue-600">
            <Truck className="h-5 w-5 text-white" />
          </div>
          <span className="text-2xl font-bold tracking-tight">DropFlow</span>
        </div>
        <p className="text-sm text-muted-foreground">Gestion de livraisons</p>
      </div>

      <Card>
        <CardHeader className="pb-4">
          <CardTitle>Connexion</CardTitle>
          <CardDescription>Connectez-vous à votre espace DropFlow</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <div className="space-y-1.5">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                placeholder="vous@exemple.com"
                autoComplete="email"
                {...emailField}
                onBlur={handleEmailBlur}
              />
              {form.formState.errors.email && (
                <p className="text-xs text-destructive">{form.formState.errors.email.message}</p>
              )}
            </div>

            {tenants.length > 1 && (
              <div className="space-y-1.5">
                <Label>Organisation</Label>
                <Select
                  value={tenantId > 0 ? String(tenantId) : ''}
                  onValueChange={v => setTenantId(Number(v))}
                  disabled={isLoadingTenants}
                >
                  <SelectTrigger>
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

            <div className="space-y-1.5">
              <div className="flex items-center justify-between">
                <Label htmlFor="password">Mot de passe</Label>
                <Link to="/forgot-password" className="text-xs text-primary hover:underline">
                  Oublié ?
                </Link>
              </div>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  className="pr-10"
                  {...form.register('password')}
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
              {form.formState.errors.password && (
                <p className="text-xs text-destructive">{form.formState.errors.password.message}</p>
              )}
            </div>

            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting ? (
                <span className="flex items-center gap-2">
                  <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                  Connexion…
                </span>
              ) : (
                'Se connecter'
              )}
            </Button>
          </form>

          <p className="mt-4 text-center text-sm text-muted-foreground">
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
