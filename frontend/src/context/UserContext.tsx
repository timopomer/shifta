import { createContext, useContext, useState, useEffect, ReactNode } from 'react'
import { EmployeeResponse } from '@/api'
import { useEmployees } from '@/hooks'

interface UserContextValue {
  currentUser: EmployeeResponse | null
  setCurrentUser: (user: EmployeeResponse | null) => void
  isManager: boolean
  isLoading: boolean
}

const UserContext = createContext<UserContextValue | undefined>(undefined)

export function UserProvider({ children }: { children: ReactNode }) {
  const { data: employees, isLoading: employeesLoading } = useEmployees()
  const [currentUser, setCurrentUser] = useState<EmployeeResponse | null>(null)
  const [initialized, setInitialized] = useState(false)

  // Initialize with manager on first load (demo mode)
  useEffect(() => {
    if (employees && employees.length > 0 && !initialized) {
      // Find a manager to be the default user
      const manager = employees.find(e => e.isManager)
      const defaultUser = manager ?? employees[0]
      if (defaultUser) {
        setCurrentUser(defaultUser)
      }
      setInitialized(true)
    }
  }, [employees, initialized])

  const isManager = currentUser?.isManager ?? false

  return (
    <UserContext.Provider
      value={{
        currentUser,
        setCurrentUser,
        isManager,
        isLoading: employeesLoading || !initialized,
      }}
    >
      {children}
    </UserContext.Provider>
  )
}

export function useCurrentUser() {
  const context = useContext(UserContext)
  if (context === undefined) {
    throw new Error('useCurrentUser must be used within a UserProvider')
  }
  return context
}

