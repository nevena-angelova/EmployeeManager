import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../../environments/environment';
import { TimeEntriesService } from './time-entries.service';

describe('TimeEntriesService', () => {
  let service: TimeEntriesService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TimeEntriesService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('gets a day', () => {
    service.getMine('2026-11-02').subscribe();

    const request = http.expectOne((r) => r.url === `${environment.apiUrl}/time-entries/mine`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('date')).toBe('2026-11-02');
  });

  it('saves a day', () => {
    const body = { entries: [{ projectId: 'p1', hours: 6.5 }] };
    service.saveMine('2026-11-02', body).subscribe();

    const request = http.expectOne((r) => r.method === 'PUT');
    expect(request.request.urlWithParams).toBe(
      `${environment.apiUrl}/time-entries/mine?date=2026-11-02`,
    );
    expect(request.request.body).toEqual(body);
  });
});
