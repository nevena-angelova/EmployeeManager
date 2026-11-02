import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { lineColour, teamLines } from '../../shared/team-lines';
import { OrgChartService } from '../org-chart/org-chart.service';
import { flattenOrgChart } from '../projects/org-chart-people';
import { AllocationSave, TeamAllocations, TeamMemberHours } from './allocation.model';
import { AllocationsService } from './allocations.service';
import { currentMonth, isMonth, monthLabel, nextMonth, previousMonth } from './month';

/** One report's hours, keyed by project id. */
type RowForm = FormGroup<Record<string, FormControl<number | null>>>;
/** The month's grid, keyed by employee id. */
type TeamForm = FormGroup<Record<string, RowForm>>;
type TeamValues = Record<string, Record<string, number | null>>;

/**
 * Monthly hours: everyone sees their own, and managers allocate their direct reports' hours on their
 * projects in a grid. The month comes from the `?month=` query param (default: the current UTC month).
 */
@Component({
  imports: [ReactiveFormsModule, RouterLink],
  selector: 'app-allocations',
  styleUrl: './allocations.scss',
  templateUrl: './allocations.html',
})
export class Allocations {
  private readonly allocationsService = inject(AllocationsService);
  private readonly orgChartService = inject(OrgChartService);
  private readonly router = inject(Router);
  private readonly user = inject(AuthService).currentUser;

  /** Bound from the `?month=` query param; anything that isn't `yyyy-MM` means the current month. */
  readonly month = input<string>();

  protected readonly selectedMonth = computed(() => {
    const month = this.month();
    return isMonth(month) ? month : currentMonth();
  });
  protected readonly label = computed(() => monthLabel(this.selectedMonth()));
  protected readonly isManager = computed(() => this.user()?.role === 'Manager');

  protected readonly mine = rxResource({
    params: () => this.selectedMonth(),
    stream: ({ params: month }) => this.allocationsService.getMine(month),
  });
  /** Only loaded for managers; undefined params leave the resource idle. */
  protected readonly team = rxResource({
    params: () => (this.isManager() ? this.selectedMonth() : undefined),
    stream: ({ params: month }) => this.allocationsService.getTeam(month),
  });
  /** Everyone, for the reports' team colours. */
  protected readonly orgChart = rxResource({
    params: () => (this.isManager() ? true : undefined),
    stream: () => this.orgChartService.getOrgChart(),
  });
  protected readonly lines = computed(() =>
    teamLines(flattenOrgChart(this.orgChart.value() ?? [])),
  );

  /** One form per loaded month; saving replaces the grid and so the form. */
  protected readonly form = computed(() => buildForm(this.team.value()));
  /** The form's current values, so totals update as you type. */
  private readonly values = signal<TeamValues>({});

  protected readonly totals = computed(() => {
    const values = this.values();
    return new Map(
      (this.team.value()?.members ?? []).map((member) => [
        member.employeeId,
        sum(Object.values(values[member.employeeId] ?? {})) + member.otherHours,
      ]),
    );
  });
  protected readonly dirty = computed(() => {
    const values = this.values();
    const initial = initialValues(this.team.value());
    return Object.entries(initial).some(([employeeId, row]) =>
      Object.entries(row).some(
        ([projectId, hours]) => (values[employeeId]?.[projectId] ?? 0) !== (hours ?? 0),
      ),
    );
  });

  protected readonly saving = signal(false);
  protected readonly saved = signal(false);
  /** 400 errors for one report, keyed by employee id. */
  protected readonly rowErrors = signal<ReadonlyMap<string, string>>(new Map());
  /** A 400 error for the whole grid, or a failed save. */
  protected readonly saveError = signal<string | null>(null);
  /** A 409: the month is closed or someone else changed it. */
  protected readonly conflict = signal<string | null>(null);

  constructor() {
    effect((onCleanup) => {
      const form = this.form();
      this.values.set(form.getRawValue());
      const subscription = form.valueChanges.subscribe(() => this.values.set(form.getRawValue()));
      onCleanup(() => subscription.unsubscribe());
    });

    // Messages belong to the month they were shown for.
    effect(() => {
      this.selectedMonth();
      this.clearMessages();
    });
  }

  protected goTo(month: string): void {
    this.router.navigate([], { queryParams: { month } });
  }

  protected previous(): void {
    this.goTo(previousMonth(this.selectedMonth()));
  }

  protected next(): void {
    this.goTo(nextMonth(this.selectedMonth()));
  }

  /** The report's team line colour, as on the org chart. */
  protected colour(id: string): string | null {
    return lineColour(this.lines(), id);
  }

  /** The input for a cell, or null where the report isn't on the project ("—"). */
  protected control(employeeId: string, projectId: string): FormControl<number | null> | null {
    return this.form().controls[employeeId]?.controls[projectId] ?? null;
  }

  protected isOver(team: TeamAllocations, member: TeamMemberHours): boolean {
    return (this.totals().get(member.employeeId) ?? 0) > team.capacityHours;
  }

