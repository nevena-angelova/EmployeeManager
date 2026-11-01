import {
  Component,
  ElementRef,
  afterRenderEffect,
  computed,
  inject,
  viewChild,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';

import { TeamMember, lineColour, teamLines } from '../../shared/team-lines';
import { OrgChartBranch } from './org-chart-branch/org-chart-branch';
import { OrgChartNode } from './org-chart.model';
import { OrgChartService } from './org-chart.service';

@Component({
  imports: [OrgChartBranch],
  selector: 'app-org-chart',
  styleUrl: './org-chart.scss',
  templateUrl: './org-chart.html',
})
export class OrgChart {
  private readonly orgChartService = inject(OrgChartService);
  private readonly scroller = viewChild<ElementRef<HTMLElement>>('scroller');

  protected readonly orgChart = rxResource({ stream: () => this.orgChartService.getOrgChart() });
  protected readonly lines = computed(() => teamLines(flatten(this.orgChart.value() ?? [])));

  /** The key above the chart: each team lead, their line colour and how many people are on it. */
  protected readonly teams = computed(() =>
    (this.orgChart.value() ?? []).flatMap((root) =>
      root.directReports.map((lead) => ({
        lead,
        colour: lineColour(this.lines(), lead.id),
        size: countPeople(lead),
      })),
    ),
  );

  constructor() {
    // The top of the chart sits in the middle, so start a chart wider than the screen scrolled to its centre.
    afterRenderEffect(() => {
      const scroller = this.scroller()?.nativeElement;
      if (scroller && this.orgChart.value()) {
        scroller.scrollLeft = (scroller.scrollWidth - scroller.clientWidth) / 2;
      }
    });
  }
}

function flatten(nodes: OrgChartNode[], managerId: string | null = null): TeamMember[] {
  return nodes.flatMap((node) => [
    { id: node.id, managerId, firstName: node.firstName, lastName: node.lastName },
    ...flatten(node.directReports, node.id),
  ]);
}

function countPeople(node: OrgChartNode): number {
  return 1 + node.directReports.reduce((sum, report) => sum + countPeople(report), 0);
}
