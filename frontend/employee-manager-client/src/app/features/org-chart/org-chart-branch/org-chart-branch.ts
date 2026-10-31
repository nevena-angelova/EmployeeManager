import { Component, computed, input, signal } from '@angular/core';

import { OrgChartNode } from '../org-chart.model';

/** An employee card plus, recursively, the branch of everyone reporting to them. */
@Component({
  selector: 'app-org-chart-branch',
  styleUrl: './org-chart-branch.scss',
  templateUrl: './org-chart-branch.html',
})
export class OrgChartBranch {
  readonly node = input.required<OrgChartNode>();

  protected readonly collapsed = signal(false);
  protected readonly fullName = computed(() => `${this.node().firstName} ${this.node().lastName}`);
  protected readonly initials = computed(() =>
    `${this.node().firstName.charAt(0)}${this.node().lastName.charAt(0)}`.toUpperCase(),
  );
  protected readonly reportCount = computed(() => this.node().directReports.length);

  protected toggle(): void {
    this.collapsed.update((collapsed) => !collapsed);
  }
}
