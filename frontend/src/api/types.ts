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

// API Error Response
export interface ApiError {
  error: string
}
