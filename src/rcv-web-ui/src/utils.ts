import type { PollStatus, ResultsStatus } from './types'

export function formatDate(value: string | null) {
  if (!value) return 'No deadline'
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

export function resultsPollingInterval(resultsState?: ResultsStatus, pollStatus?: PollStatus) {
  const resultsCanChange = resultsState === 'NoVotes' || resultsState === 'InProgress'
  return pollStatus === 'Active' && resultsCanChange ? 5_000 : false
}
