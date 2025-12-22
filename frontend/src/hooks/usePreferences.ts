import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { preferencesApi, CreatePreferenceRequest } from '@/api'

export const preferenceKeys = {
  bySchedule: (scheduleId: string) => ['preferences', 'schedule', scheduleId] as const,
  byEmployee: (employeeId: string) => ['preferences', 'employee', employeeId] as const,
  detail: (id: string) => ['preferences', id] as const,
}

export function usePreferencesBySchedule(scheduleId: string) {
  return useQuery({
    queryKey: preferenceKeys.bySchedule(scheduleId),
    queryFn: () => preferencesApi.getBySchedule(scheduleId),
    enabled: !!scheduleId,
  })
}

export function usePreferencesByEmployee(employeeId: string) {
  return useQuery({
    queryKey: preferenceKeys.byEmployee(employeeId),
    queryFn: () => preferencesApi.getByEmployee(employeeId),
    enabled: !!employeeId,
  })
}

export function usePreference(id: string) {
  return useQuery({
    queryKey: preferenceKeys.detail(id),
    queryFn: () => preferencesApi.getById(id),
    enabled: !!id,
  })
}

export function useCreatePreference() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreatePreferenceRequest) => preferencesApi.create(request),
    onSuccess: (_, request) => {
      queryClient.invalidateQueries({ queryKey: ['preferences'] })
      queryClient.invalidateQueries({ queryKey: preferenceKeys.byEmployee(request.employeeId) })
    },
  })
}

export function useDeletePreference() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => preferencesApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['preferences'] })
    },
  })
}
