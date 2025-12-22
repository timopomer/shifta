import { createFileRoute, Link } from '@tanstack/react-router'
import { Calendar, Users, Clock, CheckCircle, CalendarDays, HandHeart } from 'lucide-react'
import { useSchedules } from '@/hooks'
import { useEmployees } from '@/hooks'
import { ScheduleStatus } from '@/api'
import { StatusBadge, PageLoader } from '@/components'
import { useCurrentUser } from '@/context'
import { format } from 'date-fns'

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
  const activeSchedules = schedules.filter((s) => s.status !== ScheduleStatus.Archived)
  const draftSchedules = schedules.filter((s) => s.status === ScheduleStatus.Draft)
  const finalizedSchedules = schedules.filter((s) => s.status === ScheduleStatus.Finalized)

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

// Employee sees a simpler dashboard focused on their schedules and preferences
function EmployeeDashboard({ 
  schedules,
}: { 
  schedules: { id: string; name: string; weekStartDate: string; status: ScheduleStatus }[]
}) {
  const openForPreferences = schedules.filter(
    (s) => s.status === ScheduleStatus.OpenForPreferences
  )
  const finalizedSchedules = schedules.filter(
    (s) => s.status === ScheduleStatus.Finalized
  )

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">My Dashboard</h1>
        <p className="mt-1 text-sm text-gray-500">
          View your schedules and submit shift preferences
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
                These schedules are open for preference submission
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

      {/* Stats for Employee */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
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
    </div>
  )
}
