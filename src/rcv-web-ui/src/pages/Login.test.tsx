import { MemoryRouter } from 'react-router-dom'
import { render, screen } from '@testing-library/react'
import { vi } from 'vitest'
import * as AuthContext from '../context/AuthContext'
import Login from './Login'

vi.mock('../context/AuthContext')

describe('Login', () => {
  it('renders a "Sign in with Google" button and a "Sign in with Microsoft" button', () => {
    vi.mocked(AuthContext.useAuth).mockReturnValue({
      user: null,
      login: vi.fn(),
      logout: vi.fn(),
    })

    render(
      <MemoryRouter>
        <Login />
      </MemoryRouter>
    )

    expect(screen.getByRole('button', { name: /sign in with google/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /sign in with microsoft/i })).toBeInTheDocument()
  })

  describe('in development mode', () => {
    afterEach(() => {
      vi.unstubAllEnvs()
    })

    it('renders a "Dev Login (local only)" button in development mode', () => {
      vi.stubEnv('DEV', true)

      vi.mocked(AuthContext.useAuth).mockReturnValue({
        user: null,
        login: vi.fn(),
        logout: vi.fn(),
      })

      render(
        <MemoryRouter>
          <Login />
        </MemoryRouter>
      )

      expect(screen.getByRole('button', { name: /dev login \(local only\)/i })).toBeInTheDocument()
    })
  })
})
