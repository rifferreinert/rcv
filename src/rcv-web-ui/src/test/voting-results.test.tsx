import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from './render'
import { openPoll, server } from './server'

describe('voting and results', () => {
  it('submits a partial ranked ballot and supports non-drag reordering', async () => {
    const user = userEvent.setup()
    const castVote = vi.fn()
    server.use(
      http.get('*/api/polls/poll-1', () => HttpResponse.json(openPoll)),
      http.get('*/api/polls/poll-1/votes/me', () => HttpResponse.json({
        hasVoted: false,
        canChange: true,
        castAt: null,
        updatedAt: null,
        rankedOptionIds: [],
      })),
      http.post('*/api/polls/poll-1/votes', async ({ request }) => {
        castVote(await request.json())
        return HttpResponse.json({
          pollId: 'poll-1',
          id: 'vote-1',
          updatedAt: '2026-08-07T18:00:00Z',
          castAt: '2026-08-07T18:00:00Z',
          rankedChoices: ['option-2', 'option-1'],
        })
      }),
    )
    renderApp('/polls/poll-1')

    await user.click(await screen.findByRole('button', { name: /Community garden/ }))
    await user.click(screen.getByRole('button', { name: /Playground/ }))
    await user.click(screen.getByRole('button', { name: 'Move Playground up' }))
    await user.click(screen.getByRole('button', { name: 'Submit ballot' }))

    expect(await screen.findByText('Your ballot was saved.')).toBeInTheDocument()
    expect(castVote).toHaveBeenCalledWith({ rankedOptionIds: ['option-2', 'option-1'] })

    await user.click(screen.getByRole('button', { name: 'Delete poll' }))
    expect(screen.getByText('This removes the poll from your dashboard and makes its link unavailable. This cannot be undone.')).toBeInTheDocument()
  })

  it('shows a final winner and an accessible round table', async () => {
    server.use(
      http.get('*/api/polls/poll-1/results', () => HttpResponse.json({
        state: 'Final',
        pollId: 'poll-1',
        totalVotes: 5,
        winner: openPoll.options[0],
        isTie: false,
        tiedOptions: [],
        rounds: [{
          roundNumber: 1,
          voteCounts: { 'option-1': 3, 'option-2': 2 },
          eliminatedOptionId: 'option-2',
        }],
        calculatedAt: '2026-08-07T18:00:00Z',
      })),
      http.get('*/api/polls/poll-1', () => HttpResponse.json({ ...openPoll, status: 'Closed', closedAt: '2026-08-07T18:00:00Z' })),
    )
    renderApp('/polls/poll-1/results')
    expect(await screen.findByText('Winner')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Community garden' })).toBeInTheDocument()
    expect(screen.getByRole('table')).toHaveAccessibleName('Vote totals for round 1')
  })
})
