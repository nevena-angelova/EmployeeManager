import { MonthStatistics } from '../allocation.model';
import { formatHours } from '../hours';

/** One month of the chart: planned and logged bars and the team's capacity, on the chart's shared scale. */
export interface MonthBar {
  /** `yyyy-MM`. */
  month: string;
  /** E.g. "Jan". */
  label: string;
  planned: number;
  logged: number;
  /** Every report's capacity added up. */
  capacityHours: number;
  /** Heights as a share of the chart, 0–100. */
  plannedPercent: number;
  loggedPercent: number;
  capacityPercent: number;
}

export interface StatisticsChart {
  bars: MonthBar[];
  /** The hours at the top of the chart. */
  scaleHours: number;
  /** A text version of the chart, for screen readers. */
  description: string;
}

/**
 * Lays out a year as grouped bars, planned beside logged for each month. Every month shares one scale, set
 * by the tallest bar or capacity, so the months compare directly. Capacity is per employee in the API, so
 * it's multiplied by the number of reports to sit on the same scale as the team's hours.
 */
export function monthBars(
  months: readonly MonthStatistics[],
  memberCount: number,
): StatisticsChart {
  const reports = Math.max(memberCount, 0);
  const capacities = months.map((m) => Math.max(m.capacityHours, 0) * reports);
  const scaleHours = Math.max(0, ...months.flatMap((m, i) => [m.planned, m.logged, capacities[i]]));
  const percent = (hours: number) => (scaleHours > 0 ? (Math.max(hours, 0) / scaleHours) * 100 : 0);

  const bars = months.map((m, i) => ({
    month: m.month,
    label: shortMonth(m.month),
    planned: m.planned,
    logged: m.logged,
    capacityHours: capacities[i],
    plannedPercent: percent(m.planned),
    loggedPercent: percent(m.logged),
    capacityPercent: percent(capacities[i]),
  }));

  const described = bars.map(
    (bar) =>
      `${bar.label}: ${formatHours(bar.planned)} planned, ${formatHours(bar.logged)} logged` +
      (reports ? ` of ${formatHours(bar.capacityHours)}` : ''),
  );
  return {
    bars,
    scaleHours,
    description: `Planned and logged hours by month. ${described.join('; ')}.`,
  };
}

/** E.g. "Jan" for `2026-01`. */
export function shortMonth(month: string): string {
  const [year, index] = month.split('-').map(Number);
  return new Intl.DateTimeFormat('en-GB', { month: 'short', timeZone: 'UTC' }).format(
    new Date(Date.UTC(year, index - 1, 1)),
  );
}
