/** Hours to one decimal place, without trailing zeros: 40, 22.5. */
export function formatHours(hours: number): string {
  return String(Math.round(hours * 10) / 10);
}
