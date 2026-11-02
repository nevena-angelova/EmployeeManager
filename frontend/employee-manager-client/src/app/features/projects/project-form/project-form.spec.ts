import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { OrgChartNode } from '../../org-chart/org-chart.model';
import { Project } from '../project.model';
import { ProjectsService } from '../projects.service';
import { ProjectForm } from './project-form';

function report(id: string, firstName: string, lastName: string): OrgChartNode {
  return { id, firstName, lastName, jobTitle: 'Developer', email: '', directReports: [] };
}

const ethan = report('e1', 'Ethan', 'Clarke');
const hannah = report('e2', 'Hannah', 'Lindqvist');

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

describe('ProjectForm', () => {
  let fixture: ComponentFixture<ProjectForm>;
  let element: HTMLElement;
  let service: { create: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn> };
  let saved: Project[];

  async function render(
    current: Project | null,
    response: () => Observable<Project> = () => of(project),
  ): Promise<void> {
    service = { create: vi.fn(response), update: vi.fn(response) };
    TestBed.configureTestingModule({
      imports: [ProjectForm],
      providers: [{ provide: ProjectsService, useValue: service }],
    });
    fixture = TestBed.createComponent(ProjectForm);
    fixture.componentRef.setInput('project', current);
    fixture.componentRef.setInput('directReports', [ethan, hannah]);
    saved = [];
    fixture.componentInstance.saved.subscribe((p) => saved.push(p));
    await fixture.whenStable();
    element = fixture.nativeElement as HTMLElement;
  }

  async function fill(id: string, value: string): Promise<void> {
    const control = element.querySelector<HTMLInputElement | HTMLTextAreaElement>(`#${id}`)!;
    control.value = value;
    control.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  function checkbox(name: string): HTMLInputElement {
    return [...element.querySelectorAll('.members label')]
      .find((l) => l.textContent?.includes(name))!
      .querySelector('input')!;
  }

  async function click(input: HTMLInputElement): Promise<void> {
    input.click();
    await fixture.whenStable();
  }

  async function submit(): Promise<void> {
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  }

  it('creates a project with the ticked direct reports', async () => {
    await render(null);

    await fill('name', '  Payments API ');
    await click(checkbox('Hannah Lindqvist'));
    await submit();

    expect(service.create).toHaveBeenCalledWith({
      name: 'Payments API',
      description: null,
      memberIds: ['e2'],
    });
    expect(saved).toEqual([project]);
  });

  it('does not submit without a name', async () => {
    await render(null);

    await submit();

    expect(service.create).not.toHaveBeenCalled();
    expect(element.querySelector('.field-error')?.textContent).toContain('Enter a project name.');
  });

  it('edits a project, keeping a former report who can only be removed', async () => {
    await render(project);

    expect(element.querySelector<HTMLInputElement>('#name')!.value).toBe('Payments API');
    expect(checkbox('Ethan Clarke').checked).toBe(true);
    expect(checkbox('Omar Haddad').checked).toBe(true);
    expect(element.querySelector('.tag')?.textContent).toContain('No longer your report');

    await click(checkbox('Omar Haddad'));
    expect(checkbox('Omar Haddad').disabled).toBe(true);
    await submit();

    expect(service.update).toHaveBeenCalledWith('p1', {
      name: 'Payments API',
      description: 'Move card payments.',
      memberIds: ['e1'],
    });
  });

  it('shows API validation errors under their fields', async () => {
    await render(null, () =>
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: {
              errors: {
                name: ['You already have a project with this name.'],
                memberIds: ['Only your direct reports can be added to a project.'],
              },
            },
          }),
      ),
    );

    await fill('name', 'Payments API');
    await submit();

    const errors = [...element.querySelectorAll('.field-error')].map((e) => e.textContent?.trim());
    expect(errors).toEqual([
      'You already have a project with this name.',
      'Only your direct reports can be added to a project.',
    ]);
  });
});
