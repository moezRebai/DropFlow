import { useNavigate } from 'react-router-dom'
import { ArrowLeft, Check } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useWizardStore } from '@/store/wizardStore'
import { Step1Info } from './Step1Info'
import { Step2Deliveries } from './Step2Deliveries'
import { Step3Team } from './Step3Team'
import { Step4Optimize } from './Step4Optimize'
import { Step5Validation } from './Step5Validation'

const STEPS = [
  { label: 'Informations', sublabel: 'Véhicule & date' },
  { label: 'Livraisons', sublabel: 'Sélection' },
  { label: 'Équipe', sublabel: 'Chauffeurs' },
  { label: 'Optimisation', sublabel: 'Itinéraire' },
  { label: 'Validation', sublabel: 'Confirmation' },
]

function Stepper({ current }: { current: number }) {
  return (
    <div className="flex items-center gap-0">
      {STEPS.map((step, idx) => {
        const stepNum = idx + 1
        const isCompleted = stepNum < current
        const isActive = stepNum === current

        return (
          <div key={idx} className="flex items-center">
            {/* Step circle + label */}
            <div className="flex flex-col items-center gap-1">
              <div className={cn(
                'flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-sm font-bold transition-all',
                isCompleted && 'bg-sky-600 text-white',
                isActive && 'bg-sky-600 text-white ring-4 ring-sky-200 dark:ring-sky-500/30',
                !isCompleted && !isActive && 'bg-muted text-muted-foreground',
              )}>
                {isCompleted ? <Check className="h-4 w-4" /> : stepNum}
              </div>
              <div className="hidden sm:block text-center">
                <p className={cn('text-xs font-medium leading-none', isActive ? 'text-sky-700 dark:text-sky-400' : isCompleted ? 'text-muted-foreground' : 'text-muted-foreground/60')}>
                  {step.label}
                </p>
                <p className="text-xs text-muted-foreground/60 mt-0.5">{step.sublabel}</p>
              </div>
            </div>

            {/* Connector */}
            {idx < STEPS.length - 1 && (
              <div className={cn(
                'mx-2 mb-5 h-0.5 w-8 sm:w-12 transition-colors',
                stepNum < current ? 'bg-sky-600' : 'bg-muted'
              )} />
            )}
          </div>
        )
      })}
    </div>
  )
}

export default function RouteWizard() {
  const navigate = useNavigate()
  const { currentStep, editRouteId, editRouteReference } = useWizardStore()
  const isEdit = !!editRouteId

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Hero */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 p-6 shadow-lg">
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, white 1px, transparent 1px)', backgroundSize: '24px 24px' }} />
        <div className="relative">
          <button
            onClick={() => navigate(isEdit ? `/routes/${editRouteId}` : '/routes')}
            className="mb-3 flex items-center gap-1.5 rounded-xl bg-white/15 px-3 py-1.5 text-xs font-semibold text-white transition-colors hover:bg-white/25"
          >
            <ArrowLeft className="h-3.5 w-3.5" />{isEdit ? 'Retour au détail' : 'Retour aux tournées'}
          </button>
          <h1 className="text-2xl font-bold tracking-tight text-white">
            {isEdit ? `Modifier ${editRouteReference}` : 'Nouvelle tournée'}
          </h1>
          <p className="text-sm text-sky-200">
            Étape {currentStep} sur {STEPS.length} — {STEPS[currentStep - 1].label}
          </p>
        </div>
      </div>

      {/* Stepper */}
      <div className="flex justify-center overflow-x-auto py-2">
        <Stepper current={currentStep} />
      </div>

      {/* Step content */}
      <div className="rounded-2xl border bg-card p-6 shadow-sm">
        <h2 className="mb-1 text-base font-semibold text-foreground">{STEPS[currentStep - 1].label}</h2>
        <p className="mb-5 text-sm text-muted-foreground">{STEPS[currentStep - 1].sublabel}</p>

        {currentStep === 1 && <Step1Info />}
        {currentStep === 2 && <Step2Deliveries />}
        {currentStep === 3 && <Step3Team />}
        {currentStep === 4 && <Step4Optimize />}
        {currentStep === 5 && <Step5Validation />}
      </div>
    </div>
  )
}
