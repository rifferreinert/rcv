import { describe, it, expect } from 'vitest'
import client from './client'

describe('API client', () => {
  it('uses the VITE_API_URL env var as the base URL (defaulting to http://localhost:5041) and sets withCredentials to true', () => {
    const expectedBaseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5041'

    expect(client.defaults.baseURL).toBe(expectedBaseUrl)
    expect(client.defaults.withCredentials).toBe(true)
  })
})
