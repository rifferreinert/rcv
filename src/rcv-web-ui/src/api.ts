import axios, { AxiosError } from 'axios'
import type {
  Poll,
  PollInput,
  PollListResponse,
  PollResults,
  ProblemDetails,
  Provider,
  UpdatePollInput,
  User,
  VoteResponse,
  VoteStatusResponse,
} from './types'

export const api = axios.create({
  baseURL: '/api',
  withCredentials: true,
  headers: { Accept: 'application/json' },
})

let csrfReady: Promise<void> | undefined
let csrfToken: string | undefined

export function bootstrapCsrf() {
  csrfReady ??= api
    .get<{ token: string }>('/auth/csrf')
    .then(({ data }) => {
      csrfToken = data.token
    })
    .catch((error: unknown) => {
      csrfReady = undefined
      throw error
    })
  return csrfReady
}

api.interceptors.request.use(async (config) => {
  const method = config.method?.toLowerCase()
  if (method && !['get', 'head', 'options'].includes(method)) {
    await bootstrapCsrf()
    if (csrfToken) config.headers.set('X-XSRF-TOKEN', csrfToken)
  }
  return config
})

export class ApiError extends Error {
  readonly status?: number
  readonly details?: ProblemDetails

  constructor(error: AxiosError<ProblemDetails>) {
    const details = error.response?.data
    super(details?.detail ?? details?.title ?? error.message)
    this.name = 'ApiError'
    this.status = error.response?.status
    this.details = details
  }
}

api.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ProblemDetails>) => {
    if (error.response?.status === 401 && error.config?.url !== '/auth/me') {
      window.dispatchEvent(new Event('auth:unauthorized'))
    }
    return Promise.reject(new ApiError(error))
  },
)

export const authApi = {
  me: () => api.get<User>('/auth/me').then(({ data }) => data),
  loginUrl: (provider: Provider, returnTo?: string) => {
    const params = new URLSearchParams()
    if (returnTo?.startsWith('/') && !returnTo.startsWith('//')) params.set('returnUrl', returnTo)
    const query = params.toString()
    return `/api/auth/login/${provider}${query ? `?${query}` : ''}`
  },
  logout: () => api.post('/auth/logout'),
}

export interface PollListParams {
  status?: PollStatusFilter
  page: number
  pageSize: number
}

export type PollStatusFilter = 'all' | 'active' | 'closed'

export const pollsApi = {
  list: ({ status, page, pageSize }: PollListParams) =>
    api
      .get<PollListResponse>('/polls', {
        params: { status: status === 'all' ? undefined : status, page, pageSize },
      })
      .then(({ data }) => data),
  get: (id: string) => api.get<Poll>(`/polls/${id}`).then(({ data }) => data),
  create: (input: PollInput) => api.post<Poll>('/polls', input).then(({ data }) => data),
  update: (id: string, input: UpdatePollInput) =>
    api.put<Poll>(`/polls/${id}`, input).then(({ data }) => data),
  close: (id: string) => api.post<Poll>(`/polls/${id}/close`).then(({ data }) => data),
  delete: (id: string) => api.delete(`/polls/${id}`),
  voteStatus: (id: string) =>
    api.get<VoteStatusResponse>(`/polls/${id}/votes/me`).then(({ data }) => data),
  vote: (id: string, rankedOptionIds: string[]) =>
    api
      .post<VoteResponse>(`/polls/${id}/votes`, { rankedOptionIds })
      .then(({ data }) => data),
  results: (id: string) =>
    api.get<PollResults>(`/polls/${id}/results`).then(({ data }) => data),
}

export function errorMessage(error: unknown) {
  if (error instanceof ApiError) {
    const validation = error.details?.errors
    if (validation) return Object.values(validation).flat().join(' ')
    return error.message
  }
  return error instanceof Error ? error.message : 'Something went wrong. Please try again.'
}
