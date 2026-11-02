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
import { Router } from '@angular/router';

import { projectColour } from '../../allocations/hour-rail';
import { addDays, dayLabel, dayOfMonth, isDay, isWeekend, today, weekdayShort } from './day';
import { DayTimeEntries, TimeEntrySave } from './time-entry.model';
import { TimeEntriesService } from './time-entries.service';

/** The day's hours, keyed by project id. */
type DayForm = FormGroup<Record<string, FormControl<number | null>>>;
type DayValues = Record<string, number | null>;

/** A day in the week strip. */
interface WeekDay {
  date: string;
  weekday: string;
  dayOfMonth: number;
  hours: number;
  isSelected: boolean;
  isToday: boolean;
  isFuture: boolean;
  /** A weekday up to today with nothing logged. */
  isMissing: boolean;
}

/**
 * The signed-in employee logs the hours they spent on each of their projects on a day. The day comes
 * from the page's `?date=` query param (default: today, UTC); the week strip shows which days still
 * need hours.
 */
@Component({
  imports: [ReactiveFormsModule],
  selector: 'app-daily-hours',
  styleUrl: './daily-hours.scss',
  templateUrl: './daily-hours.html',
})
export class DailyHours {
  private readonly timeEntriesService = inject(TimeEntriesService);
  private readonly router = inject(Router);

  /** `yyyy-MM-dd`; anything else means today. */
  readonly date = input<string>();

  protected readonly today = signal(today());
  protected readonly selectedDate = computed(() => {
    const date = this.date();
    return isDay(date) ? date : this.today();
  });
  protected readonly label = computed(() => dayLabel(this.selectedDate()));
  protected readonly isToday = computed(() => this.selectedDate() === this.today());

  protected readonly day = rxResource({
    params: () => this.selectedDate(),
    stream: ({ params: date }) => this.timeEntriesService.getMine(date),
  });

  /** One form per loaded day; saving replaces the day and so the form. */
  protected readonly form = computed(() => buildForm(this.day.value()));
  /** The form's current values, so the total and the week strip update as you type. */
  private readonly values = signal<DayValues>({});

  protected readonly total = computed(() => sum(Object.values(this.values())));
  protected readonly isOver = computed(
    () => this.total() > (this.day.value()?.maxHours ?? Number.POSITIVE_INFINITY),
  );
  protected readonly dirty = computed(() => {
    const values = this.values();
    return Object.entries(initialValues(this.day.value())).some(
      ([projectId, hours]) => (values[projectId] ?? 0) !== (hours ?? 0),
    );
  });

  protected readonly week = computed<WeekDay[]>(() => {
    const day = this.day.value();
    const selected = this.selectedDate();
    const todayValue = this.today();
    return (day?.week ?? []).map(({ date, hours }) => {
      // The selected day follows the form.
      const shown = date === selected ? this.total() : hours;
      const isFuture = date > todayValue;
      return {
        date,
        weekday: weekdayShort(date),
        dayOfMonth: dayOfMonth(date),
        hours: shown,
        isSelected: date === selected,
        isToday: date === todayValue,
        isFuture,
        isMissing: !isFuture && !isWeekend(date) && shown === 0,
      };
    });
  });

  protected readonly saving = signal(false);
  protected readonly saved = signal(false);
  /** A 400 from the API, or a failed save. */
  protected readonly saveError = signal<string | null>(null);
  /** A 409: the day is in the future or someone else changed it. */
  protected readonly conflict = signal<string | null>(null);

  constructor() {
    effect((onCleanup) => {
      const form = this.form();
      this.values.set(form.getRawValue());
      const subscription = form.valueChanges.subscribe(() => this.values.set(form.getRawValue()));
      onCleanup(() => subscription.unsubscribe());
    });

    // Messages belong to the day they were shown for.
    effect(() => {
      this.selectedDate();
      this.clearMessages();
    });
  }

  protected goTo(date: string): void {
    this.router.navigate([], { queryParams: { date }, queryParamsHandling: 'merge' });
  }

  protected previous(): void {
    this.goTo(addDays(this.selectedDate(), -1));
  }

  protected next(): void {
    this.goTo(addDays(this.selectedDate(), 1));
  }

  protected previousWeek(): void {
    this.goTo(addDays(this.selectedDate(), -7));
  }

  protected nextWeek(): void {
    this.goTo(addDays(this.selectedDate(), 7));
  }

  protected goToToday(): void {
    this.goTo(this.today());
  }

  protected control(projectId: string): FormControl<number | null> | null {
    return this.form().controls[projectId] ?? null;
  }

  protected projectColour(index: number): { edge: string; tint: string } {
    return projectColour(index);
  }

  /** What's wrong with the form: a bad input first, then the day's limit. */
  protected formError(): string | null {
    // Read `values` so the message follows edits.
    this.values();
    if (Object.values(this.form().controls).some((c) => c.invalid)) {
      return 'Enter hours in steps of 0.5, from 0 up.';
    }
    const max = this.day.value()?.maxHours;
    return this.isOver() && max ? `A day has at most ${max} h.` : null;
  }

  protected weekDayLabel(day: WeekDay): string {
    const hours = day.isFuture ? 'not yet' : `${this.format(day.hours)} hours`;
    return `${dayLabel(day.date)}, ${hours}${day.isMissing ? ', nothing logged' : ''}`;
  }

  protected format(hours: number): string {
    return String(Math.round(hours * 10) / 10);
  }

  protected reset(): void {
    this.form().reset(initialValues(this.day.value()));
    this.clearMessages();
  }

  protected reload(): void {
    this.clearMessages();
    this.day.reload();
  }

  protected save(): void {
    const form = this.form();
    const date = this.day.value()?.date;
    if (!date || this.saving()) {
      return;
    }
    if (form.invalid || this.isOver()) {
      form.markAllAsTouched();
      return;
    }

    const entries: TimeEntrySave[] = Object.entries(form.getRawValue())
      .filter(([, hours]) => !!hours && hours > 0)
      .map(([projectId, hours]) => ({ projectId, hours: hours! }));

    this.saving.set(true);
    this.clearMessages();
    this.timeEntriesService.saveMine(date, { entries }).subscribe({
      next: (result) => {
        this.saving.set(false);
        this.day.set(result);
        this.saved.set(true);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.showError(error);
      },
    });
  }

  private clearMessages(): void {
    this.saved.set(false);
    this.saveError.set(null);
    this.conflict.set(null);
  }

  private showError(error: unknown): void {
    if (error instanceof HttpErrorResponse && error.status === 409) {
      this.conflict.set(
        typeof error.error?.detail === 'string'
          ? error.error.detail
          : 'These hours were changed elsewhere.',
      );
      return;
    }
    const errors =
      error instanceof HttpErrorResponse && error.status === 400
        ? (error.error?.errors as Record<string, string[]> | undefined)
        : undefined;
    this.saveError.set(
      errors
        ? Object.values(errors).flat().join(' ')
        : "Couldn't save your hours. Please try again.",
    );
  }
}

function buildForm(day: DayTimeEntries | undefined): DayForm {
  const disabled = !day?.isEditable;
  return new FormGroup(
    Object.fromEntries(
      Object.entries(initialValues(day)).map(([projectId, hours]) => [
        projectId,
        new FormControl<number | null>({ value: hours, disabled }, halfHours),
      ]),
    ),
  );
}

function initialValues(day: DayTimeEntries | undefined): DayValues {
  return Object.fromEntries((day?.projects ?? []).map((p) => [p.projectId, p.hours]));
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
