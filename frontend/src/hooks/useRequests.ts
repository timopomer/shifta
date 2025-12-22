import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  shiftRequestsApi,
  timeOffRequestsApi,
  managerEmployeesApi,
  CreateShiftRequestRequest,
  ReviewShiftRequestRequest,
  CreateTimeOffRequestRequest,
  ReviewTimeOffRequestRequest,
  CreateManagerEmployeeRequest,
} from '@/api'

// Query keys
export const requestKeys = {
  shiftRequests: ['shiftRequests'] as const,
  shiftRequestsByEmployee: (employeeId: string) => ['shiftRequests', 'employee', employeeId] as const,
  shiftRequestsBySchedule: (scheduleId: string) => ['shiftRequests', 'schedule', scheduleId] as const,
  shiftRequestsByManager: (managerId: string) => ['shiftRequests', 'manager', managerId] as const,
  pendingShiftRequestsByManager: (managerId: string) => ['shiftRequests', 'pending', 'manager', managerId] as const,
  timeOffRequests: ['timeOffRequests'] as const,
  timeOffRequestsByEmployee: (employeeId: string) => ['timeOffRequests', 'employee', employeeId] as const,
  timeOffRequestsByManager: (managerId: string) => ['timeOffRequests', 'manager', managerId] as const,
  pendingTimeOffRequestsByManager: (managerId: string) => ['timeOffRequests', 'pending', 'manager', managerId] as const,
  managerEmployees: ['managerEmployees'] as const,
  managerEmployeesByManager: (managerId: string) => ['managerEmployees', 'manager', managerId] as const,
  managerEmployeesByEmployee: (employeeId: string) => ['managerEmployees', 'employee', employeeId] as const,
}

// Shift Requests Hooks
export function useShiftRequests() {
  return useQuery({
    queryKey: requestKeys.shiftRequests,
    queryFn: shiftRequestsApi.getAll,
  })
}

export function useShiftRequestsByEmployee(employeeId: string | undefined) {
  return useQuery({
    queryKey: requestKeys.shiftRequestsByEmployee(employeeId!),
    queryFn: () => shiftRequestsApi.getByEmployee(employeeId!),
    enabled: !!employeeId,
  })
}

export function useShiftRequestsBySchedule(scheduleId: string) {
  return useQuery({
    queryKey: requestKeys.shiftRequestsBySchedule(scheduleId),
    queryFn: () => shiftRequestsApi.getBySchedule(scheduleId),
  })
}

export function useShiftRequestsByManager(managerId: string | undefined) {
  return useQuery({
    queryKey: requestKeys.shiftRequestsByManager(managerId!),
    queryFn: () => shiftRequestsApi.getByManager(managerId!),
    enabled: !!managerId,
  })
}

export function usePendingShiftRequestsByManager(managerId: string | undefined) {
  return useQuery({
    queryKey: requestKeys.pendingShiftRequestsByManager(managerId!),
    queryFn: () => shiftRequestsApi.getPendingByManager(managerId!),
    enabled: !!managerId,
  })
}

export function useCreateShiftRequest() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateShiftRequestRequest) => shiftRequestsApi.create(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shiftRequests'] })
    },
  })
}

export function useReviewShiftRequest() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, reviewerId, request }: { id: string; reviewerId: string; request: ReviewShiftRequestRequest }) =>
      shiftRequestsApi.review(id, reviewerId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shiftRequests'] })
    },
  })
}

export function useDeleteShiftRequest() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => shiftRequestsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['shiftRequests'] })
    },
  })
}

// Time Off Requests Hooks
export function useTimeOffRequests() {
  return useQuery({
    queryKey: requestKeys.timeOffRequests,
    queryFn: timeOffRequestsApi.getAll,
  })
}

export function useTimeOffRequestsByEmployee(employeeId: string | undefined) {
  return useQuery({
    queryKey: requestKeys.timeOffRequestsByEmployee(employeeId!),
    queryFn: () => timeOffRequestsApi.getByEmployee(employeeId!),
    enabled: !!employeeId,
  })
}

export function useTimeOffRequestsByManager(managerId: string | undefined) {
  return useQuery({
    queryKey: requestKeys.timeOffRequestsByManager(managerId!),
    queryFn: () => timeOffRequestsApi.getByManager(managerId!),
    enabled: !!managerId,
  })
}

export function usePendingTimeOffRequestsByManager(managerId: string | undefined) {
  return useQuery({
    queryKey: requestKeys.pendingTimeOffRequestsByManager(managerId!),
    queryFn: () => timeOffRequestsApi.getPendingByManager(managerId!),
    enabled: !!managerId,
  })
}

export function useCreateTimeOffRequest() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateTimeOffRequestRequest) => timeOffRequestsApi.create(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timeOffRequests'] })
    },
  })
}

export function useReviewTimeOffRequest() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, reviewerId, request }: { id: string; reviewerId: string; request: ReviewTimeOffRequestRequest }) =>
      timeOffRequestsApi.review(id, reviewerId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timeOffRequests'] })
    },
  })
}

export function useDeleteTimeOffRequest() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => timeOffRequestsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timeOffRequests'] })
    },
  })
}

// Manager-Employees Hooks
export function useManagerEmployees() {
  return useQuery({
    queryKey: requestKeys.managerEmployees,
    queryFn: managerEmployeesApi.getAll,
  })
}

export function useManagerWithEmployees(managerId: string | undefined) {
  return useQuery({
    queryKey: requestKeys.managerEmployeesByManager(managerId!),
    queryFn: () => managerEmployeesApi.getByManager(managerId!),
    enabled: !!managerId,
  })
}

export function useEmployeeWithManagers(employeeId: string | undefined) {
  return useQuery({
    queryKey: requestKeys.managerEmployeesByEmployee(employeeId!),
    queryFn: () => managerEmployeesApi.getByEmployee(employeeId!),
    enabled: !!employeeId,
  })
}

export function useCreateManagerEmployee() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateManagerEmployeeRequest) => managerEmployeesApi.create(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['managerEmployees'] })
    },
  })
}

export function useDeleteManagerEmployee() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => managerEmployeesApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['managerEmployees'] })
    },
  })
}

