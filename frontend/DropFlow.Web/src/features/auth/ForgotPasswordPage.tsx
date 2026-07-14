import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { isAxiosError } from 'axios'
import { ArrowLeft, ArrowRight, CheckCircle2, Mail } from 'lucide-react'
import { DropflowLogo } from '@/components/shared/DropflowLogo'
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
    try {
      await authApi.forgotPassword(submittedEmail)
    } catch {
      // ignore — the user can retry via the same button
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
        {!submitted ? (
          <>
            <CardHeader className="p-8 pb-6 sm:p-10 sm:pb-7">
              <CardTitle className="text-3xl">Mot de passe oublié ?</CardTitle>
              <CardDescription className="text-base">
                Entrez votre email pour recevoir un lien de réinitialisation.
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
                  <Label htmlFor="email">Email</Label>
                  <div className="relative">
                    <Mail className="pointer-events-none absolute left-3.5 top-1/2 h-4.5 w-4.5 -translate-y-1/2 text-muted-foreground" />
                    <Input
                      id="email"
                      type="email"
                      placeholder="vous@exemple.com"
                      autoFocus
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
                <Button
                  type="submit"
                  className="group h-12 w-full bg-gradient-to-r from-sky-500 to-blue-600 text-base shadow-md shadow-blue-600/20 transition-all duration-200 hover:shadow-lg hover:shadow-blue-600/30 active:scale-[0.98]"
                  disabled={form.formState.isSubmitting}
                >
                  {form.formState.isSubmitting ? (
                    <span className="flex items-center gap-2">
                      <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                      Envoi…
                    </span>
                  ) : (
                    <span className="flex items-center gap-1.5">
                      Envoyer le lien
                      <ArrowRight className="h-4 w-4 transition-transform duration-200 group-hover:translate-x-0.5" />
                    </span>
                  )}
                </Button>
              </form>
              <div className="mt-8 text-center">
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
            <CardHeader className="p-8 pb-6 sm:p-10 sm:pb-7">
              <div className="mb-3 flex justify-center">
                <CheckCircle2 className="h-14 w-14 text-green-500 dark:text-green-400" />
              </div>
              <CardTitle className="text-center text-3xl">Email envoyé !</CardTitle>
              <CardDescription className="text-center text-base">
                Nous avons envoyé un lien à <strong>{submittedEmail}</strong>. Le lien expire dans{' '}
                <strong>24 heures</strong>.
              </CardDescription>
            </CardHeader>
            <CardContent className="p-8 pt-3 sm:p-10 sm:pt-3 space-y-4">
              <Button
                asChild
                className="h-12 w-full bg-gradient-to-r from-sky-500 to-blue-600 text-base shadow-md shadow-blue-600/20 transition-all duration-200 hover:shadow-lg hover:shadow-blue-600/30 active:scale-[0.98]"
              >
                <Link to="/login">Retour à la connexion</Link>
              </Button>
              <Button variant="outline" className="h-12 w-full text-base" onClick={handleResend}>
                Renvoyer le lien
              </Button>
            </CardContent>
          </>
        )}
      </Card>
    </div>
  )
}