  protected totalTitle(member: TeamMemberHours): string {
    return member.otherHours
      ? `Includes ${formatHours(member.otherHours)} h on other managers’ projects`
      : 'Hours on all projects this month';
  }

  /** What's wrong with a report's row: a bad cell first, else what the API said. */
  protected rowError(member: TeamMemberHours): string | null {
    const row = this.form().controls[member.employeeId];
    // Read `values` so the message follows edits.
    this.values();
    if (row && Object.values(row.controls).some((c) => c.invalid)) {
      return 'Enter hours in steps of 0.5, from 0 up.';
    }
    return this.rowErrors().get(member.employeeId.toLowerCase()) ?? null;
  }

  protected isMember(member: TeamMemberHours, projectId: string): boolean {
    return member.projectIds.includes(projectId);
  }

  protected format(hours: number): string {
    return formatHours(hours);
  }

  protected reset(): void {
    this.form().reset(initialValues(this.team.value()));
    this.clearMessages();
  }

  protected reload(): void {
    this.clearMessages();
    this.team.reload();
  }

  protected save(): void {
    const form = this.form();
    const month = this.team.value()?.month;
    if (!month || this.saving()) {
      return;
    }
    if (form.invalid) {
      form.markAllAsTouched();
      return;
    }

    const allocations: AllocationSave[] = Object.entries(form.getRawValue()).flatMap(
      ([employeeId, row]) =>
        Object.entries(row)
          .filter(([, hours]) => !!hours && hours > 0)
          .map(([projectId, hours]) => ({ employeeId, projectId, hours: hours! })),
    );

    this.saving.set(true);
    this.clearMessages();
    this.allocationsService.saveTeam(month, { allocations }).subscribe({
      next: (result) => {
        this.saving.set(false);
        this.team.set(result);
        this.saved.set(true);
        // A report's own hours may have changed if the manager is on their own projects.
        this.mine.reload();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.showErrors(error);
      },
    });
  }

  private clearMessages(): void {
    this.saved.set(false);
    this.rowErrors.set(new Map());
    this.saveError.set(null);
    this.conflict.set(null);
  }

  /** Puts 400 errors under their report's row, and turns a 409 into the Reload banner. */
  private showErrors(error: unknown): void {
    if (!(error instanceof HttpErrorResponse)) {
      this.saveError.set("Couldn't save the hours. Please try again.");
      return;
    }
    if (error.status === 409) {
      this.conflict.set(
        typeof error.error?.detail === 'string'
          ? error.error.detail
          : 'These hours were changed elsewhere.',
      );
      return;
    }
    const errors =
      error.status === 400
        ? (error.error?.errors as Record<string, string[]> | undefined)
        : undefined;
    if (!errors) {
      this.saveError.set("Couldn't save the hours. Please try again.");
      return;
    }

    const rows = new Map<string, string>();
    const other: string[] = [];
    for (const [key, messages] of Object.entries(errors)) {
      const [field, employeeId] = key.toLowerCase().split('.');
      if (field === 'allocations' && employeeId) {
        rows.set(employeeId, messages.join(' '));
      } else {
        other.push(...messages);
      }
    }
    this.rowErrors.set(rows);
    this.saveError.set(other.length ? other.join(' ') : null);
  }
}

/** A cell for every project the report is on, plus any they still have hours on, so those can be cleared. */
function buildForm(team: TeamAllocations | undefined): TeamForm {
  const values = initialValues(team);
  const disabled = !team?.isEditable;
  return new FormGroup(
    Object.fromEntries(
      Object.entries(values).map(([employeeId, row]) => [
        employeeId,
        new FormGroup(
          Object.fromEntries(
            Object.entries(row).map(([projectId, hours]) => [
              projectId,
              new FormControl<number | null>({ value: hours, disabled }, halfHours),
            ]),
          ),
        ),
      ]),
    ),
  );
}

function initialValues(team: TeamAllocations | undefined): TeamValues {
  const projectIds = new Set(team?.projects.map((p) => p.id));
  return Object.fromEntries(
    (team?.members ?? []).map((member) => {
      const hours = new Map(member.hours.map((h) => [h.projectId, h.hours]));
      const cells = [...projectIds].filter(
        (id) => member.projectIds.includes(id) || (hours.get(id) ?? 0) > 0,
      );
      return [member.employeeId, Object.fromEntries(cells.map((id) => [id, hours.get(id) ?? 0]))];
    }),
  );
}

/** Empty, or a multiple of 0.5 that is 0 or more. */
function halfHours(control: AbstractControl<number | null>): ValidationErrors | null {
  const value = control.value;
  if (value === null) {
    return null;
  }
  return Number.isFinite(value) && value >= 0 && Number.isInteger(value * 2)
    ? null
    : { halfHours: true };
}

function sum(hours: readonly (number | null)[]): number {
  return hours.reduce<number>((total, h) => total + (h && h > 0 ? h : 0), 0);
}

function formatHours(hours: number): string {
  return String(Math.round(hours * 10) / 10);
}
