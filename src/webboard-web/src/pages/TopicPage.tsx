import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { useParams } from 'react-router'
import { apiRequest } from '../api/client'
import type { CommentDto, CreateCommentRequest, TopicDetailDto } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { Field, FormError, errorMessage, fieldErrorsOf, formatDateTime } from '../components'

export default function TopicPage() {
  const { id } = useParams()
  const { isAuthenticated } = useAuth()
  const [topic, setTopic] = useState<TopicDetailDto | null>(null)
  const [error, setError] = useState('')

  const load = useCallback(() => {
    apiRequest<TopicDetailDto>(`/api/topics/${encodeURIComponent(id ?? '')}`)
      .then((t) => {
        setTopic(t)
        setError('')
      })
      .catch((e: unknown) => setError(errorMessage(e)))
  }, [id])

  useEffect(load, [load])

  if (error) {
    return (
      <div className="board">
        <FormError>{error}</FormError>
      </div>
    )
  }
  if (!topic) return <div className="board">กำลังโหลด…</div>

  return (
    <div className="board">
      <article className="topic-full">
        <h3>{topic.title}</h3>
        <div className="meta">
          โดย {topic.authorName} · <time dateTime={topic.createdAt}>{formatDateTime(topic.createdAt)}</time>
        </div>
        <p className="body">{topic.body}</p>
      </article>

      <section className="comments" aria-label="รายการความเห็น">
        {topic.comments.length === 0 && <p className="empty">ยังไม่มีความเห็น</p>}
        {topic.comments.map((c: CommentDto) => (
          <div className="comment" key={c.id}>
            <span className="who">{c.authorName}</span>
            <span className="meta">
              <time dateTime={c.createdAt}>{formatDateTime(c.createdAt)}</time>
            </span>
            <span className="text">{c.body}</span>
          </div>
        ))}
      </section>

      {isAuthenticated ? (
        <NewCommentForm topicId={topic.id} onPosted={load} />
      ) : (
        <p className="gate">เข้าสู่ระบบเพื่อแสดงความเห็น</p>
      )}
    </div>
  )
}

function NewCommentForm({ topicId, onPosted }: { topicId: number; onPosted: () => void }) {
  const { request } = useAuth()
  const [body, setBody] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<unknown>(null)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await request<CommentDto>(`/api/topics/${topicId}/comments`, {
        method: 'POST',
        body: { body } satisfies CreateCommentRequest,
      })
      setBody('')
      onPosted()
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const fieldErrors = fieldErrorsOf(error)
  return (
    <form className="card" onSubmit={submit}>
      <Field label="ความเห็น" name="body" errorKey="Body" errors={fieldErrors} multiline value={body} onChange={setBody} />
      {error !== null && Object.keys(fieldErrors).length === 0 && <FormError>{errorMessage(error)}</FormError>}
      <div className="row">
        <button className="btn" type="submit" disabled={busy}>
          ส่งความเห็น
        </button>
      </div>
    </form>
  )
}
