import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';

import { OrgChartNode } from '../../org-chart/org-chart.model';
import { Project, ProjectSave } from '../project.model';
import { ProjectsService } from '../projects.service';

type FieldName = 'name' | 'description' | 'memberIds';

const fieldNames: readonly FieldName[] = ['name', 'description', 'memberIds'];

/** Someone who can be ticked as a member. */
interface MemberOption {
  id: string;
  name: string;
  jobTitle: string;
  /** A member who no longer reports to the owner: they can be removed but not added back. */
  former: boolean;
}

/**
 * Form that creates a project or edits one, including who is on it.
 * The host supplies the heading: the Projects page shows it in its New project dialog, the project page inline.
 */
@Component({
  imports: [ReactiveFormsModule],
  selector: 'app-project-form',
  styleUrl: './project-form.scss',
  templateUrl: './project-form.html',
})
export class ProjectForm implements OnInit {
  private readonly projectsService = inject(ProjectsService);

  /** The project being edited, or null to create one. */
  readonly project = input<Project | null>(null);
  /** The signed-in manager's direct reports, who can be added. */
  readonly directReports = input<OrgChartNode[]>([]);

  readonly saved = output<Project>();
  readonly cancelled = output<void>();

  protected readonly isEdit = computed(() => this.project() !== null);

  /** Direct reports, then current members who have moved to another manager. */
  protected readonly options = computed<MemberOption[]>(() => {
    const reports = this.directReports();
    const reportIds = new Set(reports.map((r) => r.id));
    return [
      ...reports.map((r) => ({
        id: r.id,
        name: `${r.firstName} ${r.lastName}`,
        jobTitle: r.jobTitle,
        former: false,
      })),
      ...(this.project()?.members ?? [])
        .filter((m) => !reportIds.has(m.employeeId))
        .map((m) => ({
          id: m.employeeId,
          name: `${m.firstName} ${m.lastName}`,
          jobTitle: m.jobTitle,
          former: true,
        })),
    ];
  });

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    description: ['', Validators.maxLength(2000)],
  });
  protected readonly selected = signal<ReadonlySet<string>>(new Set());
  protected readonly submitting = signal(false);
  protected readonly serverErrors = signal<Partial<Record<FieldName, string>>>({});
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const project = this.project();
    if (project) {
      this.form.patchValue({ name: project.name, description: project.description ?? '' });
      this.selected.set(new Set(project.members.map((m) => m.employeeId)));
    }
  }

  /** The message to show under a field: a failed check, else what the API said. */
  protected fieldError(name: FieldName): string | null {
    if (name !== 'memberIds') {
      const control = this.form.controls[name];
      if (control.touched && control.invalid) {
        return control.hasError('required')
          ? 'Enter a project name.'
          : `Use at most ${name === 'name' ? 150 : 2000} characters.`;
      }
    }
    return this.serverErrors()[name] ?? null;
  }

  protected toggle(id: string, checked: boolean): void {
    const next = new Set(this.selected());
    if (checked) {
      next.add(id);
    } else {
      next.delete(id);
    }
    this.selected.set(next);
  }

  protected submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, description } = this.form.getRawValue();
    const body: ProjectSave = {
      name: name.trim(),
      description: description.trim() || null,
      // Keep the options' order so the request is predictable.
      memberIds: this.options()
        .map((o) => o.id)
        .filter((id) => this.selected().has(id)),
    };
    const project = this.project();
    const request: Observable<Project> = project
      ? this.projectsService.update(project.id, body)
      : this.projectsService.create(body);

    this.submitting.set(true);
    this.serverErrors.set({});
    this.error.set(null);

    request.subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.saved.emit(result);
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.showErrors(error);
      },
    });
  }

  protected cancel(): void {
    this.cancelled.emit();
  }

  /** Puts 400 validation errors under their fields; anything else becomes a form-level message. */
  private showErrors(error: unknown): void {
    const errors =
      error instanceof HttpErrorResponse && error.status === 400
        ? (error.error?.errors as Record<string, string[]> | undefined)
        : undefined;
    if (!errors) {
      this.error.set(
        error instanceof HttpErrorResponse && error.status === 404
          ? 'This project no longer exists.'
          : "Couldn't save the project. Please try again.",
      );
      return;
    }

    const fields: Partial<Record<FieldName, string>> = {};
    const other: string[] = [];
    for (const [key, messages] of Object.entries(errors)) {
      const name = toFieldName(key);
      if (name) {
        fields[name] = messages.join(' ');
      } else {
        other.push(...messages);
      }
    }
    this.serverErrors.set(fields);
    this.error.set(other.length ? other.join(' ') : null);
  }
}

/** Maps an API error key (`name`, `Name`, `$.name`, `request.Name`) to a form field. */
function toFieldName(key: string): FieldName | null {
  const last = key.split('.').pop() ?? '';
  const name = last.charAt(0).toLowerCase() + last.slice(1);
  return (fieldNames as readonly string[]).includes(name) ? (name as FieldName) : null;
}
