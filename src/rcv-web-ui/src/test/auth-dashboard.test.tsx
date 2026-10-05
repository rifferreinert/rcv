import { screen } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { renderApp } from './render'
import { openPoll, server } from './server'

describe('authentication and dashboard', () => {
  it('redirects a signed-out visitor from the dashboard to login', async () => {
    server.use(http.get('*/api/auth/me', () => new HttpResponse(null, { status: 401 })))
    renderApp('/dashboard')
    expect(await screen.findByRole('heading', { name: /sign in to ranked/i })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Dev Login (local only)' }))
      .toHaveAttribute('href', '/api/auth/dev-login')
  })

  it('loads, filters, and paginates the current user polls', async () => {
    server.use(
      http.get('*/api/polls', ({ request }) => {
        const url = new URL(request.url)
        expect(url.searchParams.get('page')).toBe('1')
        expect(url.searchParams.get('pageSize')).toBe('8')
        return HttpResponse.json({
          items: [openPoll],
          page: 1,
          pageSize: 8,
          totalCount: 1,
          totalPages: 1,
        })
      }),
    )
    renderApp('/dashboard')
    expect(await screen.findByRole('heading', { name: 'Choose our park project' })).toBeInTheDocument()
    expect(screen.getAllByText('Open')).toHaveLength(2)
    expect(screen.getByRole('link', { name: 'Edit' })).toHaveAttribute('href', '/polls/poll-1/edit')
  })
})
