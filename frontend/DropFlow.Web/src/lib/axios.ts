import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { useAuthStore } from '@/store/authStore'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  headers: { 'Content-Type': 'application/json' },
})

let isRefreshing = false
let refreshSubscribers: ((token: string) => void)[] = []

function subscribeTokenRefresh(callback: (token: string) => void) {
  refreshSubscribers.push(callback)
}

function onTokenRefreshed(token: string) {
  refreshSubscribers.forEach(cb => cb(token))
  refreshSubscribers = []
}

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = useAuthStore.getState().token
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

apiClient.interceptors.response.use(
  response => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean }

    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error)
    }

    // Prevent infinite loop if the refresh endpoint itself returns 401
    if (originalRequest.url?.includes('/auth/refresh')) {
      useAuthStore.getState().clearAuth()
      window.location.href = '/login'
      return Promise.reject(error)
    }

    if (isRefreshing) {
      return new Promise(resolve => {
        subscribeTokenRefresh(token => {
          originalRequest.headers.Authorization = `Bearer ${token}`
          resolve(apiClient(originalRequest))
        })
      })
    }

    originalRequest._retry = true
    isRefreshing = true

    try {
      const refreshToken = useAuthStore.getState().refreshToken
      const response = await axios.post<{ token: string; refreshToken: string }>(
        `${import.meta.env.VITE_API_URL}/auth/refresh`,
        { refreshToken },
      )

      const { token, refreshToken: newRefreshToken } = response.data
      useAuthStore.getState().setTokens(token, newRefreshToken)
      onTokenRefreshed(token)

      originalRequest.headers.Authorization = `Bearer ${token}`
      return apiClient(originalRequest)
    } catch {
      useAuthStore.getState().clearAuth()
      window.location.href = '/login'
      return Promise.reject(error)
    } finally {
      isRefreshing = false
    }
  },
)
