import { useId, type ReactNode } from 'react'
import { ApiError } from './api/client'

/** Server times are UTC (timestamptz); show them in the viewer's local time (NFR-002). */
export function formatDateTime(iso: string): string {
  const hasZone = /(Z|[+-]\d{2}:?\d{2})$/.test(iso)
  const date = new Date(hasZone ? iso : `${iso}Z`)
  return date.toLocaleString('th-TH', { dateStyle: 'medium', timeStyle: 'short', hour12: false })
}

export function errorMessage(err: unknown): string {
  return err instanceof Error ? err.message : 'เกิดข้อผิดพลาด'
}

export function fieldErrorsOf(err: unknown): Record<string, string[]> {
  return err instanceof ApiError ? err.fieldErrors : {}
}

interface FieldProps {
  label: string
  name: string
  /** Server validation errors keyed by property name (PascalCase from ASP.NET). */
  errors?: Record<string, string[]>
  errorKey?: string
  multiline?: boolean
  type?: string
  autoComplete?: string
  placeholder?: string
  maxLength?: number
  inputMode?: 'numeric'
  className?: string
  value: string
  onChange: (value: string) => void
}

export function Field(props: FieldProps) {
  const id = useId()
  const messages = props.errors?.[props.errorKey ?? props.name] ?? []
  const common = {
    id,
    name: props.name,
    value: props.value,
    placeholder: props.placeholder,
    onChange: (e: { target: { value: string } }) => props.onChange(e.target.value),
  }
  return (
    <div className={messages.length ? 'field bad' : 'field'}>
      <label htmlFor={id}>{props.label}</label>
      {props.multiline ? (
        <textarea {...common} />
      ) : (
        <input
          {...common}
          type={props.type ?? 'text'}
          autoComplete={props.autoComplete ?? 'off'}
          inputMode={props.inputMode}
          maxLength={props.maxLength}
          className={props.className}
        />
      )}
      {messages.map((m) => (
        <span className="err" key={m}>
          {m}
        </span>
      ))}
    </div>
  )
}

export function FormError({ children }: { children: ReactNode }) {
  return children ? (
    <p className="err form-err" role="alert">
      {children}
    </p>
  ) : null
}
