// DTOs from task-plans/spec/spec-webboard-mvp.md §3 and spec-auth-login-2fa.md §3 (JSON is camelCase).

export interface TopicSummaryDto {
  id: number
  title: string
  authorName: string
  createdAt: string
  commentCount: number
}

export interface CommentDto {
  id: number
  topicId: number
  parentCommentId: number | null
  authorName: string
  body: string
  createdAt: string
}

export interface TopicDetailDto {
  id: number
  title: string
  body: string
  authorName: string
  createdAt: string
  comments: CommentDto[]
}

export interface CreateTopicRequest {
  title: string
  body: string
}

export interface CreateCommentRequest {
  body: string
}

export interface RegisterRequest {
  username: string
  displayName: string
  password: string
}

export interface UserDto {
  id: number
  username: string
  displayName: string
  twoFactorEnabled: boolean
}

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  mfaToken: string
  twoFactorSetupRequired: boolean
}

export interface TwoFactorSetupResponse {
  sharedKey: string
  otpAuthUri: string
}

export interface VerifyTwoFactorRequest {
  code: string
}

export interface AuthResponse {
  accessToken: string
  expiresIn: number
  user: UserDto
}
