import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../core/services/api.service';
import { Employee, EmployeeCreate, EmployeeUpdate } from './employee.model';

/** Admin-only employee management. */
@Injectable({ providedIn: 'root' })
export class EmployeesService {
  private readonly api = inject(ApiService);

  /** All employees, sorted by first then last name. */
  list(): Observable<Employee[]> {
    return this.api.get<Employee[]>('employees');
  }

  /** Creates the employee and their sign-in account. */
  create(employee: EmployeeCreate): Observable<Employee> {
    return this.api.post<Employee>('employees', employee);
  }

  update(id: string, employee: EmployeeUpdate): Observable<Employee> {
    return this.api.put<Employee>(`employees/${encodeURIComponent(id)}`, employee);
  }

  delete(id: string): Observable<void> {
    return this.api.delete<void>(`employees/${encodeURIComponent(id)}`);
  }
}
