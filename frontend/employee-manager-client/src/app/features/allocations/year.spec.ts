import { addYears, currentYear, isYear } from './year';

describe('year', () => {
  it('recognises yyyy years', () => {
    expect(isYear('2026')).toBe(true);
    expect(isYear('26')).toBe(false);
    expect(isYear('2026-11')).toBe(false);
    expect(isYear('')).toBe(false);
    expect(isYear(undefined)).toBe(false);
  });

  it('takes the current year in UTC', () => {
    expect(currentYear(new Date('2027-01-01T00:30:00Z'))).toBe('2027');
    expect(currentYear(new Date('2026-12-31T23:59:59Z'))).toBe('2026');
  });

  it('steps between years', () => {
    expect(addYears('2026', -1)).toBe('2025');
    expect(addYears('2026', 1)).toBe('2027');
  });

  it('rejects malformed years', () => {
    expect(() => addYears('20x6', 1)).toThrow();
  });
});
