import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, effect, inject, signal, viewChild } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { EmployeeForm } from './employee-form/employee-form';
import { Employee } from './employee.model';
import { EmployeesService } from './employees.service';

/** Admin page that lists employees, adds and deletes them, and links to each profile for editing. */
@Component({
  imports: [EmployeeForm, RouterLink],
  selector: 'app-employees',
  styleUrl: './employees.scss',
  templateUrl: './employees.html',
})
export class Employees {
  private readonly employeesService = inject(EmployeesService);

  protected readonly employees = rxResource({ stream: () => this.employeesService.list() });

  /** Whether the Add employee dialog is open. */
  protected readonly adding = signal(false);
  protected readonly deletingId = signal<string | null>(null);
  protected readonly deleteError = signal<string | null>(null);

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
    this.deleteError.set(null);
    this.adding.set(true);
  }

  protected closeAdd(event?: Event): void {
    // Escape fires `cancel`; close through the page state so the dialog and the page stay in step.
    event?.preventDefault();
    this.adding.set(false);
  }

  protected onAdded(): void {
    this.adding.set(false);
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
