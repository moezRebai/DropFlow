import { cn } from '@/lib/utils'
import { DeliveryStatus, STATUS_COLORS, STATUS_LABELS } from '@/api/deliveries'

interface Props {
  status: DeliveryStatus
  className?: string
}

export function DeliveryStatusBadge({ status, className }: Props) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium',
        STATUS_COLORS[status],
        className,
      )}
    >
      {STATUS_LABELS[status]}
    </span>
  )
}
