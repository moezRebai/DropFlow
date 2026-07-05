import { useEffect, useRef } from 'react'

const CHANNEL = 'dropflow_deliveries'

export function emitDeliveryUpdated(deliveryId: number) {
  const ch = new BroadcastChannel(CHANNEL)
  ch.postMessage({ type: 'delivery_saved', id: deliveryId })
  ch.close()
}

export function useDeliveryBroadcastListener(onUpdate: (deliveryId: number) => void) {
  const ref = useRef(onUpdate)
  useEffect(() => { ref.current = onUpdate })

  useEffect(() => {
    const ch = new BroadcastChannel(CHANNEL)
    ch.onmessage = e => {
      if (e.data?.type === 'delivery_saved') ref.current(e.data.id as number)
    }
    return () => ch.close()
  }, [])
}
