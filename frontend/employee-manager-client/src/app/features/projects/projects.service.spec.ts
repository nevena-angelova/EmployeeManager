import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { ProjectSave } from './project.model';
import { ProjectsService } from './projects.service';

const body: ProjectSave = { name: 'Payments API', description: null, memberIds: ['e1'] };

describe('ProjectsService', () => {
  let service: ProjectsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ProjectsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists managed and own projects', () => {
    service.listManaged().subscribe();
    service.listMine().subscribe();

    expect(http.expectOne(`${environment.apiUrl}/projects/managed`).request.method).toBe('GET');
    expect(http.expectOne(`${environment.apiUrl}/projects/mine`).request.method).toBe('GET');
  });

  it('gets a project', () => {
    service.get('p 1').subscribe();

    expect(http.expectOne(`${environment.apiUrl}/projects/p%201`).request.method).toBe('GET');
  });

  it('creates, updates and deletes a project', () => {
    service.create(body).subscribe();
    service.update('p1', body).subscribe();
    service.delete('p1').subscribe();

    const create = http.expectOne((r) => r.method === 'POST');
    expect(create.request.url).toBe(`${environment.apiUrl}/projects`);
    expect(create.request.body).toEqual(body);
    const update = http.expectOne((r) => r.method === 'PUT');
    expect(update.request.url).toBe(`${environment.apiUrl}/projects/p1`);
    expect(update.request.body).toEqual(body);
    expect(http.expectOne((r) => r.method === 'DELETE').request.url).toBe(
      `${environment.apiUrl}/projects/p1`,
    );
  });
});
