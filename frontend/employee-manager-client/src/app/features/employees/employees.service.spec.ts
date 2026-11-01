import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { EmployeeCreate } from './employee.model';
import { EmployeesService } from './employees.service';

const body: EmployeeCreate = {
  firstName: 'Ada',
  lastName: 'Lovelace',
  email: 'ada.lovelace@example.com',
  jobTitle: 'Engineer',
  hireDate: '2026-01-05',
  managerId: null,
  role: 'Employee',
  password: 'Secret123!',
};

describe('EmployeesService', () => {
  let service: EmployeesService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(EmployeesService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists employees', () => {
    let result: unknown;
    service.list().subscribe((employees) => (result = employees));

    const req = http.expectOne(`${environment.apiUrl}/employees`);
    expect(req.request.method).toBe('GET');
    req.flush([]);

    expect(result).toEqual([]);
  });

  it('creates an employee', () => {
    service.create(body).subscribe();

    const req = http.expectOne(`${environment.apiUrl}/employees`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ ...body, id: '1' });
  });

  it('updates an employee', () => {
    const { password: _, ...update } = body;
    service.update('1', update).subscribe();

    const req = http.expectOne(`${environment.apiUrl}/employees/1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(update);
    req.flush({ ...update, id: '1' });
  });

  it('deletes an employee', () => {
    service.delete('1').subscribe();

    const req = http.expectOne(`${environment.apiUrl}/employees/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});
