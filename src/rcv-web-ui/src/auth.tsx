import { createContext, useContext, useEffect, type ReactNode } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Navigate, useLocation } from 'react-router-dom'
import { ApiError, authApi, bootstrapCsrf } from './api'
import type { Provider, User } from './types'
import { LoadingState } from './components'

interface AuthContextValue {
  user: User | null
  isLoading: boolean
  error: Error | null
  login: (provider: Provider, returnTo?: string) => void
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const query = useQuery({
    queryKey: ['auth', 'me'],
    queryFn: authApi.me,
    retry: (count, error) => !(error instanceof ApiError && error.status === 401) && count < 2,
  })

  useEffect(() => {
    void bootstrapCsrf().catch(() => undefined)
    const unauthorized = () => queryClient.setQueryData(['auth', 'me'], null)
    window.addEventListener('auth:unauthorized', unauthorized)
    return () => window.removeEventListener('auth:unauthorized', unauthorized)
  }, [queryClient])

  const unauthorized = query.error instanceof ApiError && query.error.status === 401

  return (
    <AuthContext.Provider
      value={{
        user: query.data ?? null,
        isLoading: query.isLoading,
        error: unauthorized ? null : query.error,
        login: (provider, returnTo) => {
          window.location.assign(authApi.loginUrl(provider, returnTo))
        },
        logout: async () => {
          await authApi.logout()
          queryClient.setQueryData(['auth', 'me'], null)
          queryClient.removeQueries({
            predicate: (candidate) => candidate.queryKey[0] !== 'auth',
          })
        },
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider')
  return context
}

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { user, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) return <LoadingState label="Checking your account…" />
  if (!user) {
    const returnTo = `${location.pathname}${location.search}`
    return <Navigate to="/login" replace state={{ returnTo }} />
  }
  return children
}
