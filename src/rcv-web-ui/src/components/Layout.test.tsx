import { MemoryRouter } from 'react-router-dom'
import { render, screen } from '@testing-library/react'
import { vi } from 'vitest'
import { Layout } from './Layout'
import * as AuthContext from '../context/AuthContext'

// Mock AuthContext so tests control the authenticated state
vi.mock('../context/AuthContext')

describe('Layout', () => {
  it('renders a navigation header with "RCV" as a home link and renders children', () => {
    // Arrange — authenticated user, with a recognisable child element
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: { id: 'u1', email: 'test@example.com', displayName: 'Test User', provider: 'google' },
      login: vi.fn(),
      logout: vi.fn(),
    })

    // Act — render Layout with a child
    render(
      <MemoryRouter initialEntries={['/']}>
        <Layout>
          <div>Child Content</div>
        </Layout>
      </MemoryRouter>
    )

    // Assert — "RCV" is rendered as a link pointing to "/"
    const homeLink = screen.getByRole('link', { name: 'RCV' })
    expect(homeLink).toBeInTheDocument()
    expect(homeLink).toHaveAttribute('href', '/')

    // Assert — children are rendered
    expect(screen.getByText('Child Content')).toBeInTheDocument()
  })
})
