import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { OrgChart } from './org-chart';
import { OrgChartNode } from './org-chart.model';
import { OrgChartService } from './org-chart.service';

const margaret: OrgChartNode = {
  id: '1',
  firstName: 'Margaret',
  lastName: 'Hayes',
  jobTitle: 'Chief Executive Officer',
  email: 'margaret.hayes@example.com',
  directReports: [],
};

describe('OrgChart', () => {
  async function render(response: () => Observable<OrgChartNode[]>): Promise<HTMLElement> {
    TestBed.configureTestingModule({
      imports: [OrgChart],
      providers: [{ provide: OrgChartService, useValue: { getOrgChart: response } }],
    });
    const fixture = TestBed.createComponent(OrgChart);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders a branch for each top-level employee', async () => {
    const element = await render(() =>
      of([margaret, { ...margaret, id: '2', firstName: 'Zoe', lastName: 'Adams' }]),
    );

    const names = [...element.querySelectorAll('.name')].map((el) => el.textContent?.trim());
    expect(names).toEqual(['Margaret Hayes', 'Zoe Adams']);
  });

  it('shows an empty state when there are no employees', async () => {
    const element = await render(() => of([]));

    expect(element.textContent).toContain('No employees yet.');
  });

  it('shows an error with a retry button when loading fails', async () => {
    const element = await render(() => throwError(() => new Error('offline')));

    expect(element.querySelector('[role="alert"]')?.textContent).toContain("couldn't be loaded");
    expect(element.querySelector('[role="alert"] button')?.textContent?.trim()).toBe('Retry');
  });
});
