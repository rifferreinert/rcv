import { MemoryRouter, Routes, Route, useLocation } from 'react-router-dom'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { vi } from 'vitest'
import { Layout } from './Layout'
import * as AuthContext from '../context/AuthContext'

function LocationDisplay() {
  const location = useLocation()
  return <div data-testid="location">{location.pathname}</div>
}

// Mock AuthContext so tests control the authenticated state
vi.mock('../context/AuthContext')

describe('Layout', () => {
  it('renders a navigation header with "RCV" as a home link', () => {
    // Arrange — authenticated user
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: { id: 'u1', email: 'test@example.com', displayName: 'Test User', provider: 'google' },
      login: vi.fn(),
      logout: vi.fn(),
    })

    // Act — render Layout as a layout route wrapping a child route
    render(
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route element={<Layout />}>
            <Route path="/" element={<div>Child Content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>
    )

    // Assert — "RCV" is rendered as a link pointing to "/"
    const homeLink = screen.getByRole('link', { name: 'RCV' })
    expect(homeLink).toBeInTheDocument()
    expect(homeLink).toHaveAttribute('href', '/')

    // Assert — outlet content is rendered
    expect(screen.getByText('Child Content')).toBeInTheDocument()
  })

  it('shows login link when not authenticated', () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: null,
      login: vi.fn(),
      logout: vi.fn(),
    })

    render(
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route element={<Layout />}>
            <Route path="/" element={<div />} />
          </Route>
        </Routes>
      </MemoryRouter>
    )

    expect(screen.getByRole('link', { name: 'Login' })).toBeInTheDocument()
  })

  it('navigates to / when the logout button is clicked', async () => {
    // Arrange — authenticated user with a logout stub that resolves immediately
    const logout = vi.fn().mockResolvedValue(undefined)
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: { id: 'u1', email: 'test@example.com', displayName: 'Test User', provider: 'google' },
      login: vi.fn(),
      logout,
    })

    const user = userEvent.setup()

    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Routes>
          <Route element={<Layout />}>
            <Route path="/" element={<div>Home</div>} />
            <Route path="/dashboard" element={<div>Dashboard</div>} />
          </Route>
        </Routes>
        <LocationDisplay />
      </MemoryRouter>
    )

    // Act — click the Logout button
    await user.click(screen.getByRole('button', { name: /logout/i }))

    // Assert — the app navigated to the home route after logout
    await waitFor(() => {
      expect(screen.getByTestId('location').textContent).toBe('/')
    })
  })
})
