import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { Employee } from './employee.model';
import { Employees } from './employees';
import { EmployeesService } from './employees.service';

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
  managerId: '1',
  managerName: 'Margaret Hayes',
  role: 'Employee',
};

describe('Employees', () => {
  let fixture: ComponentFixture<Employees>;
  let service: {
    list: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
  };

  async function render(
    list: () => Observable<Employee[]>,
    remove: () => Observable<void> = () => of(undefined),
  ): Promise<HTMLElement> {
    service = { list: vi.fn(list), delete: vi.fn(remove) };
    TestBed.configureTestingModule({
      imports: [Employees],
      providers: [{ provide: EmployeesService, useValue: service }],
    });
    fixture = TestBed.createComponent(Employees);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function button(element: HTMLElement, label: string): HTMLButtonElement {
    return [...element.querySelectorAll<HTMLButtonElement>('button')].find(
      (b) => b.getAttribute('aria-label') === label || b.textContent?.trim() === label,
    )!;
  }

  afterEach(() => vi.restoreAllMocks());

  it('lists employees with their manager and role', async () => {
    const element = await render(() => of([ethan, margaret]));

    const rows = [...element.querySelectorAll('tbody tr')].map((row) =>
      [...row.children].slice(0, 5).map((cell) => cell.textContent?.trim()),
    );
    expect(rows).toEqual([
      [
        'Ethan Clarke',
        'Backend Developer',
        'ethan.clarke@example.com',
        'Margaret Hayes',
        'Employee',
      ],
      ['Margaret Hayes', 'Chief Executive Officer', 'margaret.hayes@example.com', '—', 'Manager'],
    ]);
  });

  it('shows an empty state when there are no employees', async () => {
    const element = await render(() => of([]));

    expect(element.textContent).toContain('No employees yet.');
  });

  it('shows an error with a retry button when loading fails', async () => {
    const element = await render(() => throwError(() => new Error('offline')));

    expect(element.querySelector('[role="alert"]')?.textContent).toContain("couldn't be loaded");

    service.list.mockReturnValue(of([margaret]));
    button(element, 'Retry').click();
    await fixture.whenStable();

    expect(element.querySelector('tbody .name')?.textContent?.trim()).toBe('Margaret Hayes');
  });

  it('opens the form to add an employee', async () => {
    const element = await render(() => of([margaret]));

    button(element, 'Add employee').click();
    await fixture.whenStable();

    expect(element.querySelector('app-employee-form h2')?.textContent).toContain('Add employee');
  });

  it('opens the form to edit an employee', async () => {
    const element = await render(() => of([margaret]));

    button(element, 'Edit Margaret Hayes').click();
    await fixture.whenStable();

    expect(element.querySelector('app-employee-form h2')?.textContent).toContain('Edit employee');
    expect(element.querySelector<HTMLInputElement>('app-employee-form #email')?.value).toBe(
      'margaret.hayes@example.com',
    );
  });

  it('deletes after confirming and reloads the list', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const element = await render(() => of([ethan, margaret]));

    button(element, 'Delete Ethan Clarke').click();
    await fixture.whenStable();

    expect(service.delete).toHaveBeenCalledWith('2');
    expect(service.list).toHaveBeenCalledTimes(2);
  });

  it('does nothing when the delete is not confirmed', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    const element = await render(() => of([margaret]));

    button(element, 'Delete Margaret Hayes').click();
    await fixture.whenStable();

    expect(service.delete).not.toHaveBeenCalled();
  });

  it('shows why a delete was refused', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const element = await render(
      () => of([margaret]),
      () =>
        throwError(
          () =>
            new HttpErrorResponse({
              status: 409,
              error: { detail: 'Margaret Hayes has direct reports. Reassign them first.' },
            }),
        ),
    );

    button(element, 'Delete Margaret Hayes').click();
    await fixture.whenStable();

    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'Margaret Hayes has direct reports.',
    );
  });
});
