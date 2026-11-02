import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { lineColour, teamLines } from '../../../shared/team-lines';
import { OrgChartService } from '../../org-chart/org-chart.service';
import { directReportsOf, flattenOrgChart } from '../org-chart-people';
import { ProjectForm } from '../project-form/project-form';
import { Project } from '../project.model';
import { ProjectsService } from '../projects.service';

/** One project and its members. Its owner can edit it inline or delete it. */
@Component({
  imports: [ProjectForm, RouterLink],
  selector: 'app-project-detail',
  styleUrl: './project-detail.scss',
  templateUrl: './project-detail.html',
})
export class ProjectDetail {
  private readonly projectsService = inject(ProjectsService);
  private readonly orgChartService = inject(OrgChartService);
  private readonly router = inject(Router);
  private readonly user = inject(AuthService).currentUser;

  /** Bound from the `:id` route param. */
  readonly id = input.required<string>();

  protected readonly project = rxResource({
    params: () => this.id(),
    stream: ({ params: id }) => this.projectsService.get(id),
  });
  /** Everyone, for the members' team colours and the owner's direct reports. */
  protected readonly orgChart = rxResource({ stream: () => this.orgChartService.getOrgChart() });

  protected readonly isOwner = computed(
    () => !!this.project.value() && this.project.value()?.ownerId === this.user()?.employeeId,
  );
  protected readonly lines = computed(() =>
    teamLines(flattenOrgChart(this.orgChart.value() ?? [])),
  );
  protected readonly directReports = computed(() =>
    directReportsOf(this.orgChart.value() ?? [], this.user()?.employeeId ?? ''),
  );

  /** Whether the edit form is showing; resets when moving to another project. */
  protected readonly editing = linkedSignal({ source: this.id, computation: () => false });
  protected readonly deleting = signal(false);
  protected readonly deleteError = signal<string | null>(null);

  protected readonly notFound = computed(() => {
    const error = this.project.error();
    return error instanceof HttpErrorResponse && error.status === 404;
  });

  /** The member's team line colour, as on the org chart. */
  protected colour(id: string): string | null {
    return lineColour(this.lines(), id);
  }

  protected onSaved(project: Project): void {
    this.project.set(project);
    this.editing.set(false);
  }

  protected delete(project: Project): void {
    if (this.deleting() || !confirm(`Delete the project “${project.name}”?`)) {
      return;
    }

    this.deleting.set(true);
    this.deleteError.set(null);
    this.projectsService.delete(project.id).subscribe({
      next: () => this.router.navigate(['/projects']),
      error: (error: unknown) => {
        this.deleting.set(false);
        if (error instanceof HttpErrorResponse && error.status === 404) {
          // Already gone.
          this.router.navigate(['/projects']);
        } else {
          this.deleteError.set(`Couldn't delete “${project.name}”. Please try again.`);
        }
      },
    });
  }
}
