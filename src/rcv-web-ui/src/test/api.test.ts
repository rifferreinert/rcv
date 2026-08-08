import { describe, expect, it } from 'vitest'
import { authApi } from '../api'

describe('auth API', () => {
  it('preserves a safe post-login destination', () => {
    expect(authApi.loginUrl('google', '/polls/poll-1?from=share')).toBe(
      '/api/auth/login/google?returnUrl=%2Fpolls%2Fpoll-1%3Ffrom%3Dshare',
    )
    expect(authApi.loginUrl('microsoft', '//example.com')).toBe('/api/auth/login/microsoft')
  })
})
