import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'

export const currentUser = {
  id: 'user-1',
  email: 'alex@example.com',
  displayName: 'Alex Voter',
  provider: 'Google',
}

export const openPoll = {
  id: 'poll-1',
  title: 'Choose our park project',
  description: 'Rank the improvements you want most.',
  options: [
    { id: 'option-1', text: 'Community garden', displayOrder: 0 },
    { id: 'option-2', text: 'Playground', displayOrder: 1 },
    { id: 'option-3', text: 'Outdoor stage', displayOrder: 2 },
  ],
  closesAt: '2099-08-10T18:00:00Z',
  isResultsPublic: true,
  status: 'Active',
  creator: { id: 'user-1', displayName: 'Alex Voter' },
  createdAt: '2026-08-01T18:00:00Z',
  closedAt: null,
  voteCount: 0,
}

export const handlers = [
  http.get('*/api/auth/csrf', () => HttpResponse.json({ token: 'test-csrf-token' })),
  http.get('*/api/auth/me', () => HttpResponse.json(currentUser)),
]

export const server = setupServer(...handlers)
