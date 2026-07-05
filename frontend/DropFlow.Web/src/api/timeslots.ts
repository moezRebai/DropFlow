import { get, post, put, del } from './client'

export interface TimeSlotDto {
  id: number
  name: string
  startTime: string
  endTime: string
  displayOrder: number
}

export interface CreateTimeSlotDto {
  name: string
  startTime: string
  endTime: string
}

export interface UpdateTimeSlotDto {
  name: string
  startTime: string
  endTime: string
  displayOrder: number
}

export const timeslotKeys = {
  all: ['timeslots'] as const,
  lists: () => [...timeslotKeys.all, 'list'] as const,
  detail: (id: number) => [...timeslotKeys.all, 'detail', id] as const,
}

export const timeslotsApi = {
  getAll: () =>
    get<TimeSlotDto[]>('/api/timeslots'),

  getById: (id: number) =>
    get<TimeSlotDto>(`/api/timeslots/${id}`),

  create: (data: CreateTimeSlotDto) =>
    post<number>('/api/timeslots', data),

  update: (id: number, data: UpdateTimeSlotDto) =>
    put<void>(`/api/timeslots/${id}`, data),

  delete: (id: number) =>
    del<void>(`/api/timeslots/${id}`),
}
