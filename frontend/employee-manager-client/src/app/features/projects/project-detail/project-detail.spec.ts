import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { CurrentUser } from '../../../core/auth/auth.model';
import { AuthService } from '../../../core/auth/auth.service';
import { OrgChartService } from '../../org-chart/org-chart.service';
import { Project } from '../project.model';
import { ProjectsService } from '../projects.service';
import { ProjectDetail } from './project-detail';

const user = (employeeId: string): CurrentUser => ({
  employeeId,
  email: '',
  firstName: '',
  lastName: '',
  jobTitle: '',
  role: 'Manager',
});

const project: Project = {
  id: 'p1',
  name: 'Payments API',
  description: 'Move card payments.',
  ownerId: 'm1',
  ownerName: 'Lucas Bennett',
  members: [
    {
      employeeId: 'e1',
      firstName: 'Ethan',
      lastName: 'Clarke',
      jobTitle: 'Developer',
      isDirectReport: true,
    },
    {
      employeeId: 'x9',
      firstName: 'Omar',
      lastName: 'Haddad',
      jobTitle: 'QA',
      isDirectReport: false,
    },
  ],
};

describe('ProjectDetail', () => {
  let fixture: ComponentFixture<ProjectDetail>;
  let element: HTMLElement;
  let service: { get: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn> };

  async function render(
    currentUser: CurrentUser,
    get: () => Observable<Project> = () => of(project),
  ): Promise<void> {
    service = { get: vi.fn(get), delete: vi.fn(() => of(undefined)) };
    TestBed.configureTestingModule({
      imports: [ProjectDetail],
      providers: [
        provideRouter([]),
        { provide: ProjectsService, useValue: service },
        { provide: OrgChartService, useValue: { getOrgChart: () => of([]) } },
        { provide: AuthService, useValue: { currentUser: signal(currentUser) } },
      ],
    });
    fixture = TestBed.createComponent(ProjectDetail);
    fixture.componentRef.setInput('id', 'p1');
    await fixture.whenStable();
    element = fixture.nativeElement as HTMLElement;
  }

  function button(label: string): HTMLButtonElement | undefined {
    return [...element.querySelectorAll('button')].find((b) => b.textContent?.trim() === label);
  }

  afterEach(() => vi.restoreAllMocks());

  it('shows the project and its members, flagging a former report', async () => {
    await render(user('m1'));

    expect(element.querySelector('h2')?.textContent).toBe('Payments API');
    const members = [...element.querySelectorAll('.members li')].map((li) =>
      [...li.querySelectorAll('span:not(.stop)')].map((s) => s.textContent?.trim()).join(' '),
    );
    expect(members).toEqual(['Ethan Clarke Developer', 'Omar Haddad QA No longer your report']);
  });

  it('lets the owner edit and delete', async () => {
    await render(user('m1'));
    expect(button('Edit')).toBeDefined();

    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    button('Delete')!.click();
    await fixture.whenStable();

    expect(service.delete).toHaveBeenCalledWith('p1');
    expect(navigate).toHaveBeenCalledWith(['/projects']);
  });

  it('is read-only for members', async () => {
    await render(user('e1'));

    expect(button('Edit')).toBeUndefined();
    expect(button('Delete')).toBeUndefined();
    expect(element.querySelector('.tag')?.textContent).toContain('Moved to another team');
  });

  it('says when the project is not found', async () => {
    await render(user('e1'), () => throwError(() => new HttpErrorResponse({ status: 404 })));

    expect(element.querySelector('[role="alert"]')?.textContent).toContain("doesn't exist");
  });
});
