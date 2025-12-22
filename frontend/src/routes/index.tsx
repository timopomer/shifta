import { createFileRoute, Link } from '@tanstack/react-router'
import { Calendar, Users, Clock, CheckCircle } from 'lucide-react'
import { useSchedules } from '@/hooks'
import { useEmployees } from '@/hooks'
import { ScheduleStatus } from '@/api'
import { StatusBadge, PageLoader } from '@/components'
import { format } from 'date-fns'

export const Route = createFileRoute('/')({
  component: Dashboard,
})

function Dashboard() {
  const { data: schedules, isLoading: schedulesLoading } = useSchedules()
  const { data: employees, isLoading: employeesLoading } = useEmployees()

  if (schedulesLoading || employeesLoading) {
    return <PageLoader />
  }

  const activeSchedules = schedules?.filter(
    (s) => s.status !== ScheduleStatus.Archived
  ) ?? []
  const draftSchedules = schedules?.filter(
    (s) => s.status === ScheduleStatus.Draft
  ) ?? []
  const finalizedSchedules = schedules?.filter(
    (s) => s.status === ScheduleStatus.Finalized
  ) ?? []

  const stats = [
    {
      name: 'Total Employees',
      value: employees?.length ?? 0,
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
      name: 'Finalized',
      value: finalizedSchedules.length,
      icon: CheckCircle,
      color: 'bg-purple-500',
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
