export function DropflowLogo({ className }: { className?: string }) {
  return (
    <svg viewBox="115 96 282 320" className={className} fill="none" aria-hidden="true">
      <polygon points="256,116 377,186 377,326 256,396 135,326 135,186" fill="currentColor" />
      <g stroke="#0f172a" strokeOpacity="0.4" strokeWidth="18" strokeLinecap="round" strokeLinejoin="round">
        <line x1="256" y1="256" x2="377" y2="186" />
        <line x1="256" y1="256" x2="135" y2="186" />
        <line x1="256" y1="256" x2="256" y2="396" />
      </g>
    </svg>
  )
}
