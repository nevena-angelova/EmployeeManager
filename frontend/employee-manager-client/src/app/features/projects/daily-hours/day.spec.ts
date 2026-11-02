import { addDays, dayLabel, dayOfMonth, isDay, isWeekend, today, weekdayShort } from './day';

describe('day', () => {
  it('recognises real yyyy-MM-dd days', () => {
    expect(isDay('2026-11-02')).toBe(true);
    expect(isDay('2028-02-29')).toBe(true);
    expect(isDay('2026-02-30')).toBe(false);
    expect(isDay('2026-13-01')).toBe(false);
    expect(isDay('2026-11')).toBe(false);
    expect(isDay(undefined)).toBe(false);
  });

  it('takes today in UTC', () => {
    expect(today(new Date('2026-11-02T23:30:00-05:00'))).toBe('2026-11-03');
  });

  it('adds days across months and years', () => {
    expect(addDays('2026-11-30', 1)).toBe('2026-12-01');
    expect(addDays('2027-01-01', -1)).toBe('2026-12-31');
    expect(addDays('2026-11-02', 7)).toBe('2026-11-09');
  });

  it('describes days', () => {
    expect(dayLabel('2026-11-02')).toBe('Monday, 2 November 2026');
    expect(weekdayShort('2026-11-02')).toBe('Mon');
    expect(dayOfMonth('2026-11-02')).toBe(2);
    expect(isWeekend('2026-11-07')).toBe(true);
    expect(isWeekend('2026-11-06')).toBe(false);
  });
});
