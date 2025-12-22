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
  Heart,
  Ban,
  ThumbsUp,
  ThumbsDown,
  MessageSquare,
  Check,
  X,
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
  useCreatePreference,
  useDeletePreference,
  useShiftRequestsBySchedule,
  useCreateShiftRequest,
  useDeleteShiftRequest,
  useReviewShiftRequest,
} from '@/hooks'
import {
  Modal,
  ConfirmDialog,
  EmptyState,
  PageLoader,
  StatusBadge,
} from '@/components'
import { useCurrentUser } from '@/context'
import {
  ScheduleStatus,
  CreateShiftRequest,
  ShiftResponse,
  PreferenceType,
  PreferenceResponse,
  ShiftRequestType,
  RequestStatus,
  ShiftRequestResponse,
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
  const { currentUser, isManager, isLoading: userLoading } = useCurrentUser()
  const { data: schedule, isLoading, error } = useSchedule(scheduleId)
  const { data: employees } = useEmployees()
  const { data: preferences } = usePreferencesBySchedule(scheduleId)
  const { data: shiftRequests } = useShiftRequestsBySchedule(scheduleId)

  const openSchedule = useOpenSchedule()
  const closeSchedule = useCloseSchedule()
  const finalizeSchedule = useFinalizeSchedule()
  const archiveSchedule = useArchiveSchedule()
  const createShift = useCreateShift()
  const updateShift = useUpdateShift()
  const deleteShift = useDeleteShift()
  const createPreference = useCreatePreference()
  const deletePreference = useDeletePreference()
  const createShiftRequest = useCreateShiftRequest()
  const deleteShiftRequest = useDeleteShiftRequest()
  const reviewShiftRequest = useReviewShiftRequest()

  const [isShiftModalOpen, setIsShiftModalOpen] = useState(false)
  const [editingShift, setEditingShift] = useState<ShiftResponse | null>(null)
  const [deletingShift, setDeletingShift] = useState<ShiftResponse | null>(null)
  const [actionInProgress, setActionInProgress] = useState<string | null>(null)
  const [preferenceInProgress, setPreferenceInProgress] = useState<string | null>(null)
  const [requestInProgress, setRequestInProgress] = useState<string | null>(null)
  const [shiftRequestModal, setShiftRequestModal] = useState<{ shiftId: string; type: ShiftRequestType } | null>(null)
  const [reviewingRequest, setReviewingRequest] = useState<ShiftRequestResponse | null>(null)
  const [deletingShiftRequest, setDeletingShiftRequest] = useState<string | null>(null)

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

  const [actionError, setActionError] = useState<string | null>(null)

  const handleStatusAction = async (
    action: 'open' | 'close' | 'finalize' | 'archive'
  ) => {
    setActionInProgress(action)
    setActionError(null)
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
    } catch (error: any) {
      const message = error?.response?.data?.error || error?.message || 'Action failed. Please try again.'
      setActionError(message)
      console.error(`Failed to ${action} schedule:`, error)
    } finally {
      setActionInProgress(null)
    }
  }

  // Handle preference toggle for employees
  const handlePreferenceToggle = async (
    shiftId: string,
    type: PreferenceType,
    existingPref: PreferenceResponse | undefined
  ) => {
    if (!currentUser) return
    
    const prefKey = `${shiftId}-${type}`
    setPreferenceInProgress(prefKey)
    
    try {
      if (existingPref) {
        // Remove preference
        await deletePreference.mutateAsync(existingPref.id)
      } else {
        // Add preference
        await createPreference.mutateAsync({
          employeeId: currentUser.id,
          shiftId,
          type,
          isHard: type === PreferenceType.Unavailable,
          periodStart: null,
          periodEnd: null,
        })
      }
    } finally {
      setPreferenceInProgress(null)
    }
  }

  // Handle shift request submission
  const handleSubmitShiftRequest = async (note: string) => {
    if (!currentUser || !shiftRequestModal) return
    
    setRequestInProgress(shiftRequestModal.shiftId)
    
    try {
      await createShiftRequest.mutateAsync({
        employeeId: currentUser.id,
        shiftId: shiftRequestModal.shiftId,
        requestType: shiftRequestModal.type,
        note: note || null,
      })
      setShiftRequestModal(null)
    } finally {
      setRequestInProgress(null)
    }
  }

  // Handle deleting a shift request
  const handleDeleteShiftRequest = async () => {
    if (!deletingShiftRequest) return
    
    try {
      await deleteShiftRequest.mutateAsync(deletingShiftRequest)
      setDeletingShiftRequest(null)
    } catch (error) {
      console.error('Failed to delete shift request:', error)
    }
  }

  // Handle reviewing a shift request
  const handleReviewShiftRequest = async (requestId: string, status: RequestStatus, reviewNote?: string) => {
    if (!currentUser) return
    
    try {
      await reviewShiftRequest.mutateAsync({
        id: requestId,
        reviewerId: currentUser.id,
        request: {
          status,
          reviewNote: reviewNote || null,
        },
      })
      setReviewingRequest(null)
    } catch (error) {
      console.error('Failed to review shift request:', error)
    }
  }

  if (isLoading || userLoading) {
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
  const isOpenForPreferences = schedule.status === ScheduleStatus.OpenForPreferences
  const canSubmitPreferences = isOpenForPreferences && currentUser && !isManager
  const canSubmitRequests = isOpenForPreferences && currentUser && !isManager

  // Get preference counts per shift (for managers)
  const preferenceCountByShift = new Map<string, number>()
  preferences?.forEach((p) => {
    const count = preferenceCountByShift.get(p.shiftId) ?? 0
    preferenceCountByShift.set(p.shiftId, count + 1)
  })

  // Get current user's preferences (for employees)
  const myPreferences = preferences?.filter(p => p.employeeId === currentUser?.id) ?? []
  const getMyPreference = (shiftId: string, type: PreferenceType) => 
    myPreferences.find(p => p.shiftId === shiftId && p.type === type)

  // Get shift requests grouped by shift (for managers)
  const requestsByShift = new Map<string, ShiftRequestResponse[]>()
  shiftRequests?.forEach((r) => {
    const existing = requestsByShift.get(r.shiftId) ?? []
    existing.push(r)
    requestsByShift.set(r.shiftId, existing)
  })

  // Get current user's shift requests
  const myShiftRequests = shiftRequests?.filter(r => r.employeeId === currentUser?.id) ?? []
  const getMyShiftRequest = (shiftId: string) => 
    myShiftRequests.find(r => r.shiftId === shiftId)

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
          {isManager && (
            <StatusActions
              status={schedule.status}
              onAction={handleStatusAction}
              isLoading={actionInProgress}
              shiftsCount={schedule.shifts.length}
            />
          )}
        </div>
        
        {/* Action Error Message */}
        {actionError && (
          <div className="mt-4 p-4 bg-red-50 border border-red-200 rounded-lg">
            <div className="flex items-center gap-2 text-red-700">
              <AlertCircle className="h-5 w-5 flex-shrink-0" />
              <p className="text-sm font-medium">{actionError}</p>
              <button 
                onClick={() => setActionError(null)}
                className="ml-auto p-1 hover:bg-red-100 rounded"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Status Flow - only for managers */}
      {isManager && <StatusFlow currentStatus={schedule.status} />}

      {/* Employee Preference Submission Notice */}
      {canSubmitPreferences && (
        <div className="card border-2 border-primary-200 bg-primary-50/50">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary-100">
              <Heart className="h-5 w-5 text-primary-600" />
            </div>
            <div>
              <h2 className="font-semibold text-gray-900">Submit Your Preferences & Requests</h2>
              <p className="text-sm text-gray-600">
                Use <Heart className="h-4 w-4 inline text-green-600" /> / <Ban className="h-4 w-4 inline text-red-600" /> for preferences, 
                or <ThumbsUp className="h-4 w-4 inline text-blue-600" /> / <ThumbsDown className="h-4 w-4 inline text-orange-600" /> to request shifts (needs manager approval).
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Shifts Section */}
      <div className="card">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">
            Shifts ({schedule.shifts.length})
          </h2>
          {isDraft && isManager && (
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
              isDraft && isManager
                ? 'Add shifts to this schedule before opening for preferences'
                : 'This schedule has no shifts'
            }
            action={
              isDraft && isManager && (
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
              const shiftRequestsForShift = requestsByShift.get(shift.id) ?? []
              const pendingRequests = shiftRequestsForShift.filter(r => r.status === RequestStatus.Pending)
              
              // Employee preference state
              const myPreferShift = getMyPreference(shift.id, PreferenceType.PreferShift)
              const myUnavailable = getMyPreference(shift.id, PreferenceType.Unavailable)
              const isPrefLoading = preferenceInProgress?.startsWith(shift.id)
              
              // Employee shift request state
              const myShiftRequest = getMyShiftRequest(shift.id)
              const isRequestLoading = requestInProgress === shift.id
              const hasExistingRequest = !!myShiftRequest

              return (
                <div
                  key={shift.id}
                  className={clsx(
                    "flex flex-col gap-4 p-4 rounded-lg",
                    myPreferShift ? "bg-green-50 border border-green-200" :
                    myUnavailable ? "bg-red-50 border border-red-200" :
                    myShiftRequest?.requestType === ShiftRequestType.WantToWork ? "bg-blue-50 border border-blue-200" :
                    myShiftRequest?.requestType === ShiftRequestType.DoNotWantToWork ? "bg-orange-50 border border-orange-200" :
                    "bg-gray-50"
                  )}
                >
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <h3 className="font-medium text-gray-900">{shift.name}</h3>
                        {/* Manager sees preference count and request count */}
                        {isManager && prefCount > 0 && (
                          <span className="badge badge-blue">
                            {prefCount} preference{prefCount !== 1 ? 's' : ''}
                          </span>
                        )}
                        {isManager && pendingRequests.length > 0 && (
                          <span className="badge badge-orange">
                            {pendingRequests.length} request{pendingRequests.length !== 1 ? 's' : ''}
                          </span>
                        )}
                        {/* Employee sees their own preference/request status */}
                        {!isManager && myPreferShift && (
                          <span className="badge badge-green">Preferred</span>
                        )}
                        {!isManager && myUnavailable && (
                          <span className="badge badge-red">Unavailable</span>
                        )}
                        {!isManager && myShiftRequest && (
                          <span className={clsx(
                            "badge",
                            myShiftRequest.status === RequestStatus.Pending && "badge-yellow",
                            myShiftRequest.status === RequestStatus.Approved && "badge-green",
                            myShiftRequest.status === RequestStatus.Rejected && "badge-red"
                          )}>
                            {myShiftRequest.requestType === ShiftRequestType.WantToWork ? 'Want to work' : "Don't want"} 
                            {' - '}{myShiftRequest.status}
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
                      {/* Assignment Status (for finalized schedules) */}
                      {isFinalized || schedule.status === ScheduleStatus.Archived ? (
                        assignedEmployee ? (
                          <div className={clsx(
                            "flex items-center gap-2 px-3 py-1.5 rounded-lg",
                            assignedEmployee.id === currentUser?.id 
                              ? "bg-primary-100 border border-primary-200"
                              : "bg-green-100"
                          )}>
                            <Users className={clsx(
                              "h-4 w-4",
                              assignedEmployee.id === currentUser?.id 
                                ? "text-primary-600"
                                : "text-green-600"
                            )} />
                            <span className={clsx(
                              "text-sm font-medium",
                              assignedEmployee.id === currentUser?.id 
                                ? "text-primary-700"
                                : "text-green-700"
                            )}>
                              {assignedEmployee.id === currentUser?.id ? 'You' : assignedEmployee.name}
                            </span>
                          </div>
                        ) : (
                          <span className="text-sm text-gray-400">Unassigned</span>
                        )
                      ) : null}

                      {/* Preference & Request Buttons for Employees */}
                      {canSubmitRequests && (
                        <div className="flex items-center gap-1">
                          {/* Preference buttons */}
                          <button
                            onClick={() => handlePreferenceToggle(shift.id, PreferenceType.PreferShift, myPreferShift)}
                            disabled={isPrefLoading || !!myUnavailable || hasExistingRequest}
                            className={clsx(
                              "p-2 rounded transition-colors",
                              myPreferShift 
                                ? "bg-green-100 text-green-600 hover:bg-green-200" 
                                : "hover:bg-gray-200 text-gray-400 hover:text-green-600",
                              (isPrefLoading || myUnavailable || hasExistingRequest) && "opacity-50 cursor-not-allowed"
                            )}
                            title={myPreferShift ? "Remove preference" : "I prefer this shift (soft preference)"}
                          >
                            {myPreferShift ? <Heart className="h-5 w-5 fill-current" /> : <Heart className="h-5 w-5" />}
                          </button>
                          <button
                            onClick={() => handlePreferenceToggle(shift.id, PreferenceType.Unavailable, myUnavailable)}
                            disabled={isPrefLoading || !!myPreferShift || hasExistingRequest}
                            className={clsx(
                              "p-2 rounded transition-colors",
                              myUnavailable 
                                ? "bg-red-100 text-red-600 hover:bg-red-200" 
                                : "hover:bg-gray-200 text-gray-400 hover:text-red-600",
                              (isPrefLoading || myPreferShift || hasExistingRequest) && "opacity-50 cursor-not-allowed"
                            )}
                            title={myUnavailable ? "Remove unavailable" : "I'm unavailable (soft preference)"}
                          >
                            <Ban className="h-5 w-5" />
                          </button>
                          
                          <div className="w-px h-6 bg-gray-300 mx-1" />
                          
                          {/* Request buttons (need approval) */}
                          {!hasExistingRequest ? (
                            <>
                              <button
                                onClick={() => setShiftRequestModal({ shiftId: shift.id, type: ShiftRequestType.WantToWork })}
                                disabled={isRequestLoading || !!myPreferShift || !!myUnavailable}
                                className={clsx(
                                  "p-2 rounded transition-colors hover:bg-gray-200 text-gray-400 hover:text-blue-600",
                                  (isRequestLoading || myPreferShift || myUnavailable) && "opacity-50 cursor-not-allowed"
                                )}
                                title="Request to work this shift (needs approval)"
                              >
                                <ThumbsUp className="h-5 w-5" />
                              </button>
                              <button
                                onClick={() => setShiftRequestModal({ shiftId: shift.id, type: ShiftRequestType.DoNotWantToWork })}
                                disabled={isRequestLoading || !!myPreferShift || !!myUnavailable}
                                className={clsx(
                                  "p-2 rounded transition-colors hover:bg-gray-200 text-gray-400 hover:text-orange-600",
                                  (isRequestLoading || myPreferShift || myUnavailable) && "opacity-50 cursor-not-allowed"
                                )}
                                title="Request NOT to work this shift (needs approval)"
                              >
                                <ThumbsDown className="h-5 w-5" />
                              </button>
                            </>
                          ) : myShiftRequest?.status === RequestStatus.Pending && (
                            <button
                              onClick={() => setDeletingShiftRequest(myShiftRequest.id)}
                              className="p-2 rounded transition-colors hover:bg-gray-200 text-gray-400 hover:text-red-600"
                              title="Cancel request"
                            >
                              <Trash2 className="h-5 w-5" />
                            </button>
                          )}
                        </div>
                      )}

                      {/* Manager Edit/Delete Actions */}
                      {isDraft && isManager && (
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
                  
                  {/* Manager view: Show pending requests for this shift */}
                  {isManager && pendingRequests.length > 0 && (
                    <div className="border-t border-gray-200 pt-3">
                      <p className="text-xs font-medium text-gray-500 uppercase mb-2">Pending Requests</p>
                      <div className="flex flex-wrap gap-2">
                        {pendingRequests.map((request) => (
                          <div
                            key={request.id}
                            className={clsx(
                              "flex items-center gap-2 px-3 py-2 rounded-lg border",
                              request.requestType === ShiftRequestType.WantToWork 
                                ? "bg-blue-50 border-blue-200" 
                                : "bg-orange-50 border-orange-200"
                            )}
                          >
                            {request.requestType === ShiftRequestType.WantToWork ? (
                              <ThumbsUp className="h-4 w-4 text-blue-600" />
                            ) : (
                              <ThumbsDown className="h-4 w-4 text-orange-600" />
                            )}
                            <span className="text-sm font-medium text-gray-900">{request.employeeName}</span>
                            {request.note && (
                              <span title={request.note}>
                                <MessageSquare className="h-3 w-3 text-gray-400" />
                              </span>
                            )}
                            <div className="flex items-center gap-1 ml-2">
                              <button
                                onClick={() => handleReviewShiftRequest(request.id, RequestStatus.Approved)}
                                className="p-1 rounded bg-green-100 text-green-600 hover:bg-green-200"
                                title="Approve"
                              >
                                <Check className="h-3 w-3" />
                              </button>
                              <button
                                onClick={() => setReviewingRequest(request)}
                                className="p-1 rounded bg-red-100 text-red-600 hover:bg-red-200"
                                title="Reject (with note)"
                              >
                                <X className="h-3 w-3" />
                              </button>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
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

      {/* Shift Request Modal */}
      <Modal
        isOpen={!!shiftRequestModal}
        onClose={() => setShiftRequestModal(null)}
        title={shiftRequestModal?.type === ShiftRequestType.WantToWork 
          ? "Request to Work This Shift" 
          : "Request NOT to Work This Shift"}
      >
        <ShiftRequestForm
          type={shiftRequestModal?.type ?? ShiftRequestType.WantToWork}
          onSubmit={handleSubmitShiftRequest}
          onCancel={() => setShiftRequestModal(null)}
          isLoading={createShiftRequest.isPending}
        />
      </Modal>

      {/* Review Request Modal */}
      <Modal
        isOpen={!!reviewingRequest}
        onClose={() => setReviewingRequest(null)}
        title="Review Shift Request"
      >
        {reviewingRequest && (
          <ReviewRequestForm
            request={reviewingRequest}
            onApprove={(note) => handleReviewShiftRequest(reviewingRequest.id, RequestStatus.Approved, note)}
            onReject={(note) => handleReviewShiftRequest(reviewingRequest.id, RequestStatus.Rejected, note)}
            onCancel={() => setReviewingRequest(null)}
            isLoading={reviewShiftRequest.isPending}
          />
        )}
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
    </div>
  )
}

// Shift Request Form Component
function ShiftRequestForm({
  type,
  onSubmit,
  onCancel,
  isLoading,
}: {
  type: ShiftRequestType
  onSubmit: (note: string) => void
  onCancel: () => void
  isLoading: boolean
}) {
  const [note, setNote] = useState('')

  return (
    <div className="space-y-4">
      <div className={clsx(
        "p-4 rounded-lg",
        type === ShiftRequestType.WantToWork ? "bg-blue-50" : "bg-orange-50"
      )}>
        <div className="flex items-center gap-2">
          {type === ShiftRequestType.WantToWork ? (
            <ThumbsUp className="h-5 w-5 text-blue-600" />
          ) : (
            <ThumbsDown className="h-5 w-5 text-orange-600" />
          )}
          <p className="text-sm text-gray-700">
            {type === ShiftRequestType.WantToWork
              ? "You are requesting to work this shift. Your manager will review and approve or reject your request."
              : "You are requesting NOT to work this shift. Please provide a reason below."}
          </p>
        </div>
      </div>

      <div>
        <label htmlFor="note" className="label">
          Note (optional)
        </label>
        <textarea
          id="note"
          rows={3}
          className="input"
          placeholder={type === ShiftRequestType.WantToWork 
            ? "e.g., I have availability and would love to take this shift"
            : "e.g., I have a doctor's appointment at this time"}
          value={note}
          onChange={(e) => setNote(e.target.value)}
        />
      </div>

      <div className="flex justify-end gap-3 pt-4">
        <button type="button" className="btn btn-secondary" onClick={onCancel}>
          Cancel
        </button>
        <button
          type="button"
          className={clsx(
            "btn",
            type === ShiftRequestType.WantToWork ? "btn-primary" : "bg-orange-600 hover:bg-orange-700 text-white"
          )}
          onClick={() => onSubmit(note)}
          disabled={isLoading}
        >
          {isLoading ? 'Submitting...' : 'Submit Request'}
        </button>
      </div>
    </div>
  )
}

// Review Request Form Component
function ReviewRequestForm({
  request,
  onApprove,
  onReject,
  onCancel,
  isLoading,
}: {
  request: ShiftRequestResponse
  onApprove: (note?: string) => void
  onReject: (note?: string) => void
  onCancel: () => void
  isLoading: boolean
}) {
  const [reviewNote, setReviewNote] = useState('')

  return (
    <div className="space-y-4">
      <div className={clsx(
        "p-4 rounded-lg",
        request.requestType === ShiftRequestType.WantToWork ? "bg-blue-50" : "bg-orange-50"
      )}>
        <div className="flex items-center gap-2 mb-2">
          {request.requestType === ShiftRequestType.WantToWork ? (
            <ThumbsUp className="h-5 w-5 text-blue-600" />
          ) : (
            <ThumbsDown className="h-5 w-5 text-orange-600" />
          )}
          <span className="font-medium text-gray-900">{request.employeeName}</span>
        </div>
        <p className="text-sm text-gray-700">
          {request.requestType === ShiftRequestType.WantToWork
            ? `Wants to work: ${request.shiftName}`
            : `Does NOT want to work: ${request.shiftName}`}
        </p>
        <p className="text-xs text-gray-500 mt-1">
          {format(new Date(request.shiftStartTime), 'EEE, MMM d • h:mm a')} – {format(new Date(request.shiftEndTime), 'h:mm a')}
        </p>
        {request.note && (
          <div className="mt-3 p-2 bg-white rounded border">
            <p className="text-xs text-gray-500">Employee's note:</p>
            <p className="text-sm text-gray-700">"{request.note}"</p>
          </div>
        )}
      </div>

      <div>
        <label htmlFor="reviewNote" className="label">
          Response Note (optional)
        </label>
        <textarea
          id="reviewNote"
          rows={2}
          className="input"
          placeholder="Add a note to the employee..."
          value={reviewNote}
          onChange={(e) => setReviewNote(e.target.value)}
        />
      </div>

      <div className="flex justify-end gap-3 pt-4">
        <button type="button" className="btn btn-secondary" onClick={onCancel}>
          Cancel
        </button>
        <button
          type="button"
          className="btn bg-red-600 hover:bg-red-700 text-white"
          onClick={() => onReject(reviewNote)}
          disabled={isLoading}
        >
          Reject
        </button>
        <button
          type="button"
          className="btn btn-primary"
          onClick={() => onApprove(reviewNote)}
          disabled={isLoading}
        >
          {isLoading ? 'Processing...' : 'Approve'}
        </button>
      </div>
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
