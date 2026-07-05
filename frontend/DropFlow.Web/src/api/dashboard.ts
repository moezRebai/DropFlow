import { get } from './client'

export enum ChartPeriod {
  Week = 0,
  Month = 1,
  Year = 2,
}

export interface DashboardStatsDto {
  unplannedDeliveries: number
  unplannedTrend: number
  todayDeliveries: number
  deliveredToday: number
  monthlyRevenue: number
  revenueTrend: number
  activeRoutes: number
  totalRoutesToday: number
  driversOnRoad: number
  idleVehicles: number
}

export interface TodayDeliveryDto {
  id: number
  reference: string
  clientName: string
  deliveryAddress: string
  deliveryCity: string
  scheduledDate: string
  scheduledTime?: string
  status: string
  driverName?: string
  isLate: boolean
}

export interface RiskyDeliveryDto {
  id: number
  reference: string
  clientName: string
  deliveryAddress: string
  deliveryCity: string
  estimatedTime: string
  riskReason: string
  riskLevel: 'Warning' | 'Error' | 'Info'
}

export interface NotificationDto {
  id: number
  type: 'Success' | 'Warning' | 'Error' | 'Info'
  title: string
  message: string
  timestamp: string
  icon: string
  timeAgo: string
}

export interface RevenueChartDataDto {
  labels: string[]
  revenues: number[]
  deliveryCount: number[]
}

export interface StatusChartDataDto {
  labels: string[]
  values: number[]
  deliveredCount: number
  totalCount: number
}

export interface StoreChartDataDto {
  storeNames: string[]
  revenues: number[]
}

export const dashboardKeys = {
  all: ['dashboard'] as const,
  stats: () => [...dashboardKeys.all, 'stats'] as const,
  todayDeliveries: () => [...dashboardKeys.all, 'today-deliveries'] as const,
  riskyDeliveries: () => [...dashboardKeys.all, 'risky-deliveries'] as const,
  notifications: () => [...dashboardKeys.all, 'notifications'] as const,
  revenueChart: (period: ChartPeriod) => [...dashboardKeys.all, 'revenue-chart', period] as const,
  statusChart: (period: ChartPeriod) => [...dashboardKeys.all, 'status-chart', period] as const,
  storeChart: (period: ChartPeriod) => [...dashboardKeys.all, 'store-chart', period] as const,
}

export const dashboardApi = {
  getStats: () =>
    get<DashboardStatsDto>('/api/dashboard/stats'),

  getTodayDeliveries: () =>
    get<TodayDeliveryDto[]>('/api/dashboard/today-deliveries'),

  getRiskyDeliveries: () =>
    get<RiskyDeliveryDto[]>('/api/dashboard/risky-deliveries'),

  getNotifications: (count = 10) =>
    get<NotificationDto[]>(`/api/dashboard/notifications?count=${count}`),

  getRevenueChart: (period: ChartPeriod) =>
    get<RevenueChartDataDto>(`/api/dashboard/revenue-chart?period=${period}`),

  getStatusChart: (period: ChartPeriod) =>
    get<StatusChartDataDto>(`/api/dashboard/status-chart?period=${period}`),

  getStoreChart: (period: ChartPeriod) =>
    get<StoreChartDataDto>(`/api/dashboard/store-chart?period=${period}`),
}
