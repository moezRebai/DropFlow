type DeliveryEvent =
  | { type: 'created'; id: number }
  | { type: 'updated'; id: number }
  | { type: 'deleted'; id: number }
  | { type: 'bulk' }

type Listener = (event: DeliveryEvent) => void

const CHANNEL_NAME = 'dropflow-delivery-events'

class DeliveryEventBus {
  private channel: BroadcastChannel | null = null
  private listeners: Set<Listener> = new Set()

  private getChannel(): BroadcastChannel {
    if (!this.channel) {
      this.channel = new BroadcastChannel(CHANNEL_NAME)
      this.channel.onmessage = (e: MessageEvent<DeliveryEvent>) => {
        this.listeners.forEach(fn => fn(e.data))
      }
    }
    return this.channel
  }

  publish(event: DeliveryEvent): void {
    // Notify same-tab listeners immediately
    this.listeners.forEach(fn => fn(event))
    // Broadcast to other tabs
    try {
      this.getChannel().postMessage(event)
    } catch {
      // BroadcastChannel not available (SSR / old browser) — silent fail
    }
  }

  subscribe(listener: Listener): () => void {
    this.listeners.add(listener)
    // Ensure channel is open so other tabs can wake us up
    this.getChannel()
    return () => this.listeners.delete(listener)
  }
}

export const deliveryEventBus = new DeliveryEventBus()
