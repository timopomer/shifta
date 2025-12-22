import { Link, useRouterState } from '@tanstack/react-router'
import { Calendar, Users, LayoutDashboard, Menu, X, ChevronDown, Shield } from 'lucide-react'
import { useState, useRef, useEffect } from 'react'
import clsx from 'clsx'
import { useCurrentUser } from '@/context'
import { useEmployees } from '@/hooks'

export function Layout({ children }: { children: React.ReactNode }) {
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const routerState = useRouterState()
  const currentPath = routerState.location.pathname
  const { currentUser, setCurrentUser, isManager } = useCurrentUser()
  const { data: employees } = useEmployees()

  // Manager-only pages are hidden from non-managers
  const navigation = [
    { name: 'Dashboard', href: '/', icon: LayoutDashboard, managerOnly: false },
    { name: 'Employees', href: '/employees', icon: Users, managerOnly: true },
    { name: 'Schedules', href: '/schedules', icon: Calendar, managerOnly: false },
  ].filter(item => !item.managerOnly || isManager)

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Mobile sidebar backdrop */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 z-40 bg-gray-900/50 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      {/* Mobile sidebar */}
      <div
        className={clsx(
          'fixed inset-y-0 left-0 z-50 w-64 bg-white shadow-xl transform transition-transform duration-200 lg:hidden',
          sidebarOpen ? 'translate-x-0' : '-translate-x-full'
        )}
      >
        <div className="flex h-16 items-center justify-between px-4 border-b border-gray-200">
          <span className="text-xl font-bold text-primary-600">Shifta</span>
          <button
            onClick={() => setSidebarOpen(false)}
            className="p-2 rounded-lg hover:bg-gray-100"
          >
            <X className="h-5 w-5 text-gray-500" />
          </button>
        </div>
        <nav className="p-4 space-y-1">
          {navigation.map((item) => {
            const isActive = currentPath === item.href || 
              (item.href !== '/' && currentPath.startsWith(item.href))
            return (
              <Link
                key={item.name}
                to={item.href}
                onClick={() => setSidebarOpen(false)}
                className={clsx(
                  'flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-primary-50 text-primary-700'
                    : 'text-gray-700 hover:bg-gray-100'
                )}
              >
                <item.icon className="h-5 w-5" />
                {item.name}
              </Link>
            )
          })}
        </nav>
      </div>

      {/* Desktop sidebar */}
      <div className="hidden lg:fixed lg:inset-y-0 lg:flex lg:w-64 lg:flex-col">
        <div className="flex flex-col flex-grow bg-white border-r border-gray-200">
          <div className="flex h-16 items-center px-6 border-b border-gray-200">
            <span className="text-xl font-bold text-primary-600">Shifta</span>
          </div>
          <nav className="flex-1 p-4 space-y-1">
            {navigation.map((item) => {
              const isActive = currentPath === item.href || 
                (item.href !== '/' && currentPath.startsWith(item.href))
              return (
                <Link
                  key={item.name}
                  to={item.href}
                  className={clsx(
                    'flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors',
                    isActive
                      ? 'bg-primary-50 text-primary-700'
                      : 'text-gray-700 hover:bg-gray-100'
                  )}
                >
                  <item.icon className="h-5 w-5" />
                  {item.name}
                </Link>
              )
            })}
          </nav>
        </div>
      </div>

      {/* Main content */}
      <div className="lg:pl-64">
        {/* Top bar */}
        <div className="sticky top-0 z-30 flex h-16 items-center justify-between gap-4 bg-white border-b border-gray-200 px-4 lg:px-8">
          <div className="flex items-center gap-4">
            <button
              onClick={() => setSidebarOpen(true)}
              className="p-2 rounded-lg hover:bg-gray-100 lg:hidden"
            >
              <Menu className="h-5 w-5 text-gray-500" />
            </button>
            <h1 className="text-lg font-semibold text-gray-900 lg:hidden">Shifta</h1>
          </div>
          
          {/* Impersonation Dropdown */}
          <ImpersonationDropdown
            currentUser={currentUser}
            employees={employees ?? []}
            onUserChange={setCurrentUser}
          />
        </div>

        {/* Page content */}
        <main className="p-4 lg:p-8">{children}</main>
      </div>
    </div>
  )
}

