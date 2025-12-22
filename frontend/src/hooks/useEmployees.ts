import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { employeesApi, CreateEmployeeRequest, UpdateEmployeeRequest } from '@/api'

export const employeeKeys = {
  all: ['employees'] as const,
  detail: (id: string) => ['employees', id] as const,
}

export function useEmployees() {
  return useQuery({
    queryKey: employeeKeys.all,
    queryFn: () => employeesApi.getAll(),
  })
}

export function useEmployee(id: string) {
  return useQuery({
    queryKey: employeeKeys.detail(id),
    queryFn: () => employeesApi.getById(id),
    enabled: !!id,
  })
}

export function useCreateEmployee() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateEmployeeRequest) => employeesApi.create(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: employeeKeys.all })
    },
  })
}

export function useUpdateEmployee() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateEmployeeRequest }) =>
      employeesApi.update(id, request),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: employeeKeys.all })
      queryClient.invalidateQueries({ queryKey: employeeKeys.detail(id) })
    },
  })
}

export function useDeleteEmployee() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => employeesApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: employeeKeys.all })
    },
  })
}
