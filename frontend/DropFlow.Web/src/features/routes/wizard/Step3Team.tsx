import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Users, UserCheck, AlertTriangle, X, Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import { driversApi, driverKeys } from '@/api/drivers'
import { TeamMemberRole } from '@/api/routes'
import { useWizardStore } from '@/store/wizardStore'
import type { WizardTeamMember } from '@/store/wizardStore'

function driverInitials(firstName: string, lastName: string) {
  return `${firstName[0] ?? ''}${lastName[0] ?? ''}`.toUpperCase()
}

function driverFullName(firstName: string, lastName: string) {
  return `${firstName} ${lastName}`.trim()
}

export function Step3Team() {
  const wizard = useWizardStore()
  const [error, setError] = useState('')

  const { data: available = [], isLoading } = useQuery({
    queryKey: driverKeys.available(wizard.date),
    queryFn: () => driversApi.getAvailable(wizard.date),
    enabled: !!wizard.date,
  })

  const mainDriver = wizard.team.find(t => t.role === TeamMemberRole.MainDriver)
  const helpers = wizard.team.filter(t => t.role === TeamMemberRole.Helper)

  const selectedDriverIds = new Set(wizard.team.map(t => t.driverId))

  function selectMainDriver(driverId: number) {
    const driver = available.find(d => d.id === driverId)
    if (!driver) return
    const member: WizardTeamMember = {
      driverId,
      role: TeamMemberRole.MainDriver,
      driverName: driverFullName(driver.firstName, driver.lastName),
    }
    const withoutMain = wizard.team.filter(t => t.role !== TeamMemberRole.MainDriver)
    wizard.setTeam([member, ...withoutMain])
    setError('')
  }

  function addHelper(driverId: number) {
    const driver = available.find(d => d.id === driverId)
    if (!driver) return
    const member: WizardTeamMember = {
      driverId,
      role: TeamMemberRole.Helper,
      driverName: driverFullName(driver.firstName, driver.lastName),
    }
    wizard.setTeam([...wizard.team, member])
  }

  function removeHelper(driverId: number) {
    wizard.setTeam(wizard.team.filter(t => !(t.driverId === driverId && t.role === TeamMemberRole.Helper)))
  }

  function handleNext() {
    if (!mainDriver) {
      setError('Un chauffeur principal est requis')
      return
    }
    wizard.next()
  }

  const availableForHelper = available.filter(d => !selectedDriverIds.has(d.id))

  return (
    <div className="flex flex-col gap-6">

      {isLoading ? (
        <div className="flex flex-col gap-3">
          {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-14 rounded-xl" />)}
        </div>
      ) : available.length === 0 ? (
        <div className="flex flex-col items-center gap-3 rounded-xl border-2 border-dashed border-border py-10">
          <Users className="h-8 w-8 text-muted-foreground/40" />
          <p className="text-sm text-muted-foreground">Aucun chauffeur disponible pour le {new Date(wizard.date).toLocaleDateString('fr-FR')}</p>
          <p className="text-xs text-muted-foreground max-w-64 text-center">Les chauffeurs déjà assignés à des tournées confirmées ne sont pas affichés</p>
        </div>
      ) : (
        <>
          {/* Main driver */}
          <div className="flex flex-col gap-3">
            <div className="flex items-center gap-2">
              <UserCheck className="h-4 w-4 text-sky-600 dark:text-sky-400" />
              <h3 className="text-sm font-semibold text-foreground">Chauffeur principal <span className="text-red-500">*</span></h3>
            </div>

            <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
              {available.map(driver => {
                const isSelected = mainDriver?.driverId === driver.id
                return (
                  <button
                    key={driver.id}
                    type="button"
                    onClick={() => selectMainDriver(driver.id)}
                    aria-pressed={isSelected}
                    className={cn(
                      'flex items-center gap-3 rounded-xl border p-3 text-left transition-all',
                      isSelected
                        ? 'border-sky-500 bg-sky-50 ring-1 ring-sky-200 dark:bg-sky-500/10 dark:ring-sky-500/30'
                        : 'border-border bg-card hover:border-foreground/20 hover:bg-muted',
                      helpers.some(h => h.driverId === driver.id) && 'opacity-40 pointer-events-none'
                    )}
                  >
                    <div className={cn(
                      'flex h-9 w-9 shrink-0 items-center justify-center rounded-full text-xs font-bold text-white',
                      isSelected
                        ? 'bg-gradient-to-br from-sky-400 to-blue-600'
                        : 'bg-gradient-to-br from-slate-400 to-slate-600'
                    )}>
                      {driverInitials(driver.firstName, driver.lastName)}
                    </div>
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-foreground truncate">
                        {driverFullName(driver.firstName, driver.lastName)}
                      </p>
                      {driver.phone && <p className="text-xs text-muted-foreground">{driver.phone}</p>}
                    </div>
                    {isSelected && (
                      <div className="ml-auto shrink-0 h-5 w-5 rounded-full bg-sky-600 flex items-center justify-center">
                        <svg className="h-3 w-3 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                        </svg>
                      </div>
                    )}
                  </button>
                )
              })}
            </div>
          </div>

          {/* Helpers */}
          <div className="flex flex-col gap-3">
            <div className="flex items-center gap-2">
              <Users className="h-4 w-4 text-muted-foreground" />
              <h3 className="text-sm font-semibold text-foreground">Assistants <span className="text-xs font-normal text-muted-foreground">(optionnel)</span></h3>
            </div>

            {helpers.length > 0 && (
              <div className="flex flex-col gap-2">
                {helpers.map(h => {
                  const driver = available.find(d => d.id === h.driverId)
                  return (
                    <div key={h.driverId} className="flex items-center gap-3 rounded-xl border bg-muted px-3 py-2.5">
                      <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-slate-400 to-slate-600 text-xs font-bold text-white">
                        {driver ? driverInitials(driver.firstName, driver.lastName) : '?'}
                      </div>
                      <p className="flex-1 text-sm font-medium text-foreground">{h.driverName}</p>
                      <button onClick={() => removeHelper(h.driverId)} aria-label={`Retirer ${h.driverName}`} className="text-muted-foreground hover:text-red-500">
                        <X className="h-4 w-4" />
                      </button>
                    </div>
                  )
                })}
              </div>
            )}

            {availableForHelper.length > 0 && (
              <div className="flex flex-wrap gap-2">
                {availableForHelper.map(driver => (
                  <button
                    key={driver.id}
                    type="button"
                    onClick={() => addHelper(driver.id)}
                    className="flex items-center gap-1.5 rounded-lg border border-dashed border-border px-3 py-1.5 text-xs font-medium text-muted-foreground hover:border-sky-400 hover:text-sky-600 dark:hover:text-sky-400 transition-colors"
                  >
                    <Plus className="h-3 w-3" />
                    {driverFullName(driver.firstName, driver.lastName)}
                  </button>
                ))}
              </div>
            )}
          </div>
        </>
      )}

      {error && (
        <div className="flex items-center gap-2 rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-600 dark:bg-red-500/10 dark:text-red-400">
          <AlertTriangle className="h-4 w-4 shrink-0" />{error}
        </div>
      )}

      {/* Nav */}
      <div className="flex justify-between pt-2">
        <Button variant="outline" onClick={wizard.prev}>← Précédent</Button>
        <Button onClick={handleNext} className="bg-sky-600 hover:bg-sky-700 text-white px-6">
          Étape suivante →
        </Button>
      </div>
    </div>
  )
}
