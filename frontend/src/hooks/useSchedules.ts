import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { schedulesApi, CreateScheduleRequest, ScheduleStatus } from '@/api'

export const scheduleKeys = {
  all: ['schedules'] as const,
  byStatus: (status: ScheduleStatus) => ['schedules', 'status', status] as const,
  detail: (id: string) => ['schedules', id] as const,
}

export function useSchedules() {
  return useQuery({
    queryKey: scheduleKeys.all,
    queryFn: () => schedulesApi.getAll(),
  })
}

export function useSchedulesByStatus(status: ScheduleStatus) {
  return useQuery({
    queryKey: scheduleKeys.byStatus(status),
    queryFn: () => schedulesApi.getByStatus(status),
  })
}

export function useSchedule(id: string) {
  return useQuery({
    queryKey: scheduleKeys.detail(id),
    queryFn: () => schedulesApi.getById(id),
    enabled: !!id,
  })
}

export function useCreateSchedule() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateScheduleRequest) => schedulesApi.create(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: scheduleKeys.all })
    },
  })
}

export function useDeleteSchedule() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => schedulesApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: scheduleKeys.all })
    },
  })
}

export function useOpenSchedule() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => schedulesApi.open(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: scheduleKeys.all })
      queryClient.invalidateQueries({ queryKey: scheduleKeys.detail(id) })
    },
  })
}

export function useCloseSchedule() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => schedulesApi.close(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: scheduleKeys.all })
      queryClient.invalidateQueries({ queryKey: scheduleKeys.detail(id) })
    },
  })
}

export function useFinalizeSchedule() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => schedulesApi.finalize(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: scheduleKeys.all })
      queryClient.invalidateQueries({ queryKey: scheduleKeys.detail(id) })
    },
  })
}

export function useArchiveSchedule() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => schedulesApi.archive(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: scheduleKeys.all })
      queryClient.invalidateQueries({ queryKey: scheduleKeys.detail(id) })
    },
  })
}
