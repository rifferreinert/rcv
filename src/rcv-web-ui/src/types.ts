export type Provider = 'google' | 'microsoft'
export type PollStatus = 'Active' | 'Closed'
export type ResultsStatus = 'NoVotes' | 'InProgress' | 'Final'

export interface User {
  id: string
  email: string | null
  displayName: string | null
  provider: string
}

export interface UserSummary {
  id: string
  displayName: string | null
}

export interface PollOption {
  id: string
  text: string
  displayOrder: number
}

export interface Poll {
  id: string
  title: string
  description: string | null
  options: PollOption[]
  closesAt: string | null
  isResultsPublic: boolean
  status: PollStatus
  creator: UserSummary
  createdAt: string
  closedAt: string | null
  voteCount: number
}

export interface PollListResponse {
  items: Poll[]
  page: number
  pageSize: number
  totalCount: number
}

export interface PollInput {
  title: string
  description: string
  options: string[]
  closesAt: string | null
  isResultsPublic: boolean
}

export interface UpdatePollInput extends PollInput {
  removeClosesAt: boolean
}

export interface VoteStatusResponse {
  hasVoted: boolean
  canChange: boolean
  castAt: string | null
  updatedAt: string | null
  rankedOptionIds: string[]
}

export interface VoteResponse {
  id: string
  pollId: string
  rankedChoices: string[]
  castAt: string
  updatedAt: string | null
}

export interface ResultOption {
  id: string
  text: string
}

export interface ResultsRound {
  roundNumber: number
  voteCounts: Record<string, number>
  eliminatedOptionId: string | null
}

export interface PollResults {
  state: ResultsStatus
  pollId: string
  totalVotes: number
  winner: ResultOption | null
  isTie: boolean
  tiedOptions: ResultOption[]
  rounds: ResultsRound[]
  calculatedAt: string
}

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  errors?: Record<string, string[]>
}
