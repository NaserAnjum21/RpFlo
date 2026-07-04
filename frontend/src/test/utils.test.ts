import { describe, expect, it, vi } from 'vitest';
import { cn, formatDate, formatDateTime, formatRelativeTime } from '@/lib/utils';

describe('utils', () => {
  it('merges conditional Tailwind classes with later conflicting values winning', () => {
    expect(cn('px-2 text-sm', 'px-4')).toBe('text-sm px-4');
  });

  it.each([
    ['2026-07-01T12:00:00.000Z', '1st July, 2026'],
    ['2026-07-02T12:00:00.000Z', '2nd July, 2026'],
    ['2026-07-03T12:00:00.000Z', '3rd July, 2026'],
    ['2026-07-11T12:00:00.000Z', '11th July, 2026'],
  ])('formats ordinal dates for %s', (iso, expected) => {
    expect(formatDate(iso)).toBe(expected);
  });

  it('formats date and time in the Dhaka timezone', () => {
    expect(formatDateTime('2026-07-01T18:30:00.000Z')).toBe('2nd July, 2026 at 12:30 AM');
  });

  it('formats relative time boundaries', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-07-04T12:00:00.000Z'));

    expect(formatRelativeTime('2026-07-04T11:59:45.000Z')).toBe('just now');
    expect(formatRelativeTime('2026-07-04T11:55:00.000Z')).toBe('5m');
    expect(formatRelativeTime('2026-07-04T09:00:00.000Z')).toBe('3h');
    expect(formatRelativeTime('2026-07-02T12:00:00.000Z')).toBe('2d');

    vi.useRealTimers();
  });
});
