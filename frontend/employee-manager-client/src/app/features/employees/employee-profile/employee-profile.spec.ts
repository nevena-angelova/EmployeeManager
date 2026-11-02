import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { Employee } from '../employee.model';
import { EmployeesService } from '../employees.service';
import { EmployeeProfile } from './employee-profile';

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

describe('EmployeeProfile', () => {
  let fixture: ComponentFixture<EmployeeProfile>;
  let element: HTMLElement;
  let service: {
    get: ReturnType<typeof vi.fn>;
    list: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
  };

  async function render(
    id: string,
    get: (id: string) => Observable<Employee> = (id) =>
      of([margaret, ethan].find((e) => e.id === id)!),
  ): Promise<void> {
    service = {
      get: vi.fn(get),
      list: vi.fn(() => of([ethan, margaret])),
      update: vi.fn((_id: string, body: Partial<Employee>) =>
        of({ ...ethan, ...body } as Employee),
      ),
    };
    TestBed.configureTestingModule({
      imports: [EmployeeProfile],
      providers: [provideRouter([]), { provide: EmployeesService, useValue: service }],
    });
    fixture = TestBed.createComponent(EmployeeProfile);
    fixture.componentRef.setInput('id', id);
    await fixture.whenStable();
    element = fixture.nativeElement as HTMLElement;
  }

  function button(label: string): HTMLButtonElement | undefined {
    return [...element.querySelectorAll<HTMLButtonElement>('button')].find(
      (b) => b.textContent?.trim() === label,
    );
  }

  function details(): Record<string, string> {
    return Object.fromEntries(
      [...element.querySelectorAll('.details > div')].map((row) => [
        row.querySelector('dt')!.textContent!.trim(),
        row.querySelector('dd')!.textContent!.trim(),
      ]),
    );
  }

  it("shows the employee's details with a link to their manager", async () => {
    await render('2');

    expect(service.get).toHaveBeenCalledWith('2');
    expect(element.querySelector('h2')?.textContent).toBe('Ethan Clarke');
    expect(details()).toEqual({
      Email: 'ethan.clarke@example.com',
      'Job title': 'Backend Developer',
      Role: 'Employee',
      Manager: 'Margaret Hayes',
      'Hire date': 'April 10, 2023',
    });
    expect(element.querySelector('.details a[href="/employees/1"]')?.textContent).toBe(
      'Margaret Hayes',
    );
    expect(element.textContent).toContain('No direct reports.');
  });

  it('lists direct reports with links to their profiles', async () => {
    await render('1');

    expect(details()['Manager']).toBe('None (top level)');
    const report = element.querySelector('.reports a');
    expect(report?.textContent?.trim()).toBe('Ethan Clarke');
    expect(report?.getAttribute('href')).toBe('/employees/2');
  });

  it('edits the employee inline and shows the saved details', async () => {
    await render('2');
    expect(element.querySelector('app-employee-form')).toBeNull();

    button('Edit')!.click();
    await fixture.whenStable();

    expect(element.querySelector('dialog')).toBeNull();
    expect(button('Edit')).toBeUndefined();
    const jobTitle = element.querySelector<HTMLInputElement>('app-employee-form #jobTitle')!;
    expect(jobTitle.value).toBe('Backend Developer');

    jobTitle.value = 'Senior Backend Developer';
    jobTitle.dispatchEvent(new Event('input'));
    element.querySelector('app-employee-form form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    expect(service.update).toHaveBeenCalledWith(
      '2',
      expect.objectContaining({ jobTitle: 'Senior Backend Developer' }),
    );
    expect(element.querySelector('app-employee-form')).toBeNull();
    expect(details()['Job title']).toBe('Senior Backend Developer');
    expect(service.list).toHaveBeenCalledTimes(2);
  });

  it('cancels editing without saving', async () => {
    await render('2');

    button('Edit')!.click();
    await fixture.whenStable();
    button('Cancel')!.click();
    await fixture.whenStable();

    expect(element.querySelector('app-employee-form')).toBeNull();
    expect(service.update).not.toHaveBeenCalled();
    expect(button('Edit')).toBeDefined();
  });

  it('says so when the employee does not exist', async () => {
    await render('missing', () => throwError(() => new HttpErrorResponse({ status: 404 })));

    expect(element.querySelector('[role="alert"]')?.textContent).toContain("doesn't exist");
    expect(button('Edit')).toBeUndefined();
  });

  it('shows an error with a retry button when loading fails', async () => {
    await render('2', () => throwError(() => new HttpErrorResponse({ status: 500 })));

    expect(element.querySelector('[role="alert"]')?.textContent).toContain("couldn't be loaded");

    service.get.mockReturnValue(of(ethan));
    button('Retry')!.click();
    await fixture.whenStable();

    expect(element.querySelector('h2')?.textContent).toBe('Ethan Clarke');
  });
});
