import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { render, screen } from '@testing-library/react'
import { vi } from 'vitest'
import { ProtectedRoute } from './ProtectedRoute'
import * as AuthContext from '../context/AuthContext'

// Mock AuthContext so tests control the authenticated state
vi.mock('../context/AuthContext')

describe('ProtectedRoute', () => {
  it('redirects to /login when user is null', () => {
    // Arrange — unauthenticated user
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: null,
      login: vi.fn(),
      logout: vi.fn(),
    })

    // Act — render a protected route alongside a /login route
    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Routes>
          <Route path="/login" element={<div>Login Page</div>} />
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <div>Protected Content</div>
              </ProtectedRoute>
            }
          />
        </Routes>
      </MemoryRouter>
    )

    // Assert — protected content is gone and /login is rendered instead
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
    expect(screen.getByText('Login Page')).toBeInTheDocument()
  })
})
