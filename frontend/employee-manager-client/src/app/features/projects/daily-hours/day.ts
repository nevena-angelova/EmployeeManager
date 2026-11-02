/** Helpers for days written as `yyyy-MM-dd`, the format the time entries API uses. */

const dayPattern = /^(\d{4})-(\d{2})-(\d{2})$/;

/** Whether `value` is a real `yyyy-MM-dd` day (so not 2026-02-30). */
export function isDay(value: string | null | undefined): value is string {
  const match = value ? dayPattern.exec(value) : null;
  if (!match) {
    return false;
  }
  const date = toDate(value!);
  return date.getUTCMonth() === Number(match[2]) - 1 && date.getUTCDate() === Number(match[3]);
}

/** Today in UTC, like the API's default. */
export function today(now: Date = new Date()): string {
  return format(now);
}

/** The day `delta` days after `day` (negative for earlier). */
export function addDays(day: string, delta: number): string {
  const date = toDate(day);
  date.setUTCDate(date.getUTCDate() + delta);
  return format(date);
}

/** E.g. "Monday, 2 November 2026". */
export function dayLabel(day: string): string {
  return new Intl.DateTimeFormat('en-GB', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(toDate(day));
}

/** E.g. "Mon". */
export function weekdayShort(day: string): string {
  return new Intl.DateTimeFormat('en-GB', { weekday: 'short', timeZone: 'UTC' }).format(
    toDate(day),
  );
}

/** The day of the month, e.g. 2. */
export function dayOfMonth(day: string): number {
  return toDate(day).getUTCDate();
}

/** Saturday or Sunday. */
export function isWeekend(day: string): boolean {
  const weekday = toDate(day).getUTCDay();
  return weekday === 0 || weekday === 6;
}

function toDate(day: string): Date {
  const match = dayPattern.exec(day);
  if (!match) {
    throw new Error(`Not a yyyy-MM-dd day: ${day}`);
  }
  return new Date(Date.UTC(Number(match[1]), Number(match[2]) - 1, Number(match[3])));
}

function format(date: Date): string {
  return date.toISOString().slice(0, 10);
}
