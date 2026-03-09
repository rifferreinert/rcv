import { MemoryRouter, Routes, Route, useLocation } from 'react-router-dom'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { vi } from 'vitest'
import { AuthButton } from './AuthButton'
import * as AuthContext from '../context/AuthContext'

function LocationDisplay() {
  const location = useLocation()
  return <div data-testid="location">{location.pathname}</div>
}

// Mock AuthContext so tests control the authenticated state
vi.mock('../context/AuthContext')

describe('AuthButton', () => {
  it('shows the user display name and a logout button when authenticated', () => {
    // Arrange — authenticated user
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: { id: 'u1', email: 'alice@example.com', displayName: 'Alice', provider: 'google' },
      login: vi.fn(),
      logout: vi.fn(),
    })

    // Act — render AuthButton inside a router (links may be rendered internally)
    render(
      <MemoryRouter>
        <AuthButton />
      </MemoryRouter>
    )

    // Assert — the authenticated user's display name is visible
    expect(screen.getByText('Alice')).toBeInTheDocument()

    // Assert — a logout button is present
    expect(screen.getByRole('button', { name: /logout/i })).toBeInTheDocument()
  })

  it('shows a login link pointing to /login when unauthenticated', () => {
    // Arrange — no authenticated user
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: null,
      login: vi.fn(),
      logout: vi.fn(),
    })

    // Act — render AuthButton inside a router
    render(
      <MemoryRouter>
        <AuthButton />
      </MemoryRouter>
    )

    // Assert — a link with text "Login" that points to /login is rendered
    const loginLink = screen.getByRole('link', { name: /login/i })
    expect(loginLink).toBeInTheDocument()
    expect(loginLink).toHaveAttribute('href', '/login')
  })

  it('navigates to / after clicking the logout button', async () => {
    // Arrange — authenticated user with a logout stub that resolves immediately
    const logout = vi.fn().mockResolvedValue(undefined)
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: { id: 'u1', email: 'alice@example.com', displayName: 'Alice', provider: 'google' },
      login: vi.fn(),
      logout,
    })

    const user = userEvent.setup()

    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Routes>
          <Route path="/" element={<div>Home</div>} />
          <Route path="/dashboard" element={<AuthButton />} />
        </Routes>
        <LocationDisplay />
      </MemoryRouter>
    )

    // Act — click the Logout button
    await user.click(screen.getByRole('button', { name: /logout/i }))

    // Assert — the app navigated to the home route (exact match, not /dashboard)
    await waitFor(() => {
      expect(screen.getByTestId('location').textContent).toBe('/')
    })
  })
})
