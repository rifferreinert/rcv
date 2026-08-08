import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it, vi } from 'vitest'
import { renderApp } from './render'
import { openPoll, server } from './server'

describe('poll creation', () => {
  it('validates the form and creates a poll with unique options', async () => {
    const user = userEvent.setup()
    const created = vi.fn()
    server.use(
      http.post('*/api/polls', async ({ request }) => {
        expect(request.headers.get('X-XSRF-TOKEN')).toBe('test-csrf-token')
        created(await request.json())
        return HttpResponse.json({ ...openPoll, title: 'Lunch location' }, { status: 201 })
      }),
      http.get('*/api/polls/poll-1', () => HttpResponse.json({ ...openPoll, title: 'Lunch location' })),
      http.get('*/api/polls/poll-1/votes/me', () => HttpResponse.json({
        hasVoted: false,
        canChange: true,
        castAt: null,
        updatedAt: null,
        rankedOptionIds: [],
      })),
    )
    renderApp('/polls/new')

    await user.click(await screen.findByRole('button', { name: 'Create poll' }))
    expect(await screen.findByText('Enter a title.')).toBeInTheDocument()

    await user.type(screen.getByLabelText('Title'), 'Lunch location')
    const optionInputs = screen.getAllByLabelText(/Option \d/)
    await user.type(optionInputs[0], 'Cafe')
    await user.type(optionInputs[1], 'Food hall')
    await user.click(screen.getByRole('button', { name: 'Create poll' }))

    expect(await screen.findByRole('heading', { name: 'Lunch location' })).toBeInTheDocument()
    expect(created).toHaveBeenCalledWith(expect.objectContaining({
      title: 'Lunch location',
      options: ['Cafe', 'Food hall'],
      closesAt: null,
      isResultsPublic: true,
    }))
  })

  it('does not expose editing controls to a non-creator', async () => {
    server.use(
      http.get('*/api/polls/poll-1', () => HttpResponse.json({
        ...openPoll,
        creator: { id: 'user-2', displayName: 'Another organizer' },
      })),
    )
    renderApp('/polls/poll-1/edit')

    expect(await screen.findByText('Only the poll creator can edit this poll.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Save changes' })).not.toBeInTheDocument()
  })

  it('explicitly removes an existing deadline when editing', async () => {
    const user = userEvent.setup()
    const updated = vi.fn()
    server.use(
      http.get('*/api/polls/poll-1', () => HttpResponse.json(openPoll)),
      http.put('*/api/polls/poll-1', async ({ request }) => {
        updated(await request.json())
        return HttpResponse.json({ ...openPoll, closesAt: null })
      }),
      http.get('*/api/polls/poll-1/votes/me', () => HttpResponse.json({
        hasVoted: false,
        canChange: true,
        castAt: null,
        updatedAt: null,
        rankedOptionIds: [],
      })),
    )
    renderApp('/polls/poll-1/edit')

    const deadline = await screen.findByRole('checkbox', { name: /Set a deadline/ })
    expect(deadline).toBeChecked()
    await user.click(deadline)
    await user.click(screen.getByRole('button', { name: 'Save changes' }))

    expect(await screen.findByRole('heading', { name: openPoll.title })).toBeInTheDocument()
    expect(updated).toHaveBeenCalledWith(expect.objectContaining({
      closesAt: null,
      removeClosesAt: true,
    }))
  })
})
