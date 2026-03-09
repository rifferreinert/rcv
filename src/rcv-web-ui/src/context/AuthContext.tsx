import { createContext, useContext, useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import client from '../api/client'
import { User } from '../types'

interface AuthContextValue {
  user: User | null
  login: (provider: string) => void
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)

  useEffect(() => {
    client.get('/api/auth/me')
      .then(res => setUser(res.data))
      .catch(() => setUser(null))
  }, [])

  const login = (provider: string) => {
    window.location.href = `/api/auth/login/${provider}`
  }

  const logout = async () => {
    await client.post('/api/auth/logout')
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within an AuthProvider')
  return context
}
