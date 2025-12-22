import { createFileRoute, Link } from '@tanstack/react-router'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import {
  Calendar,
  Users,
  Clock,
  CheckCircle,
  CalendarDays,
  HandHeart,
  Briefcase,
  CalendarOff,
  ThumbsUp,
  ThumbsDown,
  AlertCircle,
  Plus,
  Trash2,
} from 'lucide-react'
import { useSchedules } from '@/hooks'
import { useEmployees } from '@/hooks'
import {
  useShiftRequestsByEmployee,
  useTimeOffRequestsByEmployee,
  useCreateTimeOffRequest,
  useDeleteShiftRequest,
  useDeleteTimeOffRequest,
  usePendingShiftRequestsByManager,
  usePendingTimeOffRequestsByManager,
} from '@/hooks'
import { ScheduleStatus, RequestStatus, ShiftRequestType } from '@/api'
import { StatusBadge, PageLoader, Modal, ConfirmDialog } from '@/components'
import { useCurrentUser } from '@/context'
import { format } from 'date-fns'
import clsx from 'clsx'

export const Route = createFileRoute('/')({
  component: Dashboard,
})

function Dashboard() {
  const { data: schedules, isLoading: schedulesLoading } = useSchedules()
  const { data: employees, isLoading: employeesLoading } = useEmployees()
  const { isManager, isLoading: userLoading } = useCurrentUser()

  if (schedulesLoading || employeesLoading || userLoading) {
    return <PageLoader />
  }

  // Show different dashboard based on role
  if (isManager) {
    return <ManagerDashboard schedules={schedules ?? []} employees={employees ?? []} />
  }

  return <EmployeeDashboard schedules={schedules ?? []} />
}

