import { MemoryRouter } from 'react-router-dom'
import { render, screen } from '@testing-library/react'
import { vi } from 'vitest'
import { AuthButton } from './AuthButton'
import * as AuthContext from '../context/AuthContext'

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
})
