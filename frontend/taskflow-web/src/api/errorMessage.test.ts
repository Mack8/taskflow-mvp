import { describe, expect, it } from 'vitest'
import { mutationErrorMessage } from './errorMessage'

describe('mutationErrorMessage', () => {
  it('shows the validation message returned by the API', () => {
    const error = {
      isAxiosError: true,
      response: { status: 400, data: { title: 'Validation failed', errors: { Title: ['Title is required.'] } } },
    }

    expect(mutationErrorMessage(error, 'Fallback')).toBe('Title is required.')
  })

  it('explains permission failures without exposing transport details', () => {
    const error = { isAxiosError: true, response: { status: 403, data: {} } }

    expect(mutationErrorMessage(error, 'Fallback')).toBe('You do not have permission to perform this action.')
  })
})
