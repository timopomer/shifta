// Types matching the C# backend DTOs

// Enums
export enum ScheduleStatus {
  Draft = 'Draft',
  OpenForPreferences = 'OpenForPreferences',
  PendingReview = 'PendingReview',
  Finalized = 'Finalized',
  Archived = 'Archived',
}

export enum PreferenceType {
  PreferShift = 'PreferShift',
  PreferPeriod = 'PreferPeriod',
  Unavailable = 'Unavailable',
}

export enum RequestStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Rejected = 'Rejected',
}

export enum ShiftRequestType {
  WantToWork = 'WantToWork',
  DoNotWantToWork = 'DoNotWantToWork',
}

// Employee DTOs
export interface CreateEmployeeRequest {
  name: string
  email: string
  abilities: string[]
  isManager: boolean
}

export interface UpdateEmployeeRequest {
  name: string
  email: string
  abilities: string[]
  isManager: boolean
}

export interface EmployeeResponse {
  id: string
  name: string
  email: string
  abilities: string[]
  isManager: boolean
  createdAt: string
  updatedAt: string
}

// Shift DTOs
export interface CreateShiftRequest {
  name: string
  startTime: string
  endTime: string
  requiredAbilities: string[]
}

export interface UpdateShiftRequest {
  name: string
  startTime: string
  endTime: string
  requiredAbilities: string[]
}

export interface ShiftResponse {
  id: string
  shiftScheduleId: string
  name: string
  startTime: string
  endTime: string
  requiredAbilities: string[]
  createdAt: string
  updatedAt: string
}

// Schedule DTOs
export interface CreateScheduleRequest {
  name: string
  weekStartDate: string
}

export interface ScheduleResponse {
  id: string
  name: string
  weekStartDate: string
  status: ScheduleStatus
  createdAt: string
  updatedAt: string
  finalizedAt: string | null
}

export interface ScheduleDetailResponse extends ScheduleResponse {
  shifts: ShiftResponse[]
  assignments: ShiftAssignmentResponse[]
}

// Assignment DTOs
export interface ShiftAssignmentResponse {
  id: string
  shiftId: string
  employeeId: string
  assignedAt: string
}

// Preference DTOs
export interface CreatePreferenceRequest {
  employeeId: string
  shiftId: string
  type: PreferenceType
  periodStart?: string | null
  periodEnd?: string | null
  isHard: boolean
}

export interface PreferenceResponse {
  id: string
  employeeId: string
  shiftId: string
  type: PreferenceType
  periodStart: string | null
  periodEnd: string | null
  isHard: boolean
  createdAt: string
}

// Manager-Employee DTOs
export interface CreateManagerEmployeeRequest {
  managerId: string
  employeeId: string
}

export interface ManagerEmployeeResponse {
  id: string
  managerId: string
  employeeId: string
  managerName: string
  employeeName: string
  createdAt: string
}

export interface EmployeeBasicInfo {
  id: string
  name: string
  email: string
}

export interface ManagerWithEmployeesResponse {
  managerId: string
  managerName: string
  employees: EmployeeBasicInfo[]
}

export interface EmployeeWithManagersResponse {
  employeeId: string
  employeeName: string
  managers: EmployeeBasicInfo[]
}

// Shift Request DTOs
export interface CreateShiftRequestRequest {
  employeeId: string
  shiftId: string
  requestType: ShiftRequestType
  note?: string | null
}

export interface ReviewShiftRequestRequest {
  status: RequestStatus
  reviewNote?: string | null
}

export interface ShiftRequestResponse {
  id: string
  employeeId: string
  employeeName: string
  shiftId: string
  shiftName: string
  shiftStartTime: string
  shiftEndTime: string
  scheduleId: string
  scheduleName: string
  requestType: ShiftRequestType
  status: RequestStatus
  note: string | null
  createdAt: string
  updatedAt: string
  reviewedById: string | null
  reviewedByName: string | null
  reviewedAt: string | null
  reviewNote: string | null
}

// Time Off Request DTOs
export interface CreateTimeOffRequestRequest {
  employeeId: string
  startDate: string
  endDate: string
  reason?: string | null
}

export interface ReviewTimeOffRequestRequest {
  status: RequestStatus
  reviewNote?: string | null
}

export interface TimeOffRequestResponse {
  id: string
  employeeId: string
  employeeName: string
  startDate: string
  endDate: string
  status: RequestStatus
  reason: string | null
  createdAt: string
  updatedAt: string
  reviewedById: string | null
  reviewedByName: string | null
  reviewedAt: string | null
  reviewNote: string | null
}

// API Error Response
export interface ApiError {
  error: string
}
