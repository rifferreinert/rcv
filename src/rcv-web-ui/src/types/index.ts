// ============================================================
// Domain Types
// ============================================================

export interface PollOption {
  id: string;
  text: string;
  displayOrder: number;
}

export interface UserSummary {
  id: string;
  name: string;
  email: string;
}

export type PollStatus = 'Active' | 'Closed' | 'Deleted';

export interface Poll {
  id: string;
  title: string;
  description?: string;
  creator: UserSummary;
  options: PollOption[];
  status: PollStatus;
  createdAt: string;        // ISO 8601
  closesAt?: string;        // ISO 8601
  closedAt?: string;        // ISO 8601
  isResultsPublic: boolean;
  isVotingPublic: boolean;
  voteCount: number;
}

export interface Vote {
  id: string;
  pollId: string;
  rankedOptionIds: string[];
  castAt: string;           // ISO 8601
  updatedAt?: string;       // ISO 8601
}

export interface RoundSummary {
  roundNumber: number;
  voteCounts: Record<string, number>;   // optionId -> count
  eliminatedOption?: PollOption;
}

export interface Result {
  pollId: string;
  winner?: PollOption;      // null if tie
  isTie: boolean;
  tiedOptions: PollOption[];
  rounds: RoundSummary[];
  finalVoteTotals: Record<string, number>;  // optionId -> count
  totalVotes: number;
}

// ============================================================
// Request Types
// ============================================================

export interface CreatePollRequest {
  title: string;
  description?: string;
  options: string[];
  closesAt?: string;        // ISO 8601
  isResultsPublic: boolean;
  isVotingPublic: boolean;
}

export interface UpdatePollRequest {
  title?: string;
  description?: string;
  closesAt?: string;        // ISO 8601
  isResultsPublic?: boolean;
  isVotingPublic?: boolean;
}

export interface CastVoteRequest {
  rankedOptionIds: string[];  // GUIDs in ranked order
}

// ============================================================
// Response Types
// ============================================================

export interface PollResponse extends Poll {}

export interface PollListResponse {
  items: Poll[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface VoteResponse {
  id: string;
  pollId: string;
  castAt: string;
  updatedAt?: string;
}

export interface VoteStatusResponse {
  hasVoted: boolean;
  canChange: boolean;
  votedAt?: string;
}

export interface VoteCountResponse {
  totalVotes: number;
  uniqueVoters: number;
}

export interface ResultResponse extends Result {}

// ============================================================
// Auth Types
// ============================================================

export interface User {
  id: string;
  email: string;
  displayName: string;
  provider: string;
}
