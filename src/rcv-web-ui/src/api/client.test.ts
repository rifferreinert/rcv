import { describe, it, expect, vi, afterEach } from 'vitest'
import client from './client'

describe('API client', () => {
  it('uses the VITE_API_URL env var as the base URL (defaulting to http://localhost:5041) and sets withCredentials to true', () => {
    const expectedBaseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5041'

    expect(client.defaults.baseURL).toBe(expectedBaseUrl)
    expect(client.defaults.withCredentials).toBe(true)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('redirects to /login when a 401 response is received', async () => {
    // Arrange — capture any assignment to window.location.href
    let redirectedTo = ''
    vi.stubGlobal('location', {
      ...window.location,
      set href(url: string) {
        redirectedTo = url
      },
      get href() {
        return redirectedTo
      },
    })

    // Get the rejection handler registered by the response interceptor
    const handlers = (client.interceptors.response as any).handlers as Array<{
      fulfilled: ((v: unknown) => unknown) | null
      rejected: ((e: unknown) => unknown) | null
    }>
    const rejectHandler = handlers[0]?.rejected

    // Act — invoke the rejection handler with a simulated 401 error
    if (rejectHandler) {
      try {
        await rejectHandler({ response: { status: 401 } })
      } catch {
        // interceptor may re-throw; that's fine
      }
    }

    // Assert
    expect(redirectedTo).toBe('/login')
  })
})
