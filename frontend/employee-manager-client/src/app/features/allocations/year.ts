/** Helpers for years written as `yyyy`, the format the statistics API uses. */

const yearPattern = /^\d{4}$/;

/** Whether `value` is a `yyyy` year. */
export function isYear(value: string | null | undefined): value is string {
  return !!value && yearPattern.test(value);
}

/** The current year in UTC, like the API's default. */
export function currentYear(now: Date = new Date()): string {
  return String(now.getUTCFullYear()).padStart(4, '0');
}

/** The year `delta` years after `year` (negative for earlier). */
export function addYears(year: string, delta: number): string {
  if (!isYear(year)) {
    throw new Error(`Not a yyyy year: ${year}`);
  }
  return String(Number(year) + delta).padStart(4, '0');
}
