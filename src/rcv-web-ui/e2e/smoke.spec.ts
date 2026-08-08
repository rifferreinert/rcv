import { expect, test, type Page } from '@playwright/test'

async function fixtureAuth(page: Page, signedIn = false) {
  await page.route('**/api/auth/csrf', (route) => route.fulfill({ json: { token: 'fixture-token' } }))
  await page.route('**/api/auth/me', (route) =>
    signedIn
      ? route.fulfill({ json: { id: 'user-1', email: 'alex@example.com', displayName: 'Alex Voter', provider: 'Google' } })
      : route.fulfill({ status: 401, contentType: 'application/problem+json', body: JSON.stringify({ title: 'Unauthorized', status: 401 }) }),
  )
}

test('home explains ranked choice voting and links to sign in', async ({ page }) => {
  await fixtureAuth(page)
  await page.goto('/')
  await expect(page.getByRole('heading', { name: /Better decisions start/i })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Get started' })).toHaveAttribute('href', '/login')
})

test('authenticated voter can open a fixture-backed poll', async ({ page }) => {
  await fixtureAuth(page, true)
  await page.route('**/api/polls/poll-1', (route) =>
    route.fulfill({
      json: {
        id: 'poll-1',
        title: 'Choose our park project',
        description: 'Rank the improvements you want most.',
        options: [
          { id: 'option-1', text: 'Community garden', displayOrder: 0 },
          { id: 'option-2', text: 'Playground', displayOrder: 1 },
        ],
        closesAt: '2099-08-10T18:00:00Z',
        isResultsPublic: true,
        status: 'Active',
        creator: { id: 'user-1', displayName: 'Alex Voter' },
        createdAt: '2026-08-01T18:00:00Z',
        closedAt: null,
        voteCount: 0,
      },
    }),
  )
  await page.route('**/api/polls/poll-1/votes/me', (route) =>
    route.fulfill({ json: { hasVoted: false, canChange: true, castAt: null, updatedAt: null, rankedOptionIds: [] } }),
  )

  await page.goto('/polls/poll-1')
  await expect(page.getByRole('heading', { name: 'Choose our park project' })).toBeVisible()
  await expect(page.getByRole('button', { name: /Community garden/ })).toBeVisible()
  await expect(page.getByText('Organizer controls')).toBeVisible()
})