interface ImpersonationDropdownProps {
  currentUser: { id: string; name: string; isManager: boolean } | null
  employees: { id: string; name: string; isManager: boolean }[]
  onUserChange: (user: { id: string; name: string; isManager: boolean; email: string; abilities: string[]; createdAt: string; updatedAt: string }) => void
}

function ImpersonationDropdown({ currentUser, employees, onUserChange }: ImpersonationDropdownProps) {
  const [isOpen, setIsOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  if (!currentUser) {
    return (
      <div className="flex items-center gap-2 px-3 py-2 bg-gray-100 rounded-lg animate-pulse">
        <div className="h-6 w-6 rounded-full bg-gray-300" />
        <div className="h-4 w-24 bg-gray-300 rounded" />
      </div>
    )
  }

  // Sort employees: managers first, then alphabetically
  const sortedEmployees = [...employees].sort((a, b) => {
    if (a.isManager !== b.isManager) return a.isManager ? -1 : 1
    return a.name.localeCompare(b.name)
  })

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className={clsx(
          'flex items-center gap-2 px-3 py-2 rounded-lg border transition-all',
          isOpen
            ? 'bg-primary-50 border-primary-300 ring-2 ring-primary-100'
            : 'bg-white border-gray-200 hover:bg-gray-50 hover:border-gray-300'
        )}
      >
        <div className="flex items-center gap-2">
          <div className={clsx(
            'h-7 w-7 rounded-full flex items-center justify-center text-xs font-medium',
            currentUser.isManager
              ? 'bg-purple-100 text-purple-700'
              : 'bg-primary-100 text-primary-700'
          )}>
            {currentUser.name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)}
          </div>
          <div className="text-left">
            <div className="text-sm font-medium text-gray-900 flex items-center gap-1.5">
              {currentUser.name}
              {currentUser.isManager && (
                <Shield className="h-3.5 w-3.5 text-purple-600" />
              )}
            </div>
            <div className="text-xs text-gray-500">
              {currentUser.isManager ? 'Manager' : 'Employee'}
            </div>
          </div>
        </div>
        <ChevronDown className={clsx(
          'h-4 w-4 text-gray-400 transition-transform',
          isOpen && 'rotate-180'
        )} />
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-72 bg-white rounded-lg shadow-lg border border-gray-200 py-2 z-50">
          <div className="px-3 py-2 border-b border-gray-100">
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider">
              View as Employee
            </p>
          </div>
          <div className="max-h-80 overflow-y-auto py-1">
            {sortedEmployees.map((employee) => (
              <button
                key={employee.id}
                onClick={() => {
                  onUserChange(employee as any)
                  setIsOpen(false)
                }}
                className={clsx(
                  'w-full flex items-center gap-3 px-3 py-2 text-left hover:bg-gray-50 transition-colors',
                  employee.id === currentUser.id && 'bg-primary-50'
                )}
              >
                <div className={clsx(
                  'h-8 w-8 rounded-full flex items-center justify-center text-xs font-medium',
                  employee.isManager
                    ? 'bg-purple-100 text-purple-700'
                    : 'bg-gray-100 text-gray-700'
                )}>
                  {employee.name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)}
                </div>
                <div className="flex-1 min-w-0">
                  <div className="text-sm font-medium text-gray-900 truncate flex items-center gap-1.5">
                    {employee.name}
                    {employee.isManager && (
                      <Shield className="h-3.5 w-3.5 text-purple-600" />
                    )}
                  </div>
                  <div className="text-xs text-gray-500">
                    {employee.isManager ? 'Manager' : 'Employee'}
                  </div>
                </div>
                {employee.id === currentUser.id && (
                  <div className="h-2 w-2 rounded-full bg-primary-500" />
                )}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
