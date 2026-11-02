import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { CurrentUser } from '../../core/auth/auth.model';
import { AuthService } from '../../core/auth/auth.service';
import { OrgChartService } from '../org-chart/org-chart.service';
import { ProjectSummary } from './project.model';
import { Projects } from './projects';
import { ProjectsService } from './projects.service';

const lucas: CurrentUser = {
  employeeId: 'm1',
  email: 'lucas.bennett@example.com',
  firstName: 'Lucas',
  lastName: 'Bennett',
  jobTitle: 'Backend Team Lead',
  role: 'Manager',
};

const payments: ProjectSummary = {
  id: 'p1',
  name: 'Payments API',
  description: 'Move card payments.',
  ownerId: 'm1',
  ownerName: 'Lucas Bennett',
  memberCount: 2,
};

const planning: ProjectSummary = {
  id: 'p2',
  name: 'Annual planning',
  description: null,
  ownerId: 'ceo',
  ownerName: 'Margaret Hayes',
  memberCount: 1,
};

describe('Projects', () => {
  let service: { listManaged: ReturnType<typeof vi.fn>; listMine: ReturnType<typeof vi.fn> };

  async function render(user: CurrentUser): Promise<HTMLElement> {
    service = { listManaged: vi.fn(() => of([payments])), listMine: vi.fn(() => of([planning])) };
    TestBed.configureTestingModule({
      imports: [Projects],
      providers: [
        provideRouter([]),
        { provide: ProjectsService, useValue: service },
        { provide: OrgChartService, useValue: { getOrgChart: () => of([]) } },
        { provide: AuthService, useValue: { currentUser: signal(user) } },
      ],
    });
    const fixture = TestBed.createComponent(Projects);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function sections(element: HTMLElement): Record<string, string[]> {
    return Object.fromEntries(
      [...element.querySelectorAll('section[aria-labelledby]')].map((s) => [
        s.querySelector('h3')!.textContent!.trim(),
        [...s.querySelectorAll('.project-name')].map((a) => a.textContent!.trim()),
      ]),
    );
  }

  it('shows a manager their own projects and the ones they are on', async () => {
    const element = await render(lucas);

    expect(sections(element)).toEqual({
      'Projects I manage': ['Payments API'],
      'Projects I’m on': ['Annual planning'],
    });
    expect(element.querySelector('button.primary')?.textContent?.trim()).toBe('New project');
  });

  it('shows an employee only the projects they are on, without New project', async () => {
    const element = await render({ ...lucas, role: 'Employee' });

    expect(sections(element)).toEqual({ 'Projects I’m on': ['Annual planning'] });
    expect(service.listManaged).not.toHaveBeenCalled();
    expect(element.querySelector('button.primary')).toBeNull();
  });
});
