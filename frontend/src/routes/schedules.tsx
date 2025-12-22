import { createFileRoute, Link } from '@tanstack/react-router'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Plus, Calendar, Trash2, ChevronRight } from 'lucide-react'
import { format } from 'date-fns'
import {
  useSchedules,
  useCreateSchedule,
  useDeleteSchedule,
} from '@/hooks'
import {
  Modal,
  ConfirmDialog,
  EmptyState,
  PageLoader,
  StatusBadge,
} from '@/components'
import { useCurrentUser } from '@/context'
import { CreateScheduleRequest, ScheduleResponse, ScheduleStatus } from '@/api'
import clsx from 'clsx'

export const Route = createFileRoute('/schedules')({
  component: SchedulesPage,
})

interface ScheduleFormData {
  name: string
  weekStartDate: string
}

function SchedulesPage() {
  const { isManager, isLoading: userLoading } = useCurrentUser()
  const { data: schedules, isLoading } = useSchedules()
  const createSchedule = useCreateSchedule()
  const deleteSchedule = useDeleteSchedule()

  const [isModalOpen, setIsModalOpen] = useState(false)
  const [deletingSchedule, setDeletingSchedule] = useState<ScheduleResponse | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ScheduleFormData>()

  const openCreateModal = () => {
    reset({ name: '', weekStartDate: '' })
    setIsModalOpen(true)
  }

  const closeModal = () => {
    setIsModalOpen(false)
    reset()
  }

  const onSubmit = async (data: ScheduleFormData) => {
    const request: CreateScheduleRequest = {
      name: data.name,
      weekStartDate: new Date(data.weekStartDate).toISOString(),
    }
    await createSchedule.mutateAsync(request)
    closeModal()
  }

  const handleDelete = async () => {
    if (deletingSchedule) {
      await deleteSchedule.mutateAsync(deletingSchedule.id)
      setDeletingSchedule(null)
    }
  }

  if (isLoading || userLoading) {
    return <PageLoader />
  }

  // Group schedules by status
  const groupedSchedules = {
    active: schedules?.filter(
      (s) => s.status !== ScheduleStatus.Archived
    ) ?? [],
    archived: schedules?.filter(
      (s) => s.status === ScheduleStatus.Archived
    ) ?? [],
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Schedules</h1>
          <p className="mt-1 text-sm text-gray-500">
            {isManager 
              ? 'Create and manage your shift schedules'
              : 'View schedules and submit your preferences'}
          </p>
        </div>
        {isManager && (
          <button onClick={openCreateModal} className="btn btn-primary">
            <Plus className="h-4 w-4" />
            New Schedule
          </button>
        )}
      </div>

      {schedules?.length === 0 ? (
        <div className="card">
          <EmptyState
            icon={Calendar}
            title="No schedules yet"
            description={isManager 
              ? "Create your first schedule to start managing shifts"
              : "No schedules available. Check back later!"}
            action={isManager && (
              <button onClick={openCreateModal} className="btn btn-primary">
                <Plus className="h-4 w-4" />
                Create Schedule
              </button>
            )}
          />
        </div>
      ) : (
        <div className="space-y-6">
          {/* Active Schedules */}
          <div className="card p-0 overflow-hidden">
            <div className="px-6 py-4 border-b border-gray-200 bg-gray-50">
              <h2 className="font-semibold text-gray-900">Active Schedules</h2>
            </div>
            {groupedSchedules.active.length === 0 ? (
              <div className="px-6 py-8 text-center text-sm text-gray-500">
                No active schedules
              </div>
            ) : (
              <div className="divide-y divide-gray-200">
                {groupedSchedules.active.map((schedule) => (
                  <ScheduleRow
                    key={schedule.id}
                    schedule={schedule}
                    onDelete={isManager ? () => setDeletingSchedule(schedule) : undefined}
                  />
                ))}
              </div>
            )}
          </div>

          {/* Archived Schedules */}
          {groupedSchedules.archived.length > 0 && (
            <div className="card p-0 overflow-hidden">
              <div className="px-6 py-4 border-b border-gray-200 bg-gray-50">
                <h2 className="font-semibold text-gray-900">Archived Schedules</h2>
              </div>
              <div className="divide-y divide-gray-200">
                {groupedSchedules.archived.map((schedule) => (
                  <ScheduleRow key={schedule.id} schedule={schedule} />
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      {/* Create Modal */}
      <Modal isOpen={isModalOpen} onClose={closeModal} title="Create Schedule">
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label htmlFor="name" className="label">
              Schedule Name
            </label>
            <input
              id="name"
              type="text"
              placeholder="e.g., Week 52 - Holiday Schedule"
              className={clsx('input', errors.name && 'border-red-500')}
              {...register('name', { required: 'Name is required' })}
            />
            {errors.name && (
              <p className="mt-1 text-sm text-red-500">{errors.name.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="weekStartDate" className="label">
              Week Start Date
            </label>
            <input
              id="weekStartDate"
              type="date"
              className={clsx('input', errors.weekStartDate && 'border-red-500')}
              {...register('weekStartDate', { required: 'Start date is required' })}
            />
            {errors.weekStartDate && (
              <p className="mt-1 text-sm text-red-500">{errors.weekStartDate.message}</p>
            )}
          </div>

          <div className="flex justify-end gap-3 pt-4">
            <button type="button" className="btn btn-secondary" onClick={closeModal}>
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={createSchedule.isPending}
            >
              {createSchedule.isPending ? 'Creating...' : 'Create Schedule'}
            </button>
          </div>
        </form>
      </Modal>

      {/* Delete Confirmation */}
      <ConfirmDialog
        isOpen={!!deletingSchedule}
        onClose={() => setDeletingSchedule(null)}
        onConfirm={handleDelete}
        title="Delete Schedule"
        message={`Are you sure you want to delete "${deletingSchedule?.name}"? This action cannot be undone.`}
        isLoading={deleteSchedule.isPending}
      />
    </div>
  )
}

interface ScheduleRowProps {
  schedule: ScheduleResponse
  onDelete?: () => void
}

function ScheduleRow({ schedule, onDelete }: ScheduleRowProps) {
  const canDelete = schedule.status === ScheduleStatus.Draft

  return (
    <Link
      to="/schedules/$scheduleId"
      params={{ scheduleId: schedule.id }}
      className="flex items-center justify-between px-6 py-4 hover:bg-gray-50 transition-colors"
    >
      <div className="flex items-center gap-4 min-w-0">
        <div className="flex-shrink-0 h-10 w-10 rounded-lg bg-primary-100 flex items-center justify-center">
          <Calendar className="h-5 w-5 text-primary-600" />
        </div>
        <div className="min-w-0">
          <h3 className="text-sm font-medium text-gray-900 truncate">{schedule.name}</h3>
          <p className="text-sm text-gray-500">
            Week of {format(new Date(schedule.weekStartDate), 'MMM d, yyyy')}
          </p>
        </div>
      </div>
      <div className="flex items-center gap-4">
        <StatusBadge status={schedule.status} />
        {canDelete && onDelete && (
          <button
            onClick={(e) => {
              e.preventDefault()
              e.stopPropagation()
              onDelete()
            }}
            className="p-1 rounded hover:bg-gray-200 text-gray-400 hover:text-red-600"
          >
            <Trash2 className="h-4 w-4" />
          </button>
        )}
        <ChevronRight className="h-5 w-5 text-gray-400" />
      </div>
    </Link>
  )
}
