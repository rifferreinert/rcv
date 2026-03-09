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
  })
})
