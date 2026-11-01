import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { Employee } from '../employee.model';
import { EmployeesService } from '../employees.service';
import { EmployeeForm } from './employee-form';

const margaret: Employee = {
  id: '1',
  firstName: 'Margaret',
  lastName: 'Hayes',
  email: 'margaret.hayes@example.com',
  jobTitle: 'Chief Executive Officer',
  hireDate: '2020-01-01',
  managerId: null,
  managerName: null,
  role: 'Manager',
};

const ethan: Employee = {
  ...margaret,
  id: '2',
  firstName: 'Ethan',
  lastName: 'Clarke',
  email: 'ethan.clarke@example.com',
  jobTitle: 'Backend Developer',
  hireDate: '2023-04-10',
  managerId: '1',
  managerName: 'Margaret Hayes',
  role: 'Employee',
};

describe('EmployeeForm', () => {
  let fixture: ComponentFixture<EmployeeForm>;
  let element: HTMLElement;
  let service: { create: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn> };
  let saved: Employee[];
  let cancelled: number;

  async function render(
    employee: Employee | null,
    response: () => Observable<Employee> = () => of(ethan),
  ): Promise<void> {
    service = { create: vi.fn(response), update: vi.fn(response) };
    TestBed.configureTestingModule({
      imports: [EmployeeForm],
      providers: [{ provide: EmployeesService, useValue: service }],
    });
    fixture = TestBed.createComponent(EmployeeForm);
    fixture.componentRef.setInput('employee', employee);
    fixture.componentRef.setInput('employees', [ethan, margaret]);
    saved = [];
    cancelled = 0;
    fixture.componentInstance.saved.subscribe((e) => saved.push(e));
    fixture.componentInstance.cancelled.subscribe(() => cancelled++);
    await fixture.whenStable();
    element = fixture.nativeElement as HTMLElement;
  }

  function field<T extends HTMLInputElement | HTMLSelectElement>(id: string): T {
    return element.querySelector<T>(`#${id}`)!;
  }

  async function fill(values: Record<string, string>): Promise<void> {
    for (const [id, value] of Object.entries(values)) {
      const control = field(id);
      control.value = value;
      control.dispatchEvent(new Event(control instanceof HTMLSelectElement ? 'change' : 'input'));
    }
    await fixture.whenStable();
  }

  async function submit(): Promise<void> {
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  }

  const newEmployee = {
    firstName: 'Ada',
    lastName: 'Lovelace',
    email: 'ada.lovelace@example.com',
    jobTitle: 'Engineer',
    hireDate: '2026-01-05',
  };

  it('creates an employee with a password, manager and role', async () => {
    await render(null);

    expect(field<HTMLSelectElement>('role').value).toBe('Employee');
    await fill({ ...newEmployee, managerId: '1', role: 'Manager', password: 'Secret123!' });
    await submit();

    expect(service.create).toHaveBeenCalledWith({
      ...newEmployee,
      managerId: '1',
      role: 'Manager',
      password: 'Secret123!',
    });
    expect(saved).toEqual([ethan]);
  });

  it('sends a null manager for a top-level employee', async () => {
    await render(null);

    await fill({ ...newEmployee, password: 'Secret123!' });
    await submit();

    expect(service.create).toHaveBeenCalledWith(expect.objectContaining({ managerId: null }));
  });

  it('edits an employee without a password field', async () => {
    await render(ethan);

    expect(element.querySelector('h2')?.textContent).toContain('Edit employee');
    expect(field('password')).toBeNull();
    expect(field('email').value).toBe('ethan.clarke@example.com');
    expect(field<HTMLSelectElement>('managerId').value).toBe('1');

    await fill({ jobTitle: 'Senior Backend Developer' });
    await submit();

    expect(service.update).toHaveBeenCalledWith('2', {
      firstName: 'Ethan',
      lastName: 'Clarke',
      email: 'ethan.clarke@example.com',
      jobTitle: 'Senior Backend Developer',
      hireDate: '2023-04-10',
      managerId: '1',
      role: 'Employee',
    });
  });

  it('offers every other employee as manager', async () => {
    await render(ethan);

    const options = [...field<HTMLSelectElement>('managerId').options].map((o) => o.text.trim());
    expect(options).toEqual(['No manager (top level)', 'Margaret Hayes']);
  });

  it('does not submit an invalid form', async () => {
    await render(null);

    await fill({ email: 'not-an-email' });
    await submit();

    expect(service.create).not.toHaveBeenCalled();
    expect(element.textContent).toContain('Enter a first name.');
    expect(element.textContent).toContain('Enter a valid email address.');
    expect(element.textContent).toContain('Enter a password.');
  });

  it('shows API validation errors next to their fields', async () => {
    await render(null, () =>
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: {
              errors: {
                email: ['Email is already in use.'],
                password: ['Passwords must have at least one digit.'],
                '': ['Something else went wrong.'],
              },
            },
          }),
      ),
    );

    await fill({ ...newEmployee, password: 'weak' });
    await submit();

    const fieldErrors = [...element.querySelectorAll('.field-error')].map((e) =>
      e.textContent?.trim(),
    );
    expect(fieldErrors).toEqual([
      'Email is already in use.',
      'Passwords must have at least one digit.',
    ]);
    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'Something else went wrong.',
    );
    expect(saved).toEqual([]);
  });

  it('shows a form-level error for other failures', async () => {
    await render(ethan, () => throwError(() => new HttpErrorResponse({ status: 500 })));

    await submit();

    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      "Couldn't save the employee.",
    );
  });

  it('emits cancelled', async () => {
    await render(null);

    [...element.querySelectorAll('button')]
      .find((b) => b.textContent?.trim() === 'Cancel')!
      .click();

    expect(cancelled).toBe(1);
    expect(service.create).not.toHaveBeenCalled();
  });
});
