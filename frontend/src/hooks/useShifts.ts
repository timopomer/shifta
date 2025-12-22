import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { shiftsApi, CreateShiftRequest, UpdateShiftRequest } from '@/api'
import { scheduleKeys } from './useSchedules'

export const shiftKeys = {
  all: (scheduleId: string) => ['shifts', scheduleId] as const,
  detail: (scheduleId: string, id: string) => ['shifts', scheduleId, id] as const,
}

export function useShifts(scheduleId: string) {
  return useQuery({
    queryKey: shiftKeys.all(scheduleId),
    queryFn: () => shiftsApi.getAll(scheduleId),
    enabled: !!scheduleId,
  })
}

export function useShift(scheduleId: string, id: string) {
  return useQuery({
    queryKey: shiftKeys.detail(scheduleId, id),
    queryFn: () => shiftsApi.getById(scheduleId, id),
    enabled: !!scheduleId && !!id,
  })
}

export function useCreateShift() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ scheduleId, request }: { scheduleId: string; request: CreateShiftRequest }) =>
      shiftsApi.create(scheduleId, request),
    onSuccess: (_, { scheduleId }) => {
      queryClient.invalidateQueries({ queryKey: shiftKeys.all(scheduleId) })
      queryClient.invalidateQueries({ queryKey: scheduleKeys.detail(scheduleId) })
    },
  })
}

export function useUpdateShift() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      scheduleId,
      id,
      request,
    }: {
      scheduleId: string
      id: string
      request: UpdateShiftRequest
    }) => shiftsApi.update(scheduleId, id, request),
    onSuccess: (_, { scheduleId, id }) => {
      queryClient.invalidateQueries({ queryKey: shiftKeys.all(scheduleId) })
      queryClient.invalidateQueries({ queryKey: shiftKeys.detail(scheduleId, id) })
      queryClient.invalidateQueries({ queryKey: scheduleKeys.detail(scheduleId) })
    },
  })
}

export function useDeleteShift() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ scheduleId, id }: { scheduleId: string; id: string }) =>
      shiftsApi.delete(scheduleId, id),
    onSuccess: (_, { scheduleId }) => {
      queryClient.invalidateQueries({ queryKey: shiftKeys.all(scheduleId) })
      queryClient.invalidateQueries({ queryKey: scheduleKeys.detail(scheduleId) })
    },
  })
}
