import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { ThemeProvider } from 'next-themes'
import { APIProvider } from '@vis.gl/react-google-maps'
import { Toaster } from '@/components/ui/sonner'
import { queryClient } from '@/lib/queryClient'
import { AppRouter } from '@/router'
import './index.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider attribute="class" defaultTheme="light" enableSystem={false}>
      <QueryClientProvider client={queryClient}>
        <APIProvider apiKey={import.meta.env.VITE_GOOGLE_MAPS_API_KEY ?? ''} libraries={['places']}>
          <AppRouter />
          <Toaster richColors position="top-right" />
          <ReactQueryDevtools initialIsOpen={false} />
        </APIProvider>
      </QueryClientProvider>
    </ThemeProvider>
  </StrictMode>,
)
