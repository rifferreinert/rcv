import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { render, screen } from '@testing-library/react'
import { vi } from 'vitest'
import { Layout } from './Layout'
import * as AuthContext from '../context/AuthContext'

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
})
