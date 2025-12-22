import { createFileRoute, Link } from '@tanstack/react-router'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import {
  ArrowLeft,
  Plus,
  Pencil,
  Trash2,
  Clock,
  Play,
  Square,
  CheckCircle,
  Archive,
  AlertCircle,
  Users,
} from 'lucide-react'
import { format } from 'date-fns'
import {
  useSchedule,
  useOpenSchedule,
  useCloseSchedule,
  useFinalizeSchedule,
  useArchiveSchedule,
  useCreateShift,
  useUpdateShift,
  useDeleteShift,
  useEmployees,
  usePreferencesBySchedule,
} from '@/hooks'
import {
  Modal,
  ConfirmDialog,
  EmptyState,
  PageLoader,
  StatusBadge,
} from '@/components'
import {
  ScheduleStatus,
  CreateShiftRequest,
  ShiftResponse,
} from '@/api'
import clsx from 'clsx'

export const Route = createFileRoute('/schedules/$scheduleId')({
  component: ScheduleDetailPage,
})

interface ShiftFormData {
  name: string
  startTime: string
  endTime: string
  requiredAbilities: string
}

function ScheduleDetailPage() {
  const { scheduleId } = Route.useParams()
  const { data: schedule, isLoading, error } = useSchedule(scheduleId)
  const { data: employees } = useEmployees()
  const { data: preferences } = usePreferencesBySchedule(scheduleId)

  const openSchedule = useOpenSchedule()
  const closeSchedule = useCloseSchedule()
  const finalizeSchedule = useFinalizeSchedule()
  const archiveSchedule = useArchiveSchedule()
  const createShift = useCreateShift()
  const updateShift = useUpdateShift()
  const deleteShift = useDeleteShift()

  const [isShiftModalOpen, setIsShiftModalOpen] = useState(false)
  const [editingShift, setEditingShift] = useState<ShiftResponse | null>(null)
  const [deletingShift, setDeletingShift] = useState<ShiftResponse | null>(null)
  const [actionInProgress, setActionInProgress] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ShiftFormData>()

  const openCreateShiftModal = () => {
    setEditingShift(null)
    reset({ name: '', startTime: '', endTime: '', requiredAbilities: '' })
    setIsShiftModalOpen(true)
  }

  const openEditShiftModal = (shift: ShiftResponse) => {
    setEditingShift(shift)
    reset({
      name: shift.name,
      startTime: format(new Date(shift.startTime), "yyyy-MM-dd'T'HH:mm"),
      endTime: format(new Date(shift.endTime), "yyyy-MM-dd'T'HH:mm"),
      requiredAbilities: shift.requiredAbilities.join(', '),
    })
    setIsShiftModalOpen(true)
  }

  const closeShiftModal = () => {
    setIsShiftModalOpen(false)
    setEditingShift(null)
    reset()
  }

  const onSubmitShift = async (data: ShiftFormData) => {
    const request: CreateShiftRequest = {
      name: data.name,
      startTime: new Date(data.startTime).toISOString(),
      endTime: new Date(data.endTime).toISOString(),
      requiredAbilities: data.requiredAbilities
        .split(',')
        .map((a) => a.trim())
        .filter((a) => a.length > 0),
    }

    if (editingShift) {
      await updateShift.mutateAsync({
        scheduleId,
        id: editingShift.id,
        request,
      })
    } else {
      await createShift.mutateAsync({ scheduleId, request })
    }
    closeShiftModal()
  }

  const handleDeleteShift = async () => {
    if (deletingShift) {
      await deleteShift.mutateAsync({ scheduleId, id: deletingShift.id })
      setDeletingShift(null)
    }
  }

  const handleStatusAction = async (
    action: 'open' | 'close' | 'finalize' | 'archive'
  ) => {
    setActionInProgress(action)
    try {
      switch (action) {
        case 'open':
          await openSchedule.mutateAsync(scheduleId)
          break
        case 'close':
          await closeSchedule.mutateAsync(scheduleId)
          break
        case 'finalize':
          await finalizeSchedule.mutateAsync(scheduleId)
          break
        case 'archive':
          await archiveSchedule.mutateAsync(scheduleId)
          break
      }
    } finally {
      setActionInProgress(null)
    }
  }

  if (isLoading) {
    return <PageLoader />
  }

  if (error || !schedule) {
    return (
      <div className="card">
        <EmptyState
          icon={AlertCircle}
          title="Schedule not found"
          description="The schedule you're looking for doesn't exist or has been deleted."
          action={
            <Link to="/schedules" className="btn btn-primary">
              Back to Schedules
            </Link>
          }
        />
      </div>
    )
  }

  const employeeMap = new Map(employees?.map((e) => [e.id, e]) ?? [])
  const isDraft = schedule.status === ScheduleStatus.Draft
  const isFinalized = schedule.status === ScheduleStatus.Finalized

  // Get preference counts per shift
  const preferenceCountByShift = new Map<string, number>()
  preferences?.forEach((p) => {
    const count = preferenceCountByShift.get(p.shiftId) ?? 0
    preferenceCountByShift.set(p.shiftId, count + 1)
  })

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <Link
          to="/schedules"
          className="inline-flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700 mb-4"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to Schedules
        </Link>
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <div className="flex items-center gap-3">
              <h1 className="text-2xl font-bold text-gray-900">{schedule.name}</h1>
              <StatusBadge status={schedule.status} />
            </div>
            <p className="mt-1 text-sm text-gray-500">
              Week of {format(new Date(schedule.weekStartDate), 'MMMM d, yyyy')}
            </p>
          </div>
          <StatusActions
            status={schedule.status}
            onAction={handleStatusAction}
            isLoading={actionInProgress}
            shiftsCount={schedule.shifts.length}
          />
        </div>
      </div>

      {/* Status Flow */}
      <StatusFlow currentStatus={schedule.status} />

      {/* Shifts Section */}
      <div className="card">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">
            Shifts ({schedule.shifts.length})
          </h2>
          {isDraft && (
            <button onClick={openCreateShiftModal} className="btn btn-primary btn-sm">
              <Plus className="h-4 w-4" />
              Add Shift
            </button>
          )}
        </div>

        {schedule.shifts.length === 0 ? (
          <EmptyState
            icon={Clock}
            title="No shifts yet"
            description={
              isDraft
                ? 'Add shifts to this schedule before opening for preferences'
                : 'This schedule has no shifts'
            }
            action={
              isDraft && (
                <button onClick={openCreateShiftModal} className="btn btn-primary">
                  <Plus className="h-4 w-4" />
                  Add Shift
                </button>
              )
            }
          />
        ) : (
          <div className="space-y-3">
            {schedule.shifts.map((shift) => {
              const assignment = schedule.assignments.find(
                (a) => a.shiftId === shift.id
              )
              const assignedEmployee = assignment
                ? employeeMap.get(assignment.employeeId)
                : null
              const prefCount = preferenceCountByShift.get(shift.id) ?? 0

              return (
                <div
                  key={shift.id}
                  className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 bg-gray-50 rounded-lg"
                >
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <h3 className="font-medium text-gray-900">{shift.name}</h3>
                      {prefCount > 0 && (
                        <span className="badge badge-blue">
                          {prefCount} preference{prefCount !== 1 ? 's' : ''}
                        </span>
                      )}
                    </div>
                    <p className="text-sm text-gray-500 mt-1">
                      {format(new Date(shift.startTime), 'EEE, MMM d • h:mm a')} –{' '}
                      {format(new Date(shift.endTime), 'h:mm a')}
                    </p>
                    {shift.requiredAbilities.length > 0 && (
                      <div className="flex flex-wrap gap-1 mt-2">
                        {shift.requiredAbilities.map((ability) => (
                          <span
                            key={ability}
                            className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-200 text-gray-700"
                          >
                            {ability}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>

                  <div className="flex items-center gap-4">
                    {/* Assignment Status */}
                    {isFinalized || schedule.status === ScheduleStatus.Archived ? (
                      assignedEmployee ? (
                        <div className="flex items-center gap-2 px-3 py-1.5 bg-green-100 rounded-lg">
                          <Users className="h-4 w-4 text-green-600" />
                          <span className="text-sm font-medium text-green-700">
                            {assignedEmployee.name}
                          </span>
                        </div>
                      ) : (
                        <span className="text-sm text-gray-400">Unassigned</span>
                      )
                    ) : null}

                    {/* Actions */}
                    {isDraft && (
                      <div className="flex items-center gap-1">
                        <button
                          onClick={() => openEditShiftModal(shift)}
                          className="p-2 rounded hover:bg-gray-200 text-gray-400 hover:text-gray-600"
                        >
                          <Pencil className="h-4 w-4" />
                        </button>
                        <button
                          onClick={() => setDeletingShift(shift)}
                          className="p-2 rounded hover:bg-gray-200 text-gray-400 hover:text-red-600"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </div>
                    )}
                  </div>
                </div>
              )
            })}
          </div>
        )}
      </div>

      {/* Assignments Summary (when finalized) */}
      {(isFinalized || schedule.status === ScheduleStatus.Archived) &&
        schedule.assignments.length > 0 && (
          <div className="card">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">
              Assignments Summary
            </h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              {Array.from(
                schedule.assignments.reduce((acc, assignment) => {
                  const employee = employeeMap.get(assignment.employeeId)
                  if (employee) {
                    const current = acc.get(employee.id) ?? { employee, shifts: [] }
                    const shift = schedule.shifts.find(
                      (s) => s.id === assignment.shiftId
                    )
                    if (shift) {
                      current.shifts.push(shift)
                    }
                    acc.set(employee.id, current)
                  }
                  return acc
                }, new Map<string, { employee: typeof employees extends (infer T)[] | undefined ? NonNullable<T> : never; shifts: ShiftResponse[] }>())
              ).map(([employeeId, { employee, shifts }]) => (
                <div key={employeeId} className="p-4 bg-gray-50 rounded-lg">
                  <div className="flex items-center gap-3 mb-3">
                    <div className="h-8 w-8 rounded-full bg-primary-100 flex items-center justify-center">
                      <span className="text-xs font-medium text-primary-700">
                        {employee.name
                          .split(' ')
                          .map((n) => n[0])
                          .join('')
                          .toUpperCase()}
                      </span>
                    </div>
                    <div>
                      <p className="font-medium text-gray-900">{employee.name}</p>
                      <p className="text-xs text-gray-500">
                        {shifts.length} shift{shifts.length !== 1 ? 's' : ''}
                      </p>
                    </div>
                  </div>
                  <div className="space-y-1">
                    {shifts.map((shift) => (
                      <div key={shift.id} className="text-sm text-gray-600">
                        {shift.name} • {format(new Date(shift.startTime), 'EEE h:mm a')}
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

      {/* Shift Modal */}
      <Modal
        isOpen={isShiftModalOpen}
        onClose={closeShiftModal}
        title={editingShift ? 'Edit Shift' : 'Add Shift'}
      >
        <form onSubmit={handleSubmit(onSubmitShift)} className="space-y-4">
          <div>
            <label htmlFor="name" className="label">
              Shift Name
            </label>
            <input
              id="name"
              type="text"
              placeholder="e.g., Morning Shift, Night Shift"
              className={clsx('input', errors.name && 'border-red-500')}
              {...register('name', { required: 'Name is required' })}
            />
            {errors.name && (
              <p className="mt-1 text-sm text-red-500">{errors.name.message}</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label htmlFor="startTime" className="label">
                Start Time
              </label>
              <input
                id="startTime"
                type="datetime-local"
                className={clsx('input', errors.startTime && 'border-red-500')}
                {...register('startTime', { required: 'Start time is required' })}
              />
              {errors.startTime && (
                <p className="mt-1 text-sm text-red-500">{errors.startTime.message}</p>
              )}
            </div>
            <div>
              <label htmlFor="endTime" className="label">
                End Time
              </label>
              <input
                id="endTime"
                type="datetime-local"
                className={clsx('input', errors.endTime && 'border-red-500')}
                {...register('endTime', { required: 'End time is required' })}
              />
              {errors.endTime && (
                <p className="mt-1 text-sm text-red-500">{errors.endTime.message}</p>
              )}
            </div>
          </div>

          <div>
            <label htmlFor="requiredAbilities" className="label">
              Required Abilities
            </label>
            <input
              id="requiredAbilities"
              type="text"
              placeholder="e.g., cashier, stock"
              className="input"
              {...register('requiredAbilities')}
            />
            <p className="mt-1 text-xs text-gray-500">
              Comma-separated list of required skills
            </p>
          </div>

          <div className="flex justify-end gap-3 pt-4">
            <button type="button" className="btn btn-secondary" onClick={closeShiftModal}>
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={createShift.isPending || updateShift.isPending}
            >
              {createShift.isPending || updateShift.isPending
                ? 'Saving...'
                : editingShift
                ? 'Update'
                : 'Add Shift'}
            </button>
          </div>
        </form>
      </Modal>

      {/* Delete Shift Confirmation */}
      <ConfirmDialog
        isOpen={!!deletingShift}
        onClose={() => setDeletingShift(null)}
        onConfirm={handleDeleteShift}
        title="Delete Shift"
        message={`Are you sure you want to delete "${deletingShift?.name}"? This action cannot be undone.`}
        isLoading={deleteShift.isPending}
      />
    </div>
  )
}

interface StatusActionsProps {
  status: ScheduleStatus
  onAction: (action: 'open' | 'close' | 'finalize' | 'archive') => void
  isLoading: string | null
  shiftsCount: number
}

function StatusActions({ status, onAction, isLoading, shiftsCount }: StatusActionsProps) {
  switch (status) {
    case ScheduleStatus.Draft:
      return (
        <button
          onClick={() => onAction('open')}
          className="btn btn-primary"
          disabled={isLoading === 'open' || shiftsCount === 0}
          title={shiftsCount === 0 ? 'Add shifts before opening' : undefined}
        >
          {isLoading === 'open' ? (
            'Opening...'
          ) : (
            <>
              <Play className="h-4 w-4" />
              Open for Preferences
            </>
          )}
        </button>
      )
    case ScheduleStatus.OpenForPreferences:
      return (
        <button
          onClick={() => onAction('close')}
          className="btn btn-secondary"
          disabled={isLoading === 'close'}
        >
          {isLoading === 'close' ? (
            'Closing...'
          ) : (
            <>
              <Square className="h-4 w-4" />
              Close Preferences
            </>
          )}
        </button>
      )
    case ScheduleStatus.PendingReview:
      return (
        <button
          onClick={() => onAction('finalize')}
          className="btn btn-primary"
          disabled={isLoading === 'finalize'}
        >
          {isLoading === 'finalize' ? (
            'Finalizing...'
          ) : (
            <>
              <CheckCircle className="h-4 w-4" />
              Finalize Schedule
            </>
          )}
        </button>
      )
    case ScheduleStatus.Finalized:
      return (
        <button
          onClick={() => onAction('archive')}
          className="btn btn-secondary"
          disabled={isLoading === 'archive'}
        >
          {isLoading === 'archive' ? (
            'Archiving...'
          ) : (
            <>
              <Archive className="h-4 w-4" />
              Archive
            </>
          )}
        </button>
      )
    default:
      return null
  }
}

interface StatusFlowProps {
  currentStatus: ScheduleStatus
}

function StatusFlow({ currentStatus }: StatusFlowProps) {
  const steps = [
    { status: ScheduleStatus.Draft, label: 'Draft' },
    { status: ScheduleStatus.OpenForPreferences, label: 'Preferences' },
    { status: ScheduleStatus.PendingReview, label: 'Review' },
    { status: ScheduleStatus.Finalized, label: 'Finalized' },
    { status: ScheduleStatus.Archived, label: 'Archived' },
  ]

  const currentIndex = steps.findIndex((s) => s.status === currentStatus)

  return (
    <div className="card">
      <div className="flex items-center justify-between">
        {steps.map((step, index) => (
          <div key={step.status} className="flex items-center flex-1">
            <div className="flex flex-col items-center">
              <div
                className={clsx(
                  'w-8 h-8 rounded-full flex items-center justify-center text-sm font-medium',
                  index < currentIndex
                    ? 'bg-primary-600 text-white'
                    : index === currentIndex
                    ? 'bg-primary-600 text-white ring-4 ring-primary-100'
                    : 'bg-gray-200 text-gray-500'
                )}
              >
                {index < currentIndex ? '✓' : index + 1}
              </div>
              <span
                className={clsx(
                  'text-xs mt-2',
                  index <= currentIndex ? 'text-primary-600 font-medium' : 'text-gray-500'
                )}
              >
                {step.label}
              </span>
            </div>
            {index < steps.length - 1 && (
              <div
                className={clsx(
                  'flex-1 h-0.5 mx-2',
                  index < currentIndex ? 'bg-primary-600' : 'bg-gray-200'
                )}
              />
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
