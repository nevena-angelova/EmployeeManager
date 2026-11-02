import { addMonths, currentMonth, isMonth, monthLabel, nextMonth, previousMonth } from './month';

describe('month', () => {
  it('recognises yyyy-MM months', () => {
    expect(isMonth('2026-11')).toBe(true);
    expect(isMonth('2026-13')).toBe(false);
    expect(isMonth('2026-1')).toBe(false);
    expect(isMonth('')).toBe(false);
    expect(isMonth(undefined)).toBe(false);
  });

  it('takes the current month in UTC', () => {
    // 23:30 on 31 October in UTC-1 is already November in UTC.
    expect(currentMonth(new Date('2026-11-01T00:30:00Z'))).toBe('2026-11');
    expect(currentMonth(new Date('2026-10-31T23:59:59Z'))).toBe('2026-10');
  });

  it('steps across years', () => {
    expect(previousMonth('2026-01')).toBe('2025-12');
    expect(nextMonth('2026-12')).toBe('2027-01');
    expect(nextMonth('2026-11')).toBe('2026-12');
    expect(addMonths('2026-11', -23)).toBe('2024-12');
  });

  it('labels a month', () => {
    expect(monthLabel('2026-11')).toBe('November 2026');
    expect(monthLabel('2027-01')).toBe('January 2027');
  });

  it('rejects malformed months', () => {
    expect(() => nextMonth('Nov 2026')).toThrow();
  });
});
