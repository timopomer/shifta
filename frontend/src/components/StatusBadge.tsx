import { ScheduleStatus } from '@/api'
import clsx from 'clsx'

const statusConfig: Record<ScheduleStatus, { label: string; className: string }> = {
  [ScheduleStatus.Draft]: {
    label: 'Draft',
    className: 'badge-gray',
  },
  [ScheduleStatus.OpenForPreferences]: {
    label: 'Open for Preferences',
    className: 'badge-blue',
  },
  [ScheduleStatus.PendingReview]: {
    label: 'Pending Review',
    className: 'badge-yellow',
  },
  [ScheduleStatus.Finalized]: {
    label: 'Finalized',
    className: 'badge-green',
  },
  [ScheduleStatus.Archived]: {
    label: 'Archived',
    className: 'badge-purple',
  },
}

interface StatusBadgeProps {
  status: ScheduleStatus
}

export function StatusBadge({ status }: StatusBadgeProps) {
  const config = statusConfig[status]
  return <span className={clsx('badge', config.className)}>{config.label}</span>
}
