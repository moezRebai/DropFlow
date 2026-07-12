import { useState, useMemo } from 'react'
import { Link } from 'react-router-dom'
import {
  LifeBuoy, Search, ChevronDown, Rocket, Package, Route as RouteIcon,
  Users2, Settings, ScrollText, Mail, MessageSquare, BookOpen, ExternalLink,
} from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { cn } from '@/lib/utils'

const SUPPORT_EMAIL = 'support@dropflow.fr'

// ─── Content ────────────────────────────────────────────────────────────────

interface Article {
  q: string
  a: string
}

interface Category {
  id: string
  title: string
  icon: React.ElementType
  color: string
  articles: Article[]
}

const CATEGORIES: Category[] = [
  {
    id: 'start',
    title: 'Démarrage',
    icon: Rocket,
    color: 'bg-sky-100 text-sky-600 dark:bg-sky-500/15 dark:text-sky-400',
    articles: [
      { q: 'Comment configurer mon entreprise ?', a: "Rendez-vous dans Paramètres → Entreprise pour renseigner vos informations légales, vos dépôts et vos coordonnées. Ces informations apparaissent sur vos documents." },
      { q: 'Comment inviter mon équipe ?', a: "Dans Paramètres → Équipe, cliquez sur « Inviter un membre », saisissez l'email et choisissez un rôle. La personne reçoit un lien d'invitation pour créer son compte." },
      { q: 'Quels sont les rôles disponibles ?', a: 'Admin (accès complet), Manager (opérations), Chauffeur (ses livraisons), Comptable (facturation) et Lecture seule (consultation).' },
    ],
  },
  {
    id: 'deliveries',
    title: 'Livraisons',
    icon: Package,
    color: 'bg-amber-100 text-amber-600 dark:bg-amber-500/15 dark:text-amber-400',
    articles: [
      { q: 'Comment créer une livraison ?', a: "Depuis Livraisons → Nouvelle livraison. Sélectionnez ou créez un client, saisissez l'adresse (avec l'autocomplétion Google), les détails et planifiez la date." },
      { q: 'Comment suivre le statut d\'une livraison ?', a: 'Chaque livraison passe par : À planifier → Confirmée → En cours → Livrée. Vous pouvez filtrer la liste par statut et changer un statut en lot.' },
      { q: 'Qu\'est-ce qu\'une livraison urgente ?', a: "Une livraison de type « Urgente » est affectée directement à un chauffeur sans passer par une tournée planifiée." },
    ],
  },
  {
    id: 'routes',
    title: 'Tournées',
    icon: RouteIcon,
    color: 'bg-violet-100 text-violet-600 dark:bg-violet-500/15 dark:text-violet-400',
    articles: [
      { q: 'Comment créer une tournée ?', a: "Tournées → Nouvelle tournée ouvre un assistant en 5 étapes : véhicule, sélection des livraisons, équipe, optimisation Google Maps puis validation." },
      { q: 'Comment fonctionne l\'optimisation ?', a: "L'étape 4 calcule l'ordre optimal des arrêts via Google Maps. Vous pouvez réorganiser manuellement : les distances et horaires sont alors recalculés." },
      { q: 'Comment modifier une tournée existante ?', a: 'Seules les tournées au statut Brouillon sont modifiables : ouvrez la tournée et cliquez sur Modifier pour rouvrir l\'assistant pré-rempli.' },
    ],
  },
  {
    id: 'clients',
    title: 'Clients',
    icon: Users2,
    color: 'bg-emerald-100 text-emerald-600 dark:bg-emerald-500/15 dark:text-emerald-400',
    articles: [
      { q: 'Comment gérer les adresses d\'un client ?', a: "Ouvrez la fiche client : vous pouvez ajouter plusieurs adresses et définir une adresse par défaut utilisée lors de la création de livraisons." },
      { q: 'Puis-je voir l\'historique d\'un client ?', a: 'Oui, la fiche client affiche l\'ensemble des livraisons passées ainsi que le chiffre d\'affaires associé.' },
    ],
  },
  {
    id: 'settings',
    title: 'Paramètres',
    icon: Settings,
    color: 'bg-muted text-muted-foreground',
    articles: [
      { q: 'Comment ajouter un véhicule ou un chauffeur ?', a: 'Dans Paramètres → Véhicules ou Chauffeurs, utilisez le bouton d\'ajout. Les chauffeurs sont liés à un compte utilisateur.' },
      { q: 'À quoi servent les créneaux horaires ?', a: 'Les créneaux (Paramètres → Créneaux) définissent des plages de livraison réutilisables, sélectionnables lors de la planification.' },
      { q: 'Que sont les enseignes ?', a: 'Les enseignes (magasins) sont les points de vente pour lesquels vous effectuez des livraisons. Gérez-les dans Paramètres → Enseignes.' },
    ],
  },
]

