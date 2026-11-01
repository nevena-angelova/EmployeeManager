import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  ElementRef,
  OnInit,
  afterNextRender,
  computed,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';

import { Role } from '../../../core/auth/auth.model';
import { Employee, EmployeeUpdate } from '../employee.model';
import { EmployeesService } from '../employees.service';

type FieldName =
  'firstName' | 'lastName' | 'email' | 'jobTitle' | 'hireDate' | 'managerId' | 'role' | 'password';

const fieldNames: readonly FieldName[] = [
  'firstName',
  'lastName',
  'email',
  'jobTitle',
  'hireDate',
  'managerId',
  'role',
  'password',
];

const requiredMessages: Partial<Record<FieldName, string>> = {
  firstName: 'Enter a first name.',
  lastName: 'Enter a last name.',
  email: 'Enter a valid email address.',
  jobTitle: 'Enter a job title.',
  hireDate: 'Enter a hire date.',
  password: 'Enter a password.',
};

/** Modal form that creates an employee (with their sign-in account) or edits one. */
@Component({
  imports: [ReactiveFormsModule],
  selector: 'app-employee-form',
  styleUrl: './employee-form.scss',
  templateUrl: './employee-form.html',
})
export class EmployeeForm implements OnInit {
  private readonly employeesService = inject(EmployeesService);
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  /** The employee being edited, or null to create one. */
  readonly employee = input<Employee | null>(null);
  /** Everyone who can be picked as manager (the employee being edited is left out). */
  readonly employees = input<Employee[]>([]);

  readonly saved = output<Employee>();
  readonly cancelled = output<void>();

  protected readonly roles: readonly Role[] = ['Employee', 'Manager', 'Admin'];
  protected readonly isEdit = computed(() => this.employee() !== null);
  protected readonly managerOptions = computed(() =>
    this.employees().filter((e) => e.id !== this.employee()?.id),
  );

  protected readonly form = inject(NonNullableFormBuilder).group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    jobTitle: ['', Validators.required],
    hireDate: [today(), Validators.required],
    /** '' means no manager (top level). */
    managerId: [''],
    role: ['Employee' as Role, Validators.required],
    password: ['', Validators.required],
  });
  protected readonly submitting = signal(false);
  protected readonly serverErrors = signal<Partial<Record<FieldName, string>>>({});
  protected readonly error = signal<string | null>(null);

  constructor() {
    afterNextRender(() => {
      const dialog = this.dialog().nativeElement;
      // jsdom may not implement showModal.
      if (typeof dialog.showModal === 'function') {
        dialog.showModal();
      } else {
        dialog.setAttribute('open', '');
      }
    });
  }

  ngOnInit(): void {
    const employee = this.employee();
    if (employee) {
      this.form.patchValue({ ...employee, managerId: employee.managerId ?? '' });
      // Passwords are only set when the account is created.
      this.form.controls.password.disable();
    }
  }

  /** The message to show under a field: a failed check, else what the API said. */
  protected fieldError(name: FieldName): string | null {
    const control = this.form.controls[name];
    if (control.touched && control.invalid) {
      return requiredMessages[name] ?? 'This field is required.';
    }
    return this.serverErrors()[name] ?? null;
  }

  protected submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const { password, managerId, ...rest } = this.form.getRawValue();
    const body: EmployeeUpdate = { ...rest, managerId: managerId || null };
    const employee = this.employee();
    const request: Observable<Employee> = employee
      ? this.employeesService.update(employee.id, body)
      : this.employeesService.create({ ...body, password });

    this.submitting.set(true);
    this.serverErrors.set({});
    this.error.set(null);

    request.subscribe({
      next: (result) => {
        this.close();
        this.saved.emit(result);
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.showErrors(error);
      },
    });
  }

  protected cancel(event?: Event): void {
    // Escape fires `cancel`; close through the page so the dialog and the page state stay in step.
    event?.preventDefault();
    this.close();
    this.cancelled.emit();
  }

  private close(): void {
    this.dialog().nativeElement.close?.();
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
          ? 'This employee no longer exists.'
          : "Couldn't save the employee. Please try again.",
      );
      return;
    }

    const fields: Partial<Record<FieldName, string>> = {};
    const other: string[] = [];
    for (const [key, messages] of Object.entries(errors)) {
      const name = toFieldName(key);
      if (name && this.form.controls[name].enabled) {
        fields[name] = messages.join(' ');
      } else {
        other.push(...messages);
      }
    }
    this.serverErrors.set(fields);
    this.error.set(other.length ? other.join(' ') : null);
  }
}

/** Maps an API error key (`email`, `Email`, `$.email`, `request.Email`) to a form field. */
function toFieldName(key: string): FieldName | null {
  const last = key.split('.').pop() ?? '';
  const name = last.charAt(0).toLowerCase() + last.slice(1);
  return (fieldNames as readonly string[]).includes(name) ? (name as FieldName) : null;
}

/** Today's local date as `YYYY-MM-DD`. */
function today(): string {
  const now = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}
