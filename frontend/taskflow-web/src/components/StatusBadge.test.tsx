import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { StatusBadge } from './StatusBadge'

describe('StatusBadge', () => {
  it('renders a human-friendly label for InProgress', () => {
    render(<StatusBadge status="InProgress" />)
    expect(screen.getByText('In progress')).toBeInTheDocument()
  })

  it('renders Todo as "To do"', () => {
    render(<StatusBadge status="Todo" />)
    expect(screen.getByText('To do')).toBeInTheDocument()
  })
})
