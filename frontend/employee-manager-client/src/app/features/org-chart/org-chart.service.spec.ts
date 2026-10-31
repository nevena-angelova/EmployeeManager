import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { OrgChartService } from './org-chart.service';

describe('OrgChartService', () => {
  let service: OrgChartService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(OrgChartService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('requests the whole org chart', () => {
    let result: unknown;
    service.getOrgChart().subscribe((nodes) => (result = nodes));

    const req = http.expectOne(`${environment.apiUrl}/employees/org-chart`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys()).toEqual([]);
    req.flush([]);

    expect(result).toEqual([]);
  });

  it('passes rootId as a query parameter', () => {
    service.getOrgChart('abc').subscribe();

    const req = http.expectOne(
      (r) =>
        r.url === `${environment.apiUrl}/employees/org-chart` && r.params.get('rootId') === 'abc',
    );
    req.flush([]);
  });
});
