/** Helpers for months written as `yyyy-MM`, the format the allocations API uses. */

const monthPattern = /^(\d{4})-(0[1-9]|1[0-2])$/;

/** Whether `value` is a `yyyy-MM` month. */
export function isMonth(value: string | null | undefined): value is string {
  return !!value && monthPattern.test(value);
}

/** The current month in UTC, like the API's default. */
export function currentMonth(now: Date = new Date()): string {
  return format(now.getUTCFullYear(), now.getUTCMonth());
}

/** The month `delta` months after `month` (negative for earlier). */
export function addMonths(month: string, delta: number): string {
  const [year, index] = parse(month);
  const total = year * 12 + index + delta;
  return format(Math.floor(total / 12), ((total % 12) + 12) % 12);
}

export function previousMonth(month: string): string {
  return addMonths(month, -1);
}

export function nextMonth(month: string): string {
  return addMonths(month, 1);
}

/** E.g. "November 2026". */
export function monthLabel(month: string): string {
  const [year, index] = parse(month);
  return new Intl.DateTimeFormat('en-GB', {
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(Date.UTC(year, index, 1)));
}

/** The year and zero-based month index. */
function parse(month: string): [number, number] {
  const match = monthPattern.exec(month);
  if (!match) {
    throw new Error(`Not a yyyy-MM month: ${month}`);
  }
  return [Number(match[1]), Number(match[2]) - 1];
}

function format(year: number, index: number): string {
  return `${String(year).padStart(4, '0')}-${String(index + 1).padStart(2, '0')}`;
}
