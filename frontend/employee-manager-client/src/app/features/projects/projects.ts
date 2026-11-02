import { Component, ElementRef, computed, effect, inject, signal, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { map } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { OrgChartService } from '../org-chart/org-chart.service';
import { directReportsOf } from './org-chart-people';
import { ProjectForm } from './project-form/project-form';
import { ProjectsService } from './projects.service';

/** Lists the projects the signed-in user is on and, for managers, the ones they run, with a New project dialog. */
@Component({
  imports: [ProjectForm, RouterLink],
  selector: 'app-projects',
  styleUrl: './projects.scss',
  templateUrl: './projects.html',
})
export class Projects {
  private readonly projectsService = inject(ProjectsService);
  private readonly orgChartService = inject(OrgChartService);
  private readonly user = inject(AuthService).currentUser;

  protected readonly isManager = computed(() => this.user()?.role === 'Manager');

  /** Only loaded for managers; undefined params leave the resource idle. */
  protected readonly managed = rxResource({
    params: () => (this.isManager() ? true : undefined),
    stream: () => this.projectsService.listManaged(),
  });
  protected readonly mine = rxResource({ stream: () => this.projectsService.listMine() });
  /** The manager's direct reports, offered as members in the New project dialog. */
  protected readonly directReports = rxResource({
    params: () => (this.isManager() ? this.user()?.employeeId : undefined),
    stream: ({ params: id }) =>
      this.orgChartService.getOrgChart(id).pipe(map((chart) => directReportsOf(chart, id))),
  });

  /** Whether the New project dialog is open. */
  protected readonly adding = signal(false);

  private readonly addDialog = viewChild<ElementRef<HTMLDialogElement>>('addDialog');

  constructor() {
    // Open the dialog as a modal once it has been rendered.
    effect(() => {
      const dialog = this.addDialog()?.nativeElement;
      if (!dialog || dialog.open) {
        return;
      }
      // jsdom may not implement showModal.
      if (typeof dialog.showModal === 'function') {
        dialog.showModal();
      } else {
        dialog.setAttribute('open', '');
      }
    });
  }

  protected add(): void {
    this.adding.set(true);
  }

  protected closeAdd(event?: Event): void {
    // Escape fires `cancel`; close through the page state so the dialog and the page stay in step.
    event?.preventDefault();
    this.adding.set(false);
  }

  protected onAdded(): void {
    this.adding.set(false);
    this.managed.reload();
    this.mine.reload();
  }
}
