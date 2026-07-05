import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import { ArrowLeft, CheckCircle2, Truck } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { authApi } from '@/api/auth'

const schema = z.object({
  email: z.string().email('Adresse email invalide'),
})
type FormData = z.infer<typeof schema>

export default function ForgotPasswordPage() {
  const [submitted, setSubmitted] = useState(false)
  const [submittedEmail, setSubmittedEmail] = useState('')
  const [error, setError] = useState<string | null>(null)

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { email: '' },
  })

  async function onSubmit(data: FormData) {
    setError(null)
    try {
      await authApi.forgotPassword(data.email)
      setSubmittedEmail(data.email)
      setSubmitted(true)
    } catch (err) {
      if (isAxiosError(err) && err.response?.data?.message) {
        setError(err.response.data.message)
      } else {
        setError('Une erreur est survenue. Veuillez réessayer.')
      }
    }
  }

  async function handleResend() {
    try { await authApi.forgotPassword(submittedEmail) } catch {}
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
        {!submitted ? (
          <>
            <CardHeader className="pb-4">
              <CardTitle>Mot de passe oublié ?</CardTitle>
              <CardDescription>
                Entrez votre email pour recevoir un lien de réinitialisation.
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
                  <Label htmlFor="email">Email</Label>
                  <Input
                    id="email"
                    type="email"
                    placeholder="vous@exemple.com"
                    autoFocus
                    {...form.register('email')}
                  />
                  {form.formState.errors.email && (
                    <p className="text-xs text-destructive">
                      {form.formState.errors.email.message}
                    </p>
                  )}
                </div>
                <Button type="submit" className="w-full" disabled={form.formState.isSubmitting}>
                  {form.formState.isSubmitting ? (
                    <span className="flex items-center gap-2">
                      <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                      Envoi…
                    </span>
                  ) : (
                    'Envoyer le lien'
                  )}
                </Button>
              </form>
              <div className="mt-4 text-center">
                <Link
                  to="/login"
                  className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
                >
                  <ArrowLeft className="h-3.5 w-3.5" />
                  Retour à la connexion
                </Link>
              </div>
            </CardContent>
          </>
        ) : (
          <>
            <CardHeader className="pb-4">
              <div className="mb-3 flex justify-center">
                <CheckCircle2 className="h-12 w-12 text-green-500" />
              </div>
              <CardTitle className="text-center">Email envoyé !</CardTitle>
              <CardDescription className="text-center">
                Nous avons envoyé un lien à <strong>{submittedEmail}</strong>. Le lien expire dans{' '}
                <strong>24 heures</strong>.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              <Button asChild className="w-full">
                <Link to="/login">Retour à la connexion</Link>
              </Button>
              <Button variant="outline" className="w-full" onClick={handleResend}>
                Renvoyer le lien
              </Button>
            </CardContent>
          </>
        )}
      </Card>
    </div>
  )
}
