import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import type { TopicDetailDto, TopicSummaryDto, UserDto } from '../api/types'

export const user: UserDto = { id: 1, username: 'ton', displayName: 'Ton', twoFactorEnabled: true }

export const topicSummary = (over: Partial<TopicSummaryDto> = {}): TopicSummaryDto => ({
  id: 1,
  title: 'Hello board',
  authorName: 'Ton',
  createdAt: '2026-10-31T08:00:00Z',
  commentCount: 0,
  ...over,
})

export const topicDetail = (over: Partial<TopicDetailDto> = {}): TopicDetailDto => ({
  id: 1,
  title: 'Hello board',
  body: 'First post',
  authorName: 'Ton',
  createdAt: '2026-10-31T08:00:00Z',
  comments: [],
  ...over,
})

export const problem = (status: number, detail: string) =>
  HttpResponse.json(
    { status, detail, title: detail },
    { status, headers: { 'Content-Type': 'application/problem+json' } },
  )

export const handlers = [
  http.get('/api/topics', () => HttpResponse.json([topicSummary()])),
  http.get('/api/topics/:id', () => HttpResponse.json(topicDetail())),
]

export const requestedPaths: string[] = []

export const server = setupServer(...handlers)
