import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import type { CreateTopicRequest, TopicSummaryDto } from '../api/types'
import { apiRequest } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Field, FormError, errorMessage, fieldErrorsOf, formatDateTime } from '../components'

export default function TopicsPage() {
  const { isAuthenticated } = useAuth()
  const [topics, setTopics] = useState<TopicSummaryDto[] | null>(null)
  const [loadError, setLoadError] = useState('')

  const load = useCallback(() => {
    apiRequest<TopicSummaryDto[]>('/api/topics')
      .then((list) => {
        setTopics(list)
        setLoadError('')
      })
      .catch((e: unknown) => setLoadError(errorMessage(e)))
  }, [])

  useEffect(load, [load])

  return (
    <div className="board">
      <div className="board-title">
        <h3>กระทู้ทั้งหมด</h3>
      </div>
      {isAuthenticated && <NewTopicForm />}
      <FormError>{loadError}</FormError>
      {topics?.length === 0 && <p className="empty">ยังไม่มีกระทู้</p>}
      {topics && topics.length > 0 && (
        <ul className="topics">
          {topics.map((t) => (
            <li key={t.id}>
              <div className="main">
                <Link className="link" to={`/topics/${t.id}`}>
                  {t.title}
                </Link>
                <div className="meta">
                  โดย {t.authorName} · <time dateTime={t.createdAt}>{formatDateTime(t.createdAt)}</time>
                </div>
              </div>
              <span className="count">{t.commentCount} ความเห็น</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

function NewTopicForm() {
  const { request } = useAuth()
  const navigate = useNavigate()
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<unknown>(null)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const created = await request<TopicSummaryDto>('/api/topics', {
        method: 'POST',
        body: { title, body } satisfies CreateTopicRequest,
      })
      navigate(`/topics/${created.id}`)
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  const fieldErrors = fieldErrorsOf(error)
  return (
    <form className="card" onSubmit={submit}>
      <Field label="หัวข้อกระทู้" name="title" errorKey="Title" errors={fieldErrors} placeholder="ไม่เกิน 200 ตัวอักษร" value={title} onChange={setTitle} />
      <Field label="เนื้อหา" name="body" errorKey="Body" errors={fieldErrors} multiline value={body} onChange={setBody} />
      {error !== null && Object.keys(fieldErrors).length === 0 && <FormError>{errorMessage(error)}</FormError>}
      <div className="row">
        <button className="btn" type="submit" disabled={busy}>
          ตั้งกระทู้
        </button>
      </div>
    </form>
  )
}
