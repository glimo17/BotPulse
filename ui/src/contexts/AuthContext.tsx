import React, { createContext, useContext, useState, useCallback } from 'react'
import api, { setToken, clearToken } from '@/lib/api'

interface User {
  userId: string
  userName: string
  email: string
  roles: string[]
}

interface AuthContextType {
  user: User | null
  isAuthenticated: boolean
  login: (username: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextType | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null)

  const login = useCallback(async (username: string, password: string) => {
    const response = await api.post<{ token: string }>('/auth/login', {
      userName: username,
      password,
    })
    const { token } = response.data
    setToken(token)

    const meResponse = await api.get<User>('/auth/me')
    setUser(meResponse.data)
  }, [])

  const logout = useCallback(() => {
    clearToken()
    setUser(null)
    api.post('/auth/logout').catch(() => {})
  }, [])

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
