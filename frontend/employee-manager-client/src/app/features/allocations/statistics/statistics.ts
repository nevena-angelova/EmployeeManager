import { Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';

import { lineColour, teamLines } from '../../../shared/team-lines';
import { OrgChartService } from '../../org-chart/org-chart.service';
import { flattenOrgChart } from '../../projects/org-chart-people';
import { MonthStatistics } from '../allocation.model';
import { AllocationsService } from '../allocations.service';
import { projectColour } from '../hour-rail';
import { formatHours } from '../hours';
import { monthLabel } from '../month';
import { addYears, currentYear, isYear } from '../year';
import { monthBars, shortMonth } from './statistics-chart';

/**
 * A manager's year of hours: their direct reports' planned and logged hours on the manager's projects, month
 * by month, as a chart and tables. The year comes from the `?year=` query param (default: the current UTC
 * year).
 */
@Component({
  imports: [RouterLink],
  selector: 'app-statistics',
  styleUrl: './statistics.scss',
  templateUrl: './statistics.html',
})
export class Statistics {
  private readonly allocationsService = inject(AllocationsService);
  private readonly orgChartService = inject(OrgChartService);
  private readonly router = inject(Router);

  /** Bound from the `?year=` query param; anything that isn't `yyyy` means the current year. */
  readonly year = input<string>();

  protected readonly selectedYear = computed(() => {
    const year = this.year();
    return isYear(year) ? year : currentYear();
  });

  protected readonly statistics = rxResource({
    params: () => this.selectedYear(),
    stream: ({ params: year }) => this.allocationsService.getTeamStatistics(year),
  });
  /** Everyone, for the reports' team colours. */
  protected readonly orgChart = rxResource({
    stream: () => this.orgChartService.getOrgChart(),
  });
  protected readonly lines = computed(() =>
    teamLines(flattenOrgChart(this.orgChart.value() ?? [])),
  );

  protected readonly chart = computed(() => {
    const statistics = this.statistics.value();
    return statistics ? monthBars(statistics.months, statistics.members.length) : null;
  });

  /** Logged hours as a whole percentage of planned, or null when nothing was planned. */
  protected readonly loggedPercent = computed(() => {
    const statistics = this.statistics.value();
    return statistics?.totalPlanned
      ? Math.round((statistics.totalLogged / statistics.totalPlanned) * 100)
      : null;
  });

  protected previous(): void {
    this.goTo(addYears(this.selectedYear(), -1));
  }

  protected next(): void {
    this.goTo(addYears(this.selectedYear(), 1));
  }

  /** The report's team line colour, as on the org chart. */
  protected colour(id: string): string | null {
    return lineColour(this.lines(), id);
  }

  protected projectColour(index: number): string {
    return projectColour(index).edge;
  }

  /** Every report's capacity added up. */
  protected teamCapacity(month: MonthStatistics, memberCount: number): number {
    return month.capacityHours * memberCount;
  }

  /** Logged minus planned, with a sign: "+4", "−12.5", "0". */
  protected difference(planned: number, logged: number): string {
    const difference = logged - planned;
    const formatted = formatHours(Math.abs(difference));
    if (formatted === '0') {
      return '0';
    }
    return `${difference > 0 ? '+' : '−'}${formatted}`;
  }

  protected shortMonth(month: string): string {
    return shortMonth(month);
  }

  protected monthLabel(month: string): string {
    return monthLabel(month);
  }

  protected format(hours: number): string {
    return formatHours(hours);
  }

  private goTo(year: string): void {
    this.router.navigate([], { queryParams: { year } });
  }
}
