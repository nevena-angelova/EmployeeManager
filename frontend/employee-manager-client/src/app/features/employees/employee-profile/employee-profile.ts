import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, linkedSignal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { EmployeeForm } from '../employee-form/employee-form';
import { Employee } from '../employee.model';
import { EmployeesService } from '../employees.service';

/** Admin page showing one employee's details, with an inline form to edit them. */
@Component({
  imports: [DatePipe, EmployeeForm, RouterLink],
  selector: 'app-employee-profile',
  styleUrl: './employee-profile.scss',
  templateUrl: './employee-profile.html',
})
export class EmployeeProfile {
  private readonly employeesService = inject(EmployeesService);

  /** Bound from the `:id` route param. */
  readonly id = input.required<string>();

  protected readonly employee = rxResource({
    params: () => this.id(),
    stream: ({ params: id }) => this.employeesService.get(id),
  });
  /** Everyone, for the manager picker and the direct reports. */
  protected readonly employees = rxResource({ stream: () => this.employeesService.list() });

  /** Whether the edit form is showing; resets when moving to another profile. */
  protected readonly editing = linkedSignal({ source: this.id, computation: () => false });

  protected readonly notFound = computed(() => {
    const error = this.employee.error();
    return error instanceof HttpErrorResponse && error.status === 404;
  });

  protected readonly directReports = computed(() =>
    (this.employees.value() ?? []).filter((e) => e.managerId === this.id()),
  );

  protected onSaved(employee: Employee): void {
    this.employee.set(employee);
    this.editing.set(false);
    // Names and managers shown on this page come from the list too.
    this.employees.reload();
  }
}
