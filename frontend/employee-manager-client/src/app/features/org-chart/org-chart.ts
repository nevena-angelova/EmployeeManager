import { Component, ElementRef, afterRenderEffect, inject, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';

import { OrgChartBranch } from './org-chart-branch/org-chart-branch';
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