const POPULAR = [
  { catId: 'start', q: 'Comment inviter mon équipe ?' },
  { catId: 'routes', q: 'Comment créer une tournée ?' },
  { catId: 'deliveries', q: 'Comment créer une livraison ?' },
]

// ─── Accordion item ─────────────────────────────────────────────────────────

function ArticleItem({ article, open, onToggle }: { article: Article; open: boolean; onToggle: () => void }) {
  return (
    <div className="rounded-xl border bg-card">
      <button onClick={onToggle} aria-expanded={open} className="flex w-full items-center justify-between gap-3 px-4 py-3 text-left">
        <span className="text-sm font-medium text-foreground">{article.q}</span>
        <ChevronDown className={cn('h-4 w-4 shrink-0 text-muted-foreground transition-transform', open && 'rotate-180')} />
      </button>
      {open && <p className="border-t px-4 py-3 text-sm leading-relaxed text-muted-foreground">{article.a}</p>}
    </div>
  )
}

// ─── Page ───────────────────────────────────────────────────────────────────

export default function HelpPage() {
  const [query, setQuery] = useState('')
  const [openKey, setOpenKey] = useState<string | null>(null)
  const [contactSent, setContactSent] = useState(false)

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    if (!q) return CATEGORIES
    return CATEGORIES
      .map(c => ({ ...c, articles: c.articles.filter(a => a.q.toLowerCase().includes(q) || a.a.toLowerCase().includes(q)) }))
      .filter(c => c.articles.length > 0)
  }, [query])

  function handleContact(e: React.FormEvent) {
    e.preventDefault()
    setContactSent(true)
    toast.success('Message envoyé — notre équipe vous répondra rapidement')
  }

  return (
    <div className="mx-auto flex max-w-4xl flex-col gap-6 p-6">

      {/* Hero + search */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 p-8 shadow-lg">
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }} />
        <div className="relative flex flex-col items-center gap-4 text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white/20">
            <LifeBuoy className="h-6 w-6 text-white" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-white">Comment pouvons-nous vous aider ?</h1>
            <p className="mt-1 text-sm text-sky-200">Parcourez les guides ou contactez le support</p>
          </div>
          <div className="relative w-full max-w-lg">
            <Search className="absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-slate-400" />
            <Input
              value={query}
              onChange={e => setQuery(e.target.value)}
              placeholder="Rechercher une question…"
              className="h-12 border-0 bg-white pl-12 text-slate-800 shadow-lg"
            />
          </div>
        </div>
      </div>

      {/* Popular */}
      {!query && (
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-sm font-medium text-muted-foreground">Populaire :</span>
          {POPULAR.map(p => {
            const cat = CATEGORIES.find(c => c.id === p.catId)!
            return (
              <button
                key={p.q}
                onClick={() => { setOpenKey(`${p.catId}-${p.q}`); document.getElementById(`cat-${p.catId}`)?.scrollIntoView({ behavior: 'smooth' }) }}
                className="inline-flex items-center gap-1.5 rounded-full border bg-card px-3 py-1.5 text-xs font-medium text-muted-foreground transition-colors hover:border-sky-300 hover:text-sky-600 dark:hover:text-sky-400"
              >
                <cat.icon className="h-3.5 w-3.5" />{p.q}
              </button>
            )
          })}
        </div>
      )}

      {/* Categories */}
      {filtered.length === 0 ? (
        <div className="flex flex-col items-center gap-3 rounded-2xl border border-dashed border-border py-16 text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-full bg-muted text-muted-foreground">
            <Search className="h-6 w-6" />
          </div>
          <p className="text-sm font-medium text-muted-foreground">Aucun résultat pour « {query} »</p>
          <p className="text-sm text-muted-foreground">Essayez d'autres mots-clés ou contactez le support.</p>
        </div>
      ) : (
        <div className="flex flex-col gap-6">
          {filtered.map(cat => (
            <div key={cat.id} id={`cat-${cat.id}`}>
              <div className="mb-3 flex items-center gap-2.5">
                <div className={cn('flex h-9 w-9 items-center justify-center rounded-xl', cat.color)}>
                  <cat.icon className="h-5 w-5" />
                </div>
                <h2 className="text-lg font-bold text-foreground">{cat.title}</h2>
              </div>
              <div className="flex flex-col gap-2">
                {cat.articles.map(a => {
                  const key = `${cat.id}-${a.q}`
                  return (
                    <ArticleItem
                      key={key}
                      article={a}
                      open={openKey === key}
                      onToggle={() => setOpenKey(openKey === key ? null : key)}
                    />
                  )
                })}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Resources + contact */}
      <div className="grid gap-4 md:grid-cols-2">
        <div className="rounded-2xl border bg-card p-6 shadow-sm">
          <div className="mb-3 flex items-center gap-2">
            <BookOpen className="h-5 w-5 text-sky-600 dark:text-sky-400" />
            <h3 className="font-semibold text-foreground">Ressources</h3>
          </div>
          <div className="flex flex-col gap-2">
            <Link to="/dashboard" className="flex items-center justify-between rounded-xl border px-4 py-3 text-sm font-medium text-muted-foreground transition-colors hover:border-sky-300 hover:text-sky-600 dark:hover:text-sky-400">
              <span className="flex items-center gap-2"><Rocket className="h-4 w-4" />Guide de démarrage</span>
              <ExternalLink className="h-4 w-4 text-muted-foreground/50" />
            </Link>
            <a href={`mailto:${SUPPORT_EMAIL}`} className="flex items-center justify-between rounded-xl border px-4 py-3 text-sm font-medium text-muted-foreground transition-colors hover:border-sky-300 hover:text-sky-600 dark:hover:text-sky-400">
              <span className="flex items-center gap-2"><ScrollText className="h-4 w-4" />Documentation API</span>
              <ExternalLink className="h-4 w-4 text-muted-foreground/50" />
            </a>
          </div>
        </div>

        <div className="rounded-2xl border bg-card p-6 shadow-sm">
          <div className="mb-3 flex items-center gap-2">
            <MessageSquare className="h-5 w-5 text-sky-600 dark:text-sky-400" />
            <h3 className="font-semibold text-foreground">Contacter le support</h3>
          </div>
          {contactSent ? (
            <div className="flex flex-col items-center gap-2 py-6 text-center">
              <div className="flex h-10 w-10 items-center justify-center rounded-full bg-emerald-100 text-emerald-600 dark:bg-emerald-500/15 dark:text-emerald-400">
                <Mail className="h-5 w-5" />
              </div>
              <p className="text-sm font-medium text-foreground">Message envoyé !</p>
              <p className="text-xs text-muted-foreground">Nous vous répondrons sous 24h.</p>
            </div>
          ) : (
            <form onSubmit={handleContact} className="flex flex-col gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="subject">Sujet</Label>
                <Input id="subject" required placeholder="Objet de votre demande" />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="message">Message</Label>
                <textarea
                  id="message"
                  required
                  rows={3}
                  placeholder="Décrivez votre problème…"
                  className="w-full rounded-xl border border-input bg-background p-3 text-sm focus:border-sky-400 focus:outline-none focus:ring-1 focus:ring-sky-400"
                />
              </div>
              <Button type="submit" className="w-full">
                <Mail className="mr-1.5 h-4 w-4" />Envoyer
              </Button>
              <p className="text-center text-xs text-muted-foreground">ou écrivez-nous à <a href={`mailto:${SUPPORT_EMAIL}`} className="text-sky-600 dark:text-sky-400 hover:underline">{SUPPORT_EMAIL}</a></p>
            </form>
          )}
        </div>
      </div>
    </div>
  )
}
