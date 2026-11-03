import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { OrgChartNode } from '../../org-chart/org-chart.model';
import { OrgChartService } from '../../org-chart/org-chart.service';
import { MonthHours, TeamStatistics } from '../allocation.model';
import { AllocationsService } from '../allocations.service';
import { Statistics } from './statistics';

/** Twelve months of 2026, with `hours` filled in by month number. */
function months(hours: Record<number, [number, number]> = {}): MonthHours[] {
  return Array.from({ length: 12 }, (_, i) => ({
    month: `2026-${String(i + 1).padStart(2, '0')}`,
    planned: hours[i + 1]?.[0] ?? 0,
    logged: hours[i + 1]?.[1] ?? 0,
  }));
}

const statistics: TeamStatistics = {
  year: 2026,
  months: months({ 10: [120, 100], 11: [80, 22.5] }).map((m) => ({
    ...m,
    capacityHours: 160,
    otherPlanned: m.month === '2026-11' ? 16 : 0,
    otherLogged: m.month === '2026-11' ? 4 : 0,
  })),
  projects: [
    {
      id: 'p1',
      name: 'Billing',
      months: months({ 10: [80, 60], 11: [80, 22.5] }),
      totalPlanned: 160,
      totalLogged: 82.5,
    },
    {
      id: 'p2',
      name: 'Payments API',
      months: months({ 10: [40, 40] }),
      totalPlanned: 40,
      totalLogged: 40,
    },
  ],
  members: [
    {
      employeeId: 'e1',
      firstName: 'Ethan',
      lastName: 'Clarke',
      jobTitle: 'Backend Developer',
      months: months({ 10: [120, 100], 11: [80, 22.5] }),
      totalPlanned: 200,
      totalLogged: 122.5,
    },
    {
      employeeId: 'e2',
      firstName: 'Olivia',
      lastName: 'Reed',
      jobTitle: 'Backend Developer',
      months: months(),
      totalPlanned: 0,
      totalLogged: 0,
    },
  ],
  totalPlanned: 200,
  totalLogged: 122.5,
};

const chart: OrgChartNode[] = [
  {
    id: 'ceo',
    firstName: 'Margaret',
    lastName: 'Hayes',
    jobTitle: 'CEO',
    email: '',
    directReports: [
      {
        id: 'm1',
        firstName: 'Lucas',
        lastName: 'Bennett',
        jobTitle: '',
        email: '',
        directReports: [
          {
            id: 'e1',
            firstName: 'Ethan',
            lastName: 'Clarke',
            jobTitle: '',
            email: '',
            directReports: [],
          },
          {
            id: 'e2',
            firstName: 'Olivia',
            lastName: 'Reed',
            jobTitle: '',
            email: '',
            directReports: [],
          },
        ],
      },
    ],
  },
];

describe('Statistics', () => {
  let getTeamStatistics: ReturnType<typeof vi.fn>;
  let fixture: ComponentFixture<Statistics>;

  async function render(
    result: ReturnType<AllocationsService['getTeamStatistics']> = of(statistics),
  ): Promise<HTMLElement> {
    getTeamStatistics = vi.fn(() => result);
    TestBed.configureTestingModule({
      imports: [Statistics],
      providers: [
        provideRouter([]),
        { provide: AllocationsService, useValue: { getTeamStatistics } },
        { provide: OrgChartService, useValue: { getOrgChart: () => of(chart) } },
      ],
    });
    fixture = TestBed.createComponent(Statistics);
    fixture.componentRef.setInput('year', '2026');
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function cells(row: Element): string[] {
    return [...row.querySelectorAll('th, td')].map((cell) =>
      (cell.textContent ?? '').replace(/\s+/g, ' ').trim(),
    );
  }

  it('loads the year and shows its totals', async () => {
    const element = await render();

    expect(getTeamStatistics).toHaveBeenCalledWith('2026');
    expect(element.querySelector('.year-label')?.textContent).toBe('2026');
    expect(element.querySelector('.summary-planned')?.textContent).toContain('200');
    expect(element.querySelector('.summary-logged')?.textContent).toContain('122.5');
    expect(element.querySelector('.summary-percent')?.textContent).toContain('61');
  });

  it('draws planned and logged bars for every month, with links to the Hours page', async () => {
    const element = await render();

    expect(element.querySelectorAll('.month-bars')).toHaveLength(12);
    // Two reports × 160 h = 320 h is the top of the scale.
    const october = element.querySelectorAll('.month-bars')[9];
    expect(october.querySelector<HTMLElement>('.bar.planned')?.style.height).toBe('37.5%');
    expect(october.querySelector<HTMLElement>('.capacity-tick')?.style.bottom).toBe('100%');
    expect(element.querySelector('.bars')?.getAttribute('aria-label')).toContain(
      'Nov: 80 planned, 22.5 logged of 320',
    );
    const links = element.querySelectorAll('.month-links a');
    expect(links).toHaveLength(12);
    expect(links[10].getAttribute('href')).toBe('/allocations?month=2026-11');
  });

  it('lists the months, projects and reports', async () => {
    const element = await render();

    const november = element.querySelectorAll('.by-month tbody tr')[10];
    expect(cells(november)).toEqual(['November 2026', '320', '80', '22.5', '−57.5', '16 / 4']);
    expect(cells(element.querySelector('.by-month tfoot tr')!)).toEqual([
      'Total',
      '',
      '200',
      '122.5',
      '−77.5',
      '',
    ]);

    const projects = [...element.querySelectorAll('.by-project tbody tr')];
    expect(projects.map((row) => cells(row)[0])).toEqual(['Billing', 'Payments API']);
    expect(cells(projects[0])[10]).toBe('80 / 60');
    expect(cells(projects[0]).at(-1)).toBe('160 / 82.5');
    expect(projects[1].getAttribute('style')).toContain('--project: var(--project-1)');

    const reports = [...element.querySelectorAll('.by-report tbody tr')];
    expect(reports.map((row) => row.querySelector('.member-name')?.textContent)).toEqual([
      'Ethan Clarke',
      'Olivia Reed',
    ]);
    expect(reports[0].querySelector<HTMLElement>('.stop')?.style.getPropertyValue('--line')).toBe(
      'var(--line-0)',
    );
    expect(cells(reports[0]).at(-1)).toBe('200 / 122.5');
  });

  it('shows empty states for no reports and no projects', async () => {
    let element = await render(of({ ...statistics, members: [] }));
    expect(element.querySelector('.status')?.textContent).toContain('no direct reports');

    TestBed.resetTestingModule();
    element = await render(of({ ...statistics, projects: [] }));
    expect(element.querySelector('.status')?.textContent).toContain('don’t run any projects');
    expect(element.querySelector('.summary')).toBeNull();
  });

  it('shows an error with Retry', async () => {
    const element = await render(throwError(() => new Error('boom')));

    expect(element.querySelector('[role="alert"]')?.textContent).toContain("couldn't be loaded");
    getTeamStatistics.mockReturnValue(of(statistics));
    element.querySelector<HTMLButtonElement>('[role="alert"] button')!.click();
    await fixture.whenStable();
    expect(element.querySelector('.summary')).not.toBeNull();
  });

  it('navigates to the previous and next year', async () => {
    const element = await render();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    element.querySelector<HTMLButtonElement>('[aria-label="Previous year"]')!.click();
    element.querySelector<HTMLButtonElement>('[aria-label="Next year"]')!.click();

    expect(navigate).toHaveBeenCalledWith([], { queryParams: { year: '2025' } });
    expect(navigate).toHaveBeenCalledWith([], { queryParams: { year: '2027' } });
  });
});
