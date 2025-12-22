import { createFileRoute, Navigate } from '@tanstack/react-router'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Plus, Pencil, Trash2, Users, UserCheck } from 'lucide-react'
import {
  useEmployees,
  useCreateEmployee,
  useUpdateEmployee,
  useDeleteEmployee,
} from '@/hooks'
import {
  Modal,
  ConfirmDialog,
  EmptyState,
  PageLoader,
} from '@/components'
import { useCurrentUser } from '@/context'
import { CreateEmployeeRequest, EmployeeResponse } from '@/api'
import clsx from 'clsx'

export const Route = createFileRoute('/employees')({
  component: EmployeesPage,
})

interface EmployeeFormData {
  name: string
  email: string
  abilities: string
  isManager: boolean
}

function EmployeesPage() {
  const { isManager, isLoading: userLoading } = useCurrentUser()
  const { data: employees, isLoading } = useEmployees()
  const createEmployee = useCreateEmployee()
  const updateEmployee = useUpdateEmployee()
  const deleteEmployee = useDeleteEmployee()

  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingEmployee, setEditingEmployee] = useState<EmployeeResponse | null>(null)
  const [deletingEmployee, setDeletingEmployee] = useState<EmployeeResponse | null>(null)

  // Redirect non-managers to dashboard
  if (!userLoading && !isManager) {
    return <Navigate to="/" />
  }

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<EmployeeFormData>()

  const openCreateModal = () => {
    setEditingEmployee(null)
    reset({ name: '', email: '', abilities: '', isManager: false })
    setIsModalOpen(true)
  }

  const openEditModal = (employee: EmployeeResponse) => {
    setEditingEmployee(employee)
    reset({
      name: employee.name,
      email: employee.email,
      abilities: employee.abilities.join(', '),
      isManager: employee.isManager,
    })
    setIsModalOpen(true)
  }

  const closeModal = () => {
    setIsModalOpen(false)
    setEditingEmployee(null)
    reset()
  }

  const onSubmit = async (data: EmployeeFormData) => {
    const request: CreateEmployeeRequest = {
      name: data.name,
      email: data.email,
      abilities: data.abilities
        .split(',')
        .map((a) => a.trim())
        .filter((a) => a.length > 0),
      isManager: data.isManager,
    }

    if (editingEmployee) {
      await updateEmployee.mutateAsync({ id: editingEmployee.id, request })
    } else {
      await createEmployee.mutateAsync(request)
    }
    closeModal()
  }

  const handleDelete = async () => {
    if (deletingEmployee) {
      await deleteEmployee.mutateAsync(deletingEmployee.id)
      setDeletingEmployee(null)
    }
  }

  if (isLoading || userLoading) {
    return <PageLoader />
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Employees</h1>
          <p className="mt-1 text-sm text-gray-500">
            Manage your team members and their abilities
          </p>
        </div>
        <button onClick={openCreateModal} className="btn btn-primary">
          <Plus className="h-4 w-4" />
          Add Employee
        </button>
      </div>

      {employees?.length === 0 ? (
        <div className="card">
          <EmptyState
            icon={Users}
            title="No employees yet"
            description="Get started by adding your first team member"
            action={
              <button onClick={openCreateModal} className="btn btn-primary">
                <Plus className="h-4 w-4" />
                Add Employee
              </button>
            }
          />
        </div>
      ) : (
        <div className="card p-0 overflow-hidden">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Employee
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Abilities
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Role
                </th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {employees?.map((employee) => (
                <tr key={employee.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center gap-3">
                      <div className="h-10 w-10 rounded-full bg-primary-100 flex items-center justify-center">
                        <span className="text-sm font-medium text-primary-700">
                          {employee.name.split(' ').map(n => n[0]).join('').toUpperCase()}
                        </span>
                      </div>
                      <div>
                        <div className="text-sm font-medium text-gray-900">
                          {employee.name}
                        </div>
                        <div className="text-sm text-gray-500">{employee.email}</div>
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex flex-wrap gap-1">
                      {employee.abilities.length === 0 ? (
                        <span className="text-sm text-gray-400">No abilities</span>
                      ) : (
                        employee.abilities.map((ability) => (
                          <span
                            key={ability}
                            className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-700"
                          >
                            {ability}
                          </span>
                        ))
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    {employee.isManager ? (
                      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium bg-purple-100 text-purple-700">
                        <UserCheck className="h-3 w-3" />
                        Manager
                      </span>
                    ) : (
                      <span className="text-sm text-gray-500">Employee</span>
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right">
                    <button
                      onClick={() => openEditModal(employee)}
                      className="p-1 rounded hover:bg-gray-100 text-gray-400 hover:text-gray-600"
                    >
                      <Pencil className="h-4 w-4" />
                    </button>
                    <button
                      onClick={() => setDeletingEmployee(employee)}
                      className="p-1 rounded hover:bg-gray-100 text-gray-400 hover:text-red-600 ml-2"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Create/Edit Modal */}
      <Modal
        isOpen={isModalOpen}
        onClose={closeModal}
        title={editingEmployee ? 'Edit Employee' : 'Add Employee'}
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label htmlFor="name" className="label">
              Name
            </label>
            <input
              id="name"
              type="text"
              className={clsx('input', errors.name && 'border-red-500')}
              {...register('name', { required: 'Name is required' })}
            />
            {errors.name && (
              <p className="mt-1 text-sm text-red-500">{errors.name.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="email" className="label">
              Email
            </label>
            <input
              id="email"
              type="email"
              className={clsx('input', errors.email && 'border-red-500')}
              {...register('email', {
                required: 'Email is required',
                pattern: {
                  value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
                  message: 'Invalid email address',
                },
              })}
            />
            {errors.email && (
              <p className="mt-1 text-sm text-red-500">{errors.email.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="abilities" className="label">
              Abilities
            </label>
            <input
              id="abilities"
              type="text"
              placeholder="e.g., cashier, stock, customer service"
              className="input"
              {...register('abilities')}
            />
            <p className="mt-1 text-xs text-gray-500">
              Comma-separated list of skills/abilities
            </p>
          </div>

          <div className="flex items-center gap-2">
            <input
              id="isManager"
              type="checkbox"
              className="h-4 w-4 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
              {...register('isManager')}
            />
            <label htmlFor="isManager" className="text-sm text-gray-700">
              This employee is a manager
            </label>
          </div>

          <div className="flex justify-end gap-3 pt-4">
            <button type="button" className="btn btn-secondary" onClick={closeModal}>
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={createEmployee.isPending || updateEmployee.isPending}
            >
              {createEmployee.isPending || updateEmployee.isPending
                ? 'Saving...'
                : editingEmployee
                ? 'Update'
                : 'Create'}
            </button>
          </div>
        </form>
      </Modal>

      {/* Delete Confirmation */}
      <ConfirmDialog
        isOpen={!!deletingEmployee}
        onClose={() => setDeletingEmployee(null)}
        onConfirm={handleDelete}
        title="Delete Employee"
        message={`Are you sure you want to delete ${deletingEmployee?.name}? This action cannot be undone.`}
        isLoading={deleteEmployee.isPending}
      />
    </div>
  )
}