// Manager sees the full overview dashboard
function ManagerDashboard({ 
  schedules, 
  employees 
}: { 
  schedules: { id: string; name: string; weekStartDate: string; status: ScheduleStatus }[]
  employees: { id: string; name: string }[]
}) {
  const { currentUser } = useCurrentUser()
  const { data: pendingShiftRequests } = usePendingShiftRequestsByManager(currentUser?.id)
  const { data: pendingTimeOffRequests } = usePendingTimeOffRequestsByManager(currentUser?.id)
  
  const activeSchedules = schedules.filter((s) => s.status !== ScheduleStatus.Archived)
  const draftSchedules = schedules.filter((s) => s.status === ScheduleStatus.Draft)

  const totalPendingRequests = (pendingShiftRequests?.length ?? 0) + (pendingTimeOffRequests?.length ?? 0)

  const stats = [
    {
      name: 'Total Employees',
      value: employees.length,
      icon: Users,
      color: 'bg-blue-500',
    },
    {
      name: 'Active Schedules',
      value: activeSchedules.length,
      icon: Calendar,
      color: 'bg-green-500',
    },
    {
      name: 'Draft Schedules',
      value: draftSchedules.length,
      icon: Clock,
      color: 'bg-yellow-500',
    },
    {
      name: 'Pending Requests',
      value: totalPendingRequests,
      icon: AlertCircle,
      color: totalPendingRequests > 0 ? 'bg-orange-500' : 'bg-gray-400',
    },
  ]

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <p className="mt-1 text-sm text-gray-500">
          Overview of your shift scheduling system
        </p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {stats.map((stat) => (
          <div key={stat.name} className="card">
            <div className="flex items-center gap-4">
              <div className={`p-3 rounded-lg ${stat.color}`}>
                <stat.icon className="h-6 w-6 text-white" />
              </div>
              <div>
                <p className="text-sm text-gray-500">{stat.name}</p>
                <p className="text-2xl font-semibold text-gray-900">{stat.value}</p>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Pending Requests Alert */}
      {totalPendingRequests > 0 && (
        <div className="card border-2 border-orange-200 bg-orange-50/50">
          <div className="flex items-center gap-3 mb-4">
            <div className="p-2 rounded-lg bg-orange-100">
              <AlertCircle className="h-5 w-5 text-orange-600" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-gray-900">Pending Requests</h2>
              <p className="text-sm text-gray-600">
                You have {totalPendingRequests} request{totalPendingRequests !== 1 ? 's' : ''} waiting for your review
              </p>
            </div>
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Pending Shift Requests */}
            {pendingShiftRequests && pendingShiftRequests.length > 0 && (
              <div className="bg-white rounded-lg border border-orange-200 p-4">
                <div className="flex items-center gap-2 mb-3">
                  <Briefcase className="h-4 w-4 text-blue-600" />
                  <h3 className="font-medium text-gray-900">Shift Requests ({pendingShiftRequests.length})</h3>
                </div>
                <div className="space-y-2">
                  {pendingShiftRequests.slice(0, 3).map((request) => (
                    <Link
                      key={request.id}
                      to="/schedules/$scheduleId"
                      params={{ scheduleId: request.scheduleId }}
                      className="flex items-center justify-between p-2 rounded hover:bg-gray-50 transition-colors"
                    >
                      <div>
                        <p className="text-sm font-medium text-gray-900">{request.employeeName}</p>
                        <p className="text-xs text-gray-500">
                          {request.requestType === ShiftRequestType.WantToWork ? 'Wants' : "Doesn't want"} {request.shiftName}
                        </p>
                      </div>
                      <span className="text-xs text-primary-600">Review →</span>
                    </Link>
                  ))}
                  {pendingShiftRequests.length > 3 && (
                    <p className="text-xs text-gray-500 text-center pt-2">
                      +{pendingShiftRequests.length - 3} more
                    </p>
                  )}
                </div>
              </div>
            )}

            {/* Pending Time Off Requests */}
            {pendingTimeOffRequests && pendingTimeOffRequests.length > 0 && (
              <div className="bg-white rounded-lg border border-orange-200 p-4">
                <div className="flex items-center gap-2 mb-3">
                  <CalendarOff className="h-4 w-4 text-orange-600" />
                  <h3 className="font-medium text-gray-900">Time Off Requests ({pendingTimeOffRequests.length})</h3>
                </div>
                <div className="space-y-2">
                  {pendingTimeOffRequests.slice(0, 3).map((request) => (
                    <div
                      key={request.id}
                      className="flex items-center justify-between p-2 rounded bg-gray-50"
                    >
                      <div>
                        <p className="text-sm font-medium text-gray-900">{request.employeeName}</p>
                        <p className="text-xs text-gray-500">
                          {format(new Date(request.startDate), 'MMM d')} – {format(new Date(request.endDate), 'MMM d')}
                        </p>
                      </div>
                      <span className="badge badge-yellow">Pending</span>
                    </div>
                  ))}
                  {pendingTimeOffRequests.length > 3 && (
                    <p className="text-xs text-gray-500 text-center pt-2">
                      +{pendingTimeOffRequests.length - 3} more
                    </p>
                  )}
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Recent Schedules */}
      <div className="card">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">Recent Schedules</h2>
          <Link to="/schedules" className="text-sm text-primary-600 hover:text-primary-700">
            View all →
          </Link>
        </div>
        {activeSchedules.length === 0 ? (
          <p className="text-sm text-gray-500 py-4 text-center">
            No schedules yet.{' '}
            <Link to="/schedules" className="text-primary-600 hover:underline">
              Create your first schedule
            </Link>
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-gray-200">
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Name
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Week Start
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Status
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {activeSchedules.slice(0, 5).map((schedule) => (
                  <tr key={schedule.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3 text-sm font-medium text-gray-900">
                      {schedule.name}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-500">
                      {format(new Date(schedule.weekStartDate), 'MMM d, yyyy')}
                    </td>
                    <td className="px-4 py-3">
                      <StatusBadge status={schedule.status} />
                    </td>
                    <td className="px-4 py-3 text-right">
                      <Link
                        to="/schedules/$scheduleId"
                        params={{ scheduleId: schedule.id }}
                        className="text-sm text-primary-600 hover:text-primary-700"
                      >
                        View
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Quick Actions */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Link
          to="/employees"
          className="card hover:border-primary-300 hover:shadow-md transition-all group"
        >
          <div className="flex items-center gap-4">
            <div className="p-3 rounded-lg bg-blue-100 group-hover:bg-blue-200 transition-colors">
              <Users className="h-6 w-6 text-blue-600" />
            </div>
            <div>
              <h3 className="font-semibold text-gray-900">Manage Employees</h3>
              <p className="text-sm text-gray-500">Add, edit, or remove team members</p>
            </div>
          </div>
        </Link>
        <Link
          to="/schedules"
          className="card hover:border-primary-300 hover:shadow-md transition-all group"
        >
          <div className="flex items-center gap-4">
            <div className="p-3 rounded-lg bg-green-100 group-hover:bg-green-200 transition-colors">
              <Calendar className="h-6 w-6 text-green-600" />
            </div>
            <div>
              <h3 className="font-semibold text-gray-900">Manage Schedules</h3>
              <p className="text-sm text-gray-500">Create and manage shift schedules</p>
            </div>
          </div>
        </Link>
      </div>
    </div>
  )
}

interface TimeOffFormData {
  startDate: string
  endDate: string
  reason: string
}

// Employee sees a simpler dashboard focused on their schedules and preferences
function EmployeeDashboard({ 
  schedules,
}: { 
  schedules: { id: string; name: string; weekStartDate: string; status: ScheduleStatus }[]
}) {
  const { currentUser } = useCurrentUser()
  const { data: shiftRequests, isLoading: shiftRequestsLoading } = useShiftRequestsByEmployee(currentUser?.id)
  const { data: timeOffRequests, isLoading: timeOffRequestsLoading } = useTimeOffRequestsByEmployee(currentUser?.id)
  
  const createTimeOffRequest = useCreateTimeOffRequest()
  const deleteShiftRequest = useDeleteShiftRequest()
  const deleteTimeOffRequest = useDeleteTimeOffRequest()
  
  const [isTimeOffModalOpen, setIsTimeOffModalOpen] = useState(false)
  const [deletingShiftRequest, setDeletingShiftRequest] = useState<string | null>(null)
  const [deletingTimeOffRequest, setDeletingTimeOffRequest] = useState<string | null>(null)
  
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<TimeOffFormData>()

  const openForPreferences = schedules.filter(
    (s) => s.status === ScheduleStatus.OpenForPreferences
  )
  const finalizedSchedules = schedules.filter(
    (s) => s.status === ScheduleStatus.Finalized
  )

  const pendingShiftRequests = shiftRequests?.filter(r => r.status === RequestStatus.Pending) ?? []
  const pendingTimeOffRequests = timeOffRequests?.filter(r => r.status === RequestStatus.Pending) ?? []

  const onSubmitTimeOff = async (data: TimeOffFormData) => {
    if (!currentUser) return
    await createTimeOffRequest.mutateAsync({
      employeeId: currentUser.id,
      startDate: new Date(data.startDate).toISOString(),
      endDate: new Date(data.endDate).toISOString(),
      reason: data.reason || null,
    })
    setIsTimeOffModalOpen(false)
    reset()
  }

  const handleDeleteShiftRequest = async () => {
    if (deletingShiftRequest) {
      await deleteShiftRequest.mutateAsync(deletingShiftRequest)
      setDeletingShiftRequest(null)
    }
  }

  const handleDeleteTimeOffRequest = async () => {
    if (deletingTimeOffRequest) {
      await deleteTimeOffRequest.mutateAsync(deletingTimeOffRequest)
      setDeletingTimeOffRequest(null)
    }
  }

  const getStatusBadge = (status: RequestStatus) => {
    switch (status) {
      case RequestStatus.Pending:
        return <span className="badge badge-yellow">Pending</span>
      case RequestStatus.Approved:
        return <span className="badge badge-green">Approved</span>
      case RequestStatus.Rejected:
        return <span className="badge badge-red">Rejected</span>
    }
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">My Dashboard</h1>
        <p className="mt-1 text-sm text-gray-500">
          View your schedules, requests, and submit shift preferences
        </p>
      </div>

      {/* Open for Preferences - Priority Section */}
      {openForPreferences.length > 0 && (
        <div className="card border-2 border-primary-200 bg-primary-50/50">
          <div className="flex items-center gap-3 mb-4">
            <div className="p-2 rounded-lg bg-primary-100">
              <HandHeart className="h-5 w-5 text-primary-600" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-gray-900">Submit Your Preferences</h2>
              <p className="text-sm text-gray-600">
                These schedules are open – click to claim shifts you'd like to work
              </p>
            </div>
          </div>
          <div className="space-y-3">
            {openForPreferences.map((schedule) => (
              <Link
                key={schedule.id}
                to="/schedules/$scheduleId"
                params={{ scheduleId: schedule.id }}
                className="flex items-center justify-between p-4 bg-white rounded-lg border border-primary-200 hover:border-primary-300 hover:shadow-sm transition-all"
              >
                <div>
                  <h3 className="font-medium text-gray-900">{schedule.name}</h3>
                  <p className="text-sm text-gray-500">
                    Week of {format(new Date(schedule.weekStartDate), 'MMM d, yyyy')}
                  </p>
                </div>
                <span className="text-sm font-medium text-primary-600">
                  Submit preferences →
                </span>
              </Link>
            ))}
          </div>
        </div>
      )}

      {/* My Requests Section */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Shift Requests */}
        <div className="card">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-blue-100">
                <Briefcase className="h-5 w-5 text-blue-600" />
              </div>
              <h2 className="text-lg font-semibold text-gray-900">My Shift Requests</h2>
            </div>
            {pendingShiftRequests.length > 0 && (
              <span className="badge badge-blue">{pendingShiftRequests.length} pending</span>
            )}
          </div>
          
          {shiftRequestsLoading ? (
            <div className="py-8 flex justify-center">
              <div className="animate-spin h-6 w-6 border-2 border-primary-600 border-t-transparent rounded-full" />
            </div>
          ) : !shiftRequests || shiftRequests.length === 0 ? (
            <div className="py-8 text-center">
              <Briefcase className="h-12 w-12 text-gray-300 mx-auto mb-3" />
              <p className="text-sm text-gray-500">No shift requests yet.</p>
              <p className="text-xs text-gray-400 mt-1">
                Go to a schedule to request shifts you'd like to work
              </p>
            </div>
          ) : (
            <div className="space-y-3 max-h-80 overflow-y-auto">
              {shiftRequests.slice(0, 10).map((request) => (
                <div
                  key={request.id}
                  className={clsx(
                    "p-3 rounded-lg border",
                    request.status === RequestStatus.Approved && "bg-green-50 border-green-200",
                    request.status === RequestStatus.Rejected && "bg-red-50 border-red-200",
                    request.status === RequestStatus.Pending && "bg-gray-50 border-gray-200"
                  )}
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        {request.requestType === ShiftRequestType.WantToWork ? (
                          <ThumbsUp className="h-4 w-4 text-green-600 flex-shrink-0" />
                        ) : (
                          <ThumbsDown className="h-4 w-4 text-red-600 flex-shrink-0" />
                        )}
                        <span className="font-medium text-gray-900 truncate">{request.shiftName}</span>
                        {getStatusBadge(request.status)}
                      </div>
                      <p className="text-xs text-gray-500 mt-1">
                        {format(new Date(request.shiftStartTime), 'EEE, MMM d • h:mm a')}
                      </p>
                      <p className="text-xs text-gray-400">{request.scheduleName}</p>
                      {request.note && (
                        <p className="text-xs text-gray-600 mt-1 italic">"{request.note}"</p>
                      )}
                      {request.reviewNote && (
                        <p className="text-xs text-primary-600 mt-1">
                          Manager: {request.reviewNote}
                        </p>
                      )}
                    </div>
                    {request.status === RequestStatus.Pending && (
                      <button
                        onClick={() => setDeletingShiftRequest(request.id)}
                        className="p-1 rounded hover:bg-gray-200 text-gray-400 hover:text-red-600"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Time Off Requests */}
        <div className="card">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-orange-100">
                <CalendarOff className="h-5 w-5 text-orange-600" />
              </div>
              <h2 className="text-lg font-semibold text-gray-900">Time Off Requests</h2>
            </div>
            <button
              onClick={() => setIsTimeOffModalOpen(true)}
              className="btn btn-sm btn-primary"
            >
              <Plus className="h-4 w-4" />
              Request Time Off
            </button>
          </div>
          
          {timeOffRequestsLoading ? (
            <div className="py-8 flex justify-center">
              <div className="animate-spin h-6 w-6 border-2 border-primary-600 border-t-transparent rounded-full" />
            </div>
          ) : !timeOffRequests || timeOffRequests.length === 0 ? (
            <div className="py-8 text-center">
              <CalendarOff className="h-12 w-12 text-gray-300 mx-auto mb-3" />
              <p className="text-sm text-gray-500">No time off requests.</p>
              <button
                onClick={() => setIsTimeOffModalOpen(true)}
                className="text-sm text-primary-600 hover:text-primary-700 mt-2"
              >
                Request time off →
              </button>
            </div>
          ) : (
            <div className="space-y-3 max-h-80 overflow-y-auto">
              {timeOffRequests.slice(0, 10).map((request) => (
                <div
                  key={request.id}
                  className={clsx(
                    "p-3 rounded-lg border",
                    request.status === RequestStatus.Approved && "bg-green-50 border-green-200",
                    request.status === RequestStatus.Rejected && "bg-red-50 border-red-200",
                    request.status === RequestStatus.Pending && "bg-gray-50 border-gray-200"
                  )}
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="font-medium text-gray-900">
                          {format(new Date(request.startDate), 'MMM d')} – {format(new Date(request.endDate), 'MMM d, yyyy')}
                        </span>
                        {getStatusBadge(request.status)}
                      </div>
                      {request.reason && (
                        <p className="text-xs text-gray-600 mt-1">Reason: {request.reason}</p>
                      )}
                      {request.reviewNote && (
                        <p className="text-xs text-primary-600 mt-1">
                          Manager: {request.reviewNote}
                        </p>
                      )}
                      <p className="text-xs text-gray-400 mt-1">
                        Requested {format(new Date(request.createdAt), 'MMM d, yyyy')}
                      </p>
                    </div>
                    {request.status === RequestStatus.Pending && (
                      <button
                        onClick={() => setDeletingTimeOffRequest(request.id)}
                        className="p-1 rounded hover:bg-gray-200 text-gray-400 hover:text-red-600"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Stats for Employee */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <div className="card">
          <div className="flex items-center gap-4">
            <div className="p-3 rounded-lg bg-green-500">
              <CalendarDays className="h-6 w-6 text-white" />
            </div>
            <div>
              <p className="text-sm text-gray-500">Open for Preferences</p>
              <p className="text-2xl font-semibold text-gray-900">{openForPreferences.length}</p>
            </div>
          </div>
        </div>
        <div className="card">
          <div className="flex items-center gap-4">
            <div className="p-3 rounded-lg bg-purple-500">
              <CheckCircle className="h-6 w-6 text-white" />
            </div>
            <div>
              <p className="text-sm text-gray-500">Published Schedules</p>
              <p className="text-2xl font-semibold text-gray-900">{finalizedSchedules.length}</p>
            </div>
          </div>
        </div>
        <div className="card">
          <div className="flex items-center gap-4">
            <div className="p-3 rounded-lg bg-blue-500">
              <Briefcase className="h-6 w-6 text-white" />
            </div>
            <div>
              <p className="text-sm text-gray-500">Pending Shift Requests</p>
              <p className="text-2xl font-semibold text-gray-900">{pendingShiftRequests.length}</p>
            </div>
          </div>
        </div>
        <div className="card">
          <div className="flex items-center gap-4">
            <div className="p-3 rounded-lg bg-orange-500">
              <CalendarOff className="h-6 w-6 text-white" />
            </div>
            <div>
              <p className="text-sm text-gray-500">Pending Time Off</p>
              <p className="text-2xl font-semibold text-gray-900">{pendingTimeOffRequests.length}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Published Schedules */}
      <div className="card">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">Published Schedules</h2>
          <Link to="/schedules" className="text-sm text-primary-600 hover:text-primary-700">
            View all →
          </Link>
        </div>
        {finalizedSchedules.length === 0 ? (
          <p className="text-sm text-gray-500 py-4 text-center">
            No published schedules yet. Check back later!
          </p>
        ) : (
          <div className="space-y-2">
            {finalizedSchedules.slice(0, 5).map((schedule) => (
              <Link
                key={schedule.id}
                to="/schedules/$scheduleId"
                params={{ scheduleId: schedule.id }}
                className="flex items-center justify-between p-3 rounded-lg hover:bg-gray-50 transition-colors"
              >
                <div className="flex items-center gap-3">
                  <Calendar className="h-5 w-5 text-gray-400" />
                  <div>
                    <h3 className="text-sm font-medium text-gray-900">{schedule.name}</h3>
                    <p className="text-xs text-gray-500">
                      {format(new Date(schedule.weekStartDate), 'MMM d, yyyy')}
                    </p>
                  </div>
                </div>
                <StatusBadge status={schedule.status} />
              </Link>
            ))}
          </div>
        )}
      </div>

      {/* Quick Action */}
      <Link
        to="/schedules"
        className="card hover:border-primary-300 hover:shadow-md transition-all group"
      >
        <div className="flex items-center gap-4">
          <div className="p-3 rounded-lg bg-green-100 group-hover:bg-green-200 transition-colors">
            <Calendar className="h-6 w-6 text-green-600" />
          </div>
          <div>
            <h3 className="font-semibold text-gray-900">View All Schedules</h3>
            <p className="text-sm text-gray-500">See all schedules and your shift assignments</p>
          </div>
        </div>
      </Link>

      {/* Time Off Request Modal */}
      <Modal
        isOpen={isTimeOffModalOpen}
        onClose={() => {
          setIsTimeOffModalOpen(false)
          reset()
        }}
        title="Request Time Off"
      >
        <form onSubmit={handleSubmit(onSubmitTimeOff)} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label htmlFor="startDate" className="label">
                Start Date
              </label>
              <input
                id="startDate"
                type="date"
                className={clsx('input', errors.startDate && 'border-red-500')}
                {...register('startDate', { required: 'Start date is required' })}
              />
              {errors.startDate && (
                <p className="mt-1 text-sm text-red-500">{errors.startDate.message}</p>
              )}
            </div>
            <div>
              <label htmlFor="endDate" className="label">
                End Date
              </label>
              <input
                id="endDate"
                type="date"
                className={clsx('input', errors.endDate && 'border-red-500')}
                {...register('endDate', { required: 'End date is required' })}
              />
              {errors.endDate && (
                <p className="mt-1 text-sm text-red-500">{errors.endDate.message}</p>
              )}
            </div>
          </div>

          <div>
            <label htmlFor="reason" className="label">
              Reason (optional)
            </label>
            <textarea
              id="reason"
              rows={3}
              className="input"
              placeholder="e.g., Family vacation, medical appointment"
              {...register('reason')}
            />
          </div>

          <div className="flex justify-end gap-3 pt-4">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => {
                setIsTimeOffModalOpen(false)
                reset()
              }}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={createTimeOffRequest.isPending}
            >
              {createTimeOffRequest.isPending ? 'Submitting...' : 'Submit Request'}
            </button>
          </div>
        </form>
      </Modal>

      {/* Delete Shift Request Confirmation */}
      <ConfirmDialog
        isOpen={!!deletingShiftRequest}
        onClose={() => setDeletingShiftRequest(null)}
        onConfirm={handleDeleteShiftRequest}
        title="Cancel Shift Request"
        message="Are you sure you want to cancel this shift request? This action cannot be undone."
        isLoading={deleteShiftRequest.isPending}
      />

      {/* Delete Time Off Request Confirmation */}
      <ConfirmDialog
        isOpen={!!deletingTimeOffRequest}
        onClose={() => setDeletingTimeOffRequest(null)}
        onConfirm={handleDeleteTimeOffRequest}
        title="Cancel Time Off Request"
        message="Are you sure you want to cancel this time off request? This action cannot be undone."
        isLoading={deleteTimeOffRequest.isPending}
      />
    </div>
  )
}
