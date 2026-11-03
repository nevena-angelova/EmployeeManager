import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { TeamAllocationsSave } from './allocation.model';
import { AllocationsService } from './allocations.service';

const body: TeamAllocationsSave = {
  allocations: [{ employeeId: 'e1', projectId: 'p1', hours: 40.5 }],
};

describe('AllocationsService', () => {
  let service: AllocationsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AllocationsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('gets the team grid and own hours for a month', () => {
    service.getTeam('2026-11').subscribe();
    service.getMine('2026-11').subscribe();

    const team = http.expectOne((r) => r.url === `${environment.apiUrl}/allocations/team`);
    expect(team.request.method).toBe('GET');
    expect(team.request.params.get('month')).toBe('2026-11');
    const mine = http.expectOne((r) => r.url === `${environment.apiUrl}/allocations/mine`);
    expect(mine.request.method).toBe('GET');
    expect(mine.request.params.get('month')).toBe('2026-11');
  });

  it('saves the team grid for a month', () => {
    service.saveTeam('2026-11', body).subscribe();

    const save = http.expectOne((r) => r.method === 'PUT');
    expect(save.request.urlWithParams).toBe(`${environment.apiUrl}/allocations/team?month=2026-11`);
    expect(save.request.body).toEqual(body);
  });

  it('gets the team statistics for a year', () => {
    service.getTeamStatistics('2026').subscribe();

    const statistics = http.expectOne(
      (r) => r.url === `${environment.apiUrl}/allocations/team/statistics`,
    );
    expect(statistics.request.method).toBe('GET');
    expect(statistics.request.params.get('year')).toBe('2026');
  });
});
