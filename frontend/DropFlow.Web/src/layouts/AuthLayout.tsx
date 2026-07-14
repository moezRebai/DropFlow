import { Outlet } from 'react-router-dom'
import { Clock3, MapPinned, Radio } from 'lucide-react'
import { DropflowLogo } from '@/components/shared/DropflowLogo'

export function AuthLayout() {
  return (
    <div className="flex h-screen bg-background">
      <aside className="relative hidden shrink-0 overflow-hidden bg-gradient-to-br from-sky-600 via-blue-700 to-indigo-900 lg:flex lg:w-[44%] lg:max-w-xl lg:flex-col lg:justify-between lg:p-12">
        <div
          className="pointer-events-none absolute inset-0 opacity-[0.07]"
          style={{
            backgroundImage:
              'linear-gradient(white 1px, transparent 1px), linear-gradient(90deg, white 1px, transparent 1px)',
            backgroundSize: '32px 32px',
          }}
        />

        <div className="relative flex items-center gap-2.5">
          <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-white/15 ring-1 ring-white/25">
            <DropflowLogo className="h-4.5 w-4.5 text-white" />
          </div>
          <span className="text-lg font-bold tracking-tight text-white">DropFlow</span>
        </div>

        <div className="relative space-y-9">
          <div className="space-y-3.5">
            <h1 className="max-w-sm text-4xl font-bold leading-[1.15] tracking-tight text-white">
              La tournée idéale, calculée en secondes.
            </h1>
            <p className="max-w-sm text-base leading-relaxed text-sky-100/80">
              Optimisez vos itinéraires, suivez vos chauffeurs en temps réel et livrez à l&apos;heure, à chaque fois.
            </p>
          </div>

          <RoutePreview />

          <div className="flex flex-wrap gap-2.5">
            <StatChip icon={<Clock3 className="h-3.5 w-3.5" />} label="98% à l'heure" />
            <StatChip icon={<MapPinned className="h-3.5 w-3.5" />} label="+2 400 livraisons / mois" />
            <StatChip icon={<Radio className="h-3.5 w-3.5" />} label="Suivi live" />
          </div>
        </div>

        <p className="relative text-xs text-sky-100/60">
          © {new Date().getFullYear()} DropFlow — Gestion de livraisons
        </p>
      </aside>

      <div className="flex flex-1 items-center justify-center overflow-y-auto p-6 sm:p-10 lg:p-14">
        <Outlet />
      </div>
    </div>
  )
}

function StatChip({ icon, label }: { icon: React.ReactNode; label: string }) {
  return (
    <div className="flex items-center gap-1.5 rounded-full bg-white/10 px-3 py-1.5 text-xs font-medium text-white ring-1 ring-white/15">
      {icon}
      {label}
    </div>
  )
}

function RoutePreview() {
  return (
    <div className="relative rounded-2xl border border-white/10 bg-white/[0.06] p-5 backdrop-blur-sm">
      <svg viewBox="0 0 320 140" className="w-full" fill="none" aria-hidden="true">
        <path
          d="M20 110 C 80 110, 70 40, 130 45 S 220 95, 300 30"
          stroke="white"
          strokeOpacity="0.35"
          strokeWidth="2"
          strokeLinecap="round"
          strokeDasharray="6 6"
          className="route-dash"
        />
        <circle cx="20" cy="110" r="5" fill="white" fillOpacity="0.9" />
        <circle cx="130" cy="45" r="4" fill="white" fillOpacity="0.6" />
        <circle cx="300" cy="30" r="5" fill="#7dd3fc" />
        <circle
          cx="300"
          cy="30"
          r="5"
          fill="none"
          stroke="#7dd3fc"
          strokeWidth="2"
          className="motion-reduce:hidden"
        >
          <animate attributeName="r" values="5;16" dur="2s" repeatCount="indefinite" />
          <animate attributeName="opacity" values="0.6;0" dur="2s" repeatCount="indefinite" />
        </circle>
      </svg>
      <div className="mt-1 flex items-center justify-between text-[11px] text-sky-100/70">
        <span>Dépôt Nord</span>
        <span className="font-medium text-white">12 min · 4,2 km restants</span>
      </div>
    </div>
  )
}
