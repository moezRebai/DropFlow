import { apiClient } from '@/lib/axios'
import type { AxiosRequestConfig } from 'axios'

export { apiClient }

export async function get<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.get<T>(url, config)
  return response.data
}

export async function post<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.post<T>(url, data, config)
  return response.data
}

export async function put<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.put<T>(url, data, config)
  return response.data
}

export async function patch<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.patch<T>(url, data, config)
  return response.data
}

export async function del<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.delete<T>(url, config)
  return response.data
}

export async function downloadFile(url: string, fallbackFilename = 'download'): Promise<void> {
  const response = await apiClient.get(url, { responseType: 'blob' })
  const blob = new Blob([response.data], { type: response.headers['content-type'] as string | undefined })
  const disposition: string = response.headers['content-disposition'] ?? ''
  const match = disposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/)
  const filename = match ? match[1].replace(/['"]/g, '') : fallbackFilename
  const objectUrl = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = objectUrl
  a.download = filename
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(objectUrl)
}
