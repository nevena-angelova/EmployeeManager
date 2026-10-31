import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OrgChartNode } from '../org-chart.model';
import { OrgChartBranch } from './org-chart-branch';

function employee(
  firstName: string,
  lastName: string,
  directReports: OrgChartNode[] = [],
): OrgChartNode {
  return {
    id: `${firstName}-${lastName}`,
    firstName,
    lastName,
    jobTitle: `${firstName}'s title`,
    email: `${firstName}.${lastName}@example.com`.toLowerCase(),
    directReports,
  };
}

describe('OrgChartBranch', () => {
  let fixture: ComponentFixture<OrgChartBranch>;
  let element: HTMLElement;

  async function render(node: OrgChartNode): Promise<void> {
    fixture = TestBed.createComponent(OrgChartBranch);
    fixture.componentRef.setInput('node', node);
    await fixture.whenStable();
    element = fixture.nativeElement as HTMLElement;
  }

  const names = () => [...element.querySelectorAll('.name')].map((el) => el.textContent?.trim());

  it('renders the employee card', async () => {
    await render(employee('Margaret', 'Hayes'));

    expect(element.querySelector('.avatar')?.textContent?.trim()).toBe('MH');
    expect(names()).toEqual(['Margaret Hayes']);
    expect(element.querySelector('.job-title')?.textContent?.trim()).toBe("Margaret's title");
    expect(element.querySelector('.email')?.getAttribute('href')).toBe(
      'mailto:margaret.hayes@example.com',
    );
  });

  it('has no toggle for an employee without reports', async () => {
    await render(employee('Ethan', 'Clarke'));

    expect(element.querySelector('.toggle')).toBeNull();
    expect(element.querySelector('.reports')).toBeNull();
  });

  it('renders direct and indirect reports recursively', async () => {
    await render(
      employee('Margaret', 'Hayes', [
        employee('Daniel', 'Okafor', [employee('Lucas', 'Bennett')]),
        employee('Sofia', 'Marin'),
      ]),
    );

    expect(names()).toEqual(['Margaret Hayes', 'Daniel Okafor', 'Lucas Bennett', 'Sofia Marin']);
    expect(element.querySelector('.toggle')?.textContent).toContain('2 reports');
  });

  it('collapses and expands the reports when the toggle is clicked', async () => {
    await render(employee('Daniel', 'Okafor', [employee('Lucas', 'Bennett')]));
    const toggle = element.querySelector<HTMLButtonElement>('.toggle')!;

    expect(toggle.textContent).toContain('1 report');
    expect(toggle.getAttribute('aria-expanded')).toBe('true');

    toggle.click();
    await fixture.whenStable();

    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    expect(names()).toEqual(['Daniel Okafor']);

    toggle.click();
    await fixture.whenStable();

    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    expect(names()).toEqual(['Daniel Okafor', 'Lucas Bennett']);
  });
});
