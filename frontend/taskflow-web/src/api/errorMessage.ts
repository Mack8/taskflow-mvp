import axios from 'axios'
import type { ApiErrorPayload } from '../types'

export function mutationErrorMessage(error: unknown, fallback: string): string {
  if (!axios.isAxiosError<ApiErrorPayload>(error)) return fallback

  if (error.response?.status === 403) {
    return 'You do not have permission to perform this action.'
  }

  const errors = error.response?.data?.errors
  const validationMessage = errors && Object.values(errors).flat()[0]
  if (validationMessage) return validationMessage

  if (error.response?.status === 400) {
    return 'Check the form and try again.'
  }

  return fallback
}
