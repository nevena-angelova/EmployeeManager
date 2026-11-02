import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { CurrentUser } from '../../core/auth/auth.model';
import { AuthService } from '../../core/auth/auth.service';
import { OrgChartNode } from '../org-chart/org-chart.model';
import { OrgChartService } from '../org-chart/org-chart.service';
import { MyAllocations, TeamAllocations } from './allocation.model';
import { Allocations } from './allocations';
import { AllocationsService } from './allocations.service';

const lucas: CurrentUser = {
  employeeId: 'm1',
  email: 'lucas.bennett@example.com',
  firstName: 'Lucas',
  lastName: 'Bennett',
  jobTitle: 'Backend Team Lead',
  role: 'Manager',
};

const team: TeamAllocations = {
  month: '2026-11',
  capacityHours: 168,
  isEditable: true,
  projects: [
    { id: 'p1', name: 'Billing' },
    { id: 'p2', name: 'Payments API' },
  ],
  members: [
    {
      employeeId: 'e1',
      firstName: 'Ethan',
      lastName: 'Clarke',
      jobTitle: 'Backend Developer',
      projectIds: ['p1', 'p2'],
      hours: [
        { projectId: 'p1', hours: 80 },
        { projectId: 'p2', hours: 40 },
      ],
      otherHours: 16,
      logged: [{ projectId: 'p1', hours: 22.5 }],
      otherLoggedHours: 4,
    },
    {
      employeeId: 'e2',
      firstName: 'Olivia',
      lastName: 'Reed',
      jobTitle: 'Backend Developer',
      projectIds: ['p2'],
      hours: [],
      otherHours: 0,
      logged: [],
      otherLoggedHours: 0,
    },
  ],
};

const mine: MyAllocations = {
  month: '2026-11',
  capacityHours: 168,
  projects: [
    {
      projectId: 'p9',
      projectName: 'Annual planning',
      ownerName: 'Margaret Hayes',
      hours: 12,
      loggedHours: 14.5,
    },
  ],
  totalHours: 12,
  totalLoggedHours: 14.5,
};

