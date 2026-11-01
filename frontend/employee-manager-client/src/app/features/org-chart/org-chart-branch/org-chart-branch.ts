import { Component, computed, input, signal } from '@angular/core';

import { lineColour } from '../../../shared/team-lines';
import { OrgChartNode } from '../org-chart.model';

/** An employee's station on the chart plus, recursively, the branch of everyone reporting to them. */
@Component({
  selector: 'app-org-chart-branch',
  styleUrl: './org-chart-branch.scss',
  templateUrl: './org-chart-branch.html',
})
export class OrgChartBranch {
  readonly node = input.required<OrgChartNode>();
  /** Each employee's team line (see `teamLines`); people without one are drawn in ink. */
  readonly lines = input<ReadonlyMap<string, number>>(new Map());

  protected readonly collapsed = signal(false);
  /** Draw the reports' lines in only when someone opens the team, not on first load. */
  protected readonly revealOnOpen = signal(false);
  protected readonly fullName = computed(() => `${this.node().firstName} ${this.node().lastName}`);
  protected readonly onLine = computed(() => this.lines().has(this.node().id));
  protected readonly reportCount = computed(() => this.node().directReports.length);

  protected colour(id: string): string | null {
    return lineColour(this.lines(), id);
  }

  protected toggle(): void {
    this.revealOnOpen.set(true);
    this.collapsed.update((collapsed) => !collapsed);
  }
}
