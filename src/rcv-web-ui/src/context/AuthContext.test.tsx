import { describe, it, expect, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import axios from 'axios'
import { AuthProvider, useAuth } from './AuthContext'

vi.mock('axios')

// A minimal consumer component that surfaces the user state as visible text
function TestConsumer() {
  const { user } = useAuth()
  return (
    <div data-testid="user-state">
      {user === null ? 'null' : 'authenticated'}
    </div>
  )
}

// A consumer that renders the authenticated user's displayName
function DisplayNameConsumer() {
  const { user } = useAuth()
  return (
    <div data-testid="display-name">
      {user ? user.displayName : ''}
    </div>
  )
}

describe('AuthContext', () => {
  describe('AuthProvider', () => {
    it('sets user to null when /api/auth/me returns 401', async () => {
      // Arrange — simulate a 401 Unauthorized response from the auth endpoint
      const unauthorizedError = Object.assign(
        new Error('Request failed with status code 401'),
        { response: { status: 401, data: {} } }
      )
      vi.mocked(axios.get).mockRejectedValue(unauthorizedError)

      // Act — render the provider with a consumer that reads the user state
      render(
        <AuthProvider>
          <TestConsumer />
        </AuthProvider>
      )

      // Assert — once the async auth check resolves, user must be null
      await waitFor(() => {
        expect(screen.getByTestId('user-state')).toHaveTextContent('null')
      })
    })

    it('sets user to the returned object when /api/auth/me resolves successfully', async () => {
      // Arrange — simulate a successful response with a user payload
      const mockUser = {
        id: 'user-1',
        email: 'test@example.com',
        displayName: 'Test User',
        provider: 'google',
      }
      vi.mocked(axios.get).mockResolvedValue({ data: mockUser })

      // Act — render the provider with a consumer that displays the user's displayName
      render(
        <AuthProvider>
          <DisplayNameConsumer />
        </AuthProvider>
      )

      // Assert — once the async auth check resolves, the user's displayName is shown
      await waitFor(() => {
        expect(screen.getByTestId('display-name')).toHaveTextContent('Test User')
      })
    })
  })

  describe('useAuth', () => {
    it('throws an error when called outside of AuthProvider', () => {
      // Arrange — a bare component that calls useAuth() with no provider in the tree
      function BareConsumer() {
        useAuth()
        return null
      }

      // Suppress React's console.error output for the expected throw
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})

      // Act & Assert — rendering without AuthProvider must throw the sentinel message
      expect(() => render(<BareConsumer />)).toThrow(
        'useAuth must be used within an AuthProvider'
      )

      consoleError.mockRestore()
    })
  })
})