const chart: OrgChartNode[] = [
  {
    id: 'ceo',
    firstName: 'Margaret',
    lastName: 'Hayes',
    jobTitle: 'CEO',
    email: 'margaret.hayes@example.com',
    directReports: [
      {
        id: 'm1',
        firstName: 'Lucas',
        lastName: 'Bennett',
        jobTitle: 'Backend Team Lead',
        email: 'lucas.bennett@example.com',
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

describe('Allocations', () => {
  let service: {
    getTeam: ReturnType<typeof vi.fn>;
    saveTeam: ReturnType<typeof vi.fn>;
    getMine: ReturnType<typeof vi.fn>;
  };
  let fixture: ComponentFixture<Allocations>;

  async function render(user: CurrentUser, grid: TeamAllocations = team): Promise<HTMLElement> {
    service = {
      getTeam: vi.fn(() => of(grid)),
      saveTeam: vi.fn(() => of(grid)),
      getMine: vi.fn(() => of(mine)),
    };
    TestBed.configureTestingModule({
      imports: [Allocations],
      providers: [
        provideRouter([]),
        { provide: AllocationsService, useValue: service },
        { provide: OrgChartService, useValue: { getOrgChart: () => of(chart) } },
        { provide: AuthService, useValue: { currentUser: signal(user) } },
      ],
    });
    fixture = TestBed.createComponent(Allocations);
    fixture.componentRef.setInput('month', '2026-11');
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function rows(element: HTMLElement): string[][] {
    return [...element.querySelectorAll('.grid tbody tr:not(.row-error)')].map((tr) =>
      [...tr.querySelectorAll('th, td')].map(
        (cell) =>
          cell.querySelector('input')?.value ??
          cell.querySelector('.member-name, .total-value, .none')?.textContent?.trim() ??
          '',
      ),
    );
  }

  async function type(input: HTMLInputElement, value: string): Promise<void> {
    input.value = value;
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  it('shows a manager the grid with totals including other hours', async () => {
    const element = await render(lucas);

    expect(service.getTeam).toHaveBeenCalledWith('2026-11');
    expect(element.querySelector('.month-label')?.textContent?.trim()).toBe('November 2026');
    expect(
      [...element.querySelectorAll('.grid thead th')].map((th) =>
        [...th.childNodes]
          .filter((node) => !(node instanceof Element && node.classList.contains('pair-caption')))
          .map((node) => node.textContent)
          .join('')
          .trim(),
      ),
    ).toEqual(['Direct report', 'Billing', 'Payments API', 'Total / 168 h']);
    expect(element.querySelector('.grid thead .pair-caption')?.textContent).toBe('PlannedLogged');
    expect(rows(element)).toEqual([
      ['Ethan Clarke', '80', '40', '136'],
      ['Olivia Reed', '—', '0', '0'],
    ]);
    expect(element.querySelector('.grid tbody .total')?.getAttribute('title')).toBe(
      'Includes 16 h on other managers’ projects',
    );
    expect(element.querySelector<HTMLElement>('.stop')?.style.getPropertyValue('--line')).toBe(
      'var(--line-0)',
    );
  });

  it('updates totals as you type and flags going over capacity', async () => {
    const element = await render(lucas);
    const input = element.querySelector<HTMLInputElement>('.grid tbody input')!;

    await type(input, '120.5');

    expect(rows(element)[0][3]).toBe('176.5');
    expect(element.querySelector('.grid tbody .total')?.classList).toContain('over');
    expect(element.querySelector('.over-label')?.textContent).toContain('Over capacity');
  });

  it('saves the non-zero hours', async () => {
    const element = await render(lucas);
    await type(element.querySelectorAll<HTMLInputElement>('.grid tbody input')[2], '20');

    element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    await fixture.whenStable();

    expect(service.saveTeam).toHaveBeenCalledWith('2026-11', {
      allocations: [
        { employeeId: 'e1', projectId: 'p1', hours: 80 },
        { employeeId: 'e1', projectId: 'p2', hours: 40 },
        { employeeId: 'e2', projectId: 'p2', hours: 20 },
      ],
    });
  });

  it('shows a 400 error under its row', async () => {
    const element = await render(lucas);
    service.saveTeam.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: { errors: { 'allocations.e1': ['Ethan Clarke is over capacity.'] } },
          }),
      ),
    );

    element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    await fixture.whenStable();

    const error = element.querySelector('.row-error');
    expect(error?.textContent?.trim()).toBe('Ethan Clarke is over capacity.');
    expect(error?.previousElementSibling?.querySelector('.member-name')?.textContent).toBe(
      'Ethan Clarke',
    );
  });

  it('shows a 409 as a banner with Reload', async () => {
    const element = await render(lucas);
    service.saveTeam.mockReturnValue(
      throwError(
        () => new HttpErrorResponse({ status: 409, error: { detail: 'The hours were changed.' } }),
      ),
    );

    element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    await fixture.whenStable();
    const banner = element.querySelector('.banner-error');
    expect(banner?.textContent).toContain('The hours were changed.');

    banner!.querySelector('button')!.click();
    await fixture.whenStable();
    expect(service.getTeam).toHaveBeenCalledTimes(2);
    expect(element.querySelector('.banner-error')).toBeNull();
  });

  it('makes a past month read-only', async () => {
    const element = await render(lucas, { ...team, isEditable: false });

    const inputs = [...element.querySelectorAll<HTMLInputElement>('.grid tbody input')];
    expect(inputs.length).toBe(3);
    expect(inputs.every((input) => input.disabled)).toBe(true);
    expect(element.querySelector('button[type="submit"]')).toBeNull();
    expect(element.querySelector('.note')?.textContent).toContain('read-only');
    // Read-only hours keep their blocks rather than turning into bare numbers.
    expect(inputs.every((input) => input.closest('.block'))).toBe(true);
  });

  it('shows empty states for no reports and no projects', async () => {
    let element = await render(lucas, { ...team, members: [] });
    expect(element.textContent).toContain('You have no direct reports');

    TestBed.resetTestingModule();
    element = await render(lucas, { ...team, projects: [] });
    expect(element.querySelector('.status a')?.getAttribute('href')).toBe('/projects');
  });

  it('shows an employee only their own hours', async () => {
    const element = await render({ ...lucas, role: 'Employee' });

    expect(service.getTeam).not.toHaveBeenCalled();
    expect(element.querySelector('.grid')).toBeNull();
    expect(element.querySelector('.mine .project-name')?.textContent).toBe('Annual planning');
    expect(element.querySelector('.mine tfoot')?.textContent).toContain('12');
    expect(element.querySelector('.mine tfoot')?.textContent).toContain('/ 168 h');
  });

  it('shows the logged hours next to the allocated ones', async () => {
    const element = await render(lucas);

    const ethan = element.querySelector('.grid tbody tr')!;
    const logged = [...ethan.querySelectorAll('td.hours')].map(
      (cell) => cell.querySelector('.block.logged .value')?.textContent ?? null,
    );
    expect(logged).toEqual(['22.5', '0']);
    expect(ethan.querySelector('.total-logged .total-value')?.textContent).toBe('26.5');
    expect(ethan.querySelector('.total-logged')?.textContent).toContain('incl. 4 h elsewhere');

    const row = element.querySelector('.mine tbody tr')!;
    const logCell = row.querySelector('.logged-hours')!;
    expect(logCell.textContent?.trim()).toBe('14.5');
    expect(logCell.classList).toContain('ahead');
    expect(element.querySelector('.mine tfoot .logged-hours')?.textContent).toContain('14.5 h');
    expect(element.querySelector('.mine-logged')?.textContent).toBe('14.5 h');

    // A logged bar under each planned one.
    const rails = [...ethan.querySelectorAll('.total .rail')];
    expect(rails.map((rail) => rail.getAttribute('aria-label'))).toEqual([
      '136 of 168 hours: Billing 80, Payments API 40, elsewhere 16',
      'Logged 26.5 of 168 hours: Billing 22.5, elsewhere 4',
    ]);
    expect(element.querySelector('.mine-rail.logged-rail .rail')?.getAttribute('aria-label')).toBe(
      'Logged 14.5 of 168 hours: Annual planning 14.5',
    );
  });

  it('navigates to the previous and next month', async () => {
    const element = await render({ ...lucas, role: 'Employee' });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    element.querySelector<HTMLButtonElement>('[aria-label="Previous month"]')!.click();
    element.querySelector<HTMLButtonElement>('[aria-label="Next month"]')!.click();

    expect(navigate).toHaveBeenCalledWith([], { queryParams: { month: '2026-10' } });
    expect(navigate).toHaveBeenCalledWith([], { queryParams: { month: '2026-12' } });
  });
});
