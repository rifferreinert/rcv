import { describe, expect, it } from 'vitest'
import { resultsPollingInterval } from '../utils'

describe('results polling', () => {
  it('polls NoVotes and InProgress only while the poll is active', () => {
    expect(resultsPollingInterval('NoVotes', 'Active')).toBe(5_000)
    expect(resultsPollingInterval('InProgress', 'Active')).toBe(5_000)
    expect(resultsPollingInterval('NoVotes', 'Closed')).toBe(false)
    expect(resultsPollingInterval('InProgress', 'Closed')).toBe(false)
    expect(resultsPollingInterval('Final', 'Active')).toBe(false)
    expect(resultsPollingInterval('Final', 'Closed')).toBe(false)
  })
})
