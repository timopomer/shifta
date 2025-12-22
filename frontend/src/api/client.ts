import axios from 'axios'
import type {
  CreateEmployeeRequest,
  UpdateEmployeeRequest,
  EmployeeResponse,
  CreateShiftRequest,
  UpdateShiftRequest,
  ShiftResponse,
  CreateScheduleRequest,
  ScheduleResponse,
  ScheduleDetailResponse,
  CreatePreferenceRequest,
  PreferenceResponse,
  ScheduleStatus,
} from './types'

const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

// Employees API
export const employeesApi = {
  getAll: async (): Promise<EmployeeResponse[]> => {
    const { data } = await api.get<EmployeeResponse[]>('/employees')
    return data
  },

  getById: async (id: string): Promise<EmployeeResponse> => {
    const { data } = await api.get<EmployeeResponse>(`/employees/${id}`)
    return data
  },

  create: async (request: CreateEmployeeRequest): Promise<EmployeeResponse> => {
    const { data } = await api.post<EmployeeResponse>('/employees', request)
    return data
  },

  update: async (id: string, request: UpdateEmployeeRequest): Promise<EmployeeResponse> => {
    const { data } = await api.put<EmployeeResponse>(`/employees/${id}`, request)
    return data
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/employees/${id}`)
  },
}

// Schedules API
export const schedulesApi = {
  getAll: async (): Promise<ScheduleResponse[]> => {
    const { data } = await api.get<ScheduleResponse[]>('/schedules')
    return data
  },

  getByStatus: async (status: ScheduleStatus): Promise<ScheduleResponse[]> => {
    const { data } = await api.get<ScheduleResponse[]>(`/schedules/status/${status}`)
    return data
  },

  getById: async (id: string): Promise<ScheduleDetailResponse> => {
    const { data } = await api.get<ScheduleDetailResponse>(`/schedules/${id}`)
    return data
  },

  create: async (request: CreateScheduleRequest): Promise<ScheduleResponse> => {
    const { data } = await api.post<ScheduleResponse>('/schedules', request)
    return data
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/schedules/${id}`)
  },

  // Status transitions
  open: async (id: string): Promise<ScheduleResponse> => {
    const { data } = await api.post<ScheduleResponse>(`/schedules/${id}/open`)
    return data
  },

  close: async (id: string): Promise<ScheduleResponse> => {
    const { data } = await api.post<ScheduleResponse>(`/schedules/${id}/close`)
    return data
  },

  finalize: async (id: string): Promise<ScheduleResponse> => {
    const { data } = await api.post<ScheduleResponse>(`/schedules/${id}/finalize`)
    return data
  },

  archive: async (id: string): Promise<ScheduleResponse> => {
    const { data } = await api.post<ScheduleResponse>(`/schedules/${id}/archive`)
    return data
  },
}

// Shifts API (nested under schedules)
export const shiftsApi = {
  getAll: async (scheduleId: string): Promise<ShiftResponse[]> => {
    const { data } = await api.get<ShiftResponse[]>(`/schedules/${scheduleId}/shifts`)
    return data
  },

  getById: async (scheduleId: string, id: string): Promise<ShiftResponse> => {
    const { data } = await api.get<ShiftResponse>(`/schedules/${scheduleId}/shifts/${id}`)
    return data
  },

  create: async (scheduleId: string, request: CreateShiftRequest): Promise<ShiftResponse> => {
    const { data } = await api.post<ShiftResponse>(`/schedules/${scheduleId}/shifts`, request)
    return data
  },

  update: async (scheduleId: string, id: string, request: UpdateShiftRequest): Promise<ShiftResponse> => {
    const { data } = await api.put<ShiftResponse>(`/schedules/${scheduleId}/shifts/${id}`, request)
    return data
  },

  delete: async (scheduleId: string, id: string): Promise<void> => {
    await api.delete(`/schedules/${scheduleId}/shifts/${id}`)
  },
}

// Preferences API
export const preferencesApi = {
  getBySchedule: async (scheduleId: string): Promise<PreferenceResponse[]> => {
    const { data } = await api.get<PreferenceResponse[]>(`/preferences/schedule/${scheduleId}`)
    return data
  },

  getByEmployee: async (employeeId: string): Promise<PreferenceResponse[]> => {
    const { data } = await api.get<PreferenceResponse[]>(`/preferences/employee/${employeeId}`)
    return data
  },

  getById: async (id: string): Promise<PreferenceResponse> => {
    const { data } = await api.get<PreferenceResponse>(`/preferences/${id}`)
    return data
  },

  create: async (request: CreatePreferenceRequest): Promise<PreferenceResponse> => {
    const { data } = await api.post<PreferenceResponse>('/preferences', request)
    return data
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/preferences/${id}`)
  },
}

export default api
