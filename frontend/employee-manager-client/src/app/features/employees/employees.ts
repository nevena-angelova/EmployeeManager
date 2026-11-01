import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';

import { EmployeeForm } from './employee-form/employee-form';
import { Employee } from './employee.model';
import { EmployeesService } from './employees.service';

/** Admin page to add, edit and delete employees. */
@Component({
  imports: [EmployeeForm],
  selector: 'app-employees',
  styleUrl: './employees.scss',
  templateUrl: './employees.html',
})
export class Employees {
  private readonly employeesService = inject(EmployeesService);

  protected readonly employees = rxResource({ stream: () => this.employeesService.list() });

  /** The open form: `{ employee: null }` adds one, `{ employee }` edits it. */
  protected readonly form = signal<{ employee: Employee | null } | null>(null);
  protected readonly deletingId = signal<string | null>(null);
  protected readonly deleteError = signal<string | null>(null);

  protected add(): void {
    this.deleteError.set(null);
    this.form.set({ employee: null });
  }

  protected edit(employee: Employee): void {
    this.deleteError.set(null);
    this.form.set({ employee });
  }

  protected onSaved(): void {
    this.form.set(null);
    this.employees.reload();
  }

  protected delete(employee: Employee): void {
    if (
      this.deletingId() ||
      !confirm(
        `Delete ${employee.firstName} ${employee.lastName}? This also removes their account.`,
      )
    ) {
      return;
    }

    this.deletingId.set(employee.id);
    this.deleteError.set(null);
    this.employeesService.delete(employee.id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.employees.reload();
      },
      error: (error: unknown) => {
        this.deletingId.set(null);
        this.deleteError.set(deleteErrorMessage(employee, error));
        // A 404 means it's already gone, so refresh either way.
        this.employees.reload();
      },
    });
  }
}

function deleteErrorMessage(employee: Employee, error: unknown): string {
  const name = `${employee.firstName} ${employee.lastName}`;
  if (error instanceof HttpErrorResponse) {
    if (error.status === 409 && typeof error.error?.detail === 'string') {
      return error.error.detail;
    }
    if (error.status === 404) {
      return `${name} no longer exists.`;
    }
  }
  return `Couldn't delete ${name}. Please try again.`;
}
