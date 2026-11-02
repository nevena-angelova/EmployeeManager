import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { DailyHours } from './daily-hours';
import { today } from './day';
import { DayTimeEntries } from './time-entry.model';
import { TimeEntriesService } from './time-entries.service';

function dayOf(date: string, overrides: Partial<DayTimeEntries> = {}): DayTimeEntries {
  return {
    date,
    isEditable: true,
    maxHours: 24,
    projects: [
      { projectId: 'p1', projectName: 'Alpha', ownerName: 'Mia Manager', hours: 6 },
      { projectId: 'p2', projectName: 'Beta', ownerName: 'Noah Other', hours: 0 },
    ],
    totalHours: 6,
    // Monday 26 October to Sunday 1 November 2026.
    week: [
      { date: '2026-10-26', hours: 8 },
      { date: '2026-10-27', hours: 0 },
      { date: '2026-10-28', hours: 7.5 },
      { date: '2026-10-29', hours: 6 },
      { date: '2026-10-30', hours: 8 },
      { date: '2026-10-31', hours: 0 },
      { date: '2026-11-01', hours: 0 },
    ],
    ...overrides,
  };
}

describe('DailyHours', () => {
  let service: { getMine: ReturnType<typeof vi.fn>; saveMine: ReturnType<typeof vi.fn> };
  let fixture: ComponentFixture<DailyHours>;

  async function render(day: DayTimeEntries, date = '2026-10-29'): Promise<HTMLElement> {
    service = {
      getMine: vi.fn(() => of(day)),
      saveMine: vi.fn((_date: string, body: { entries: { hours: number }[] }) =>
        of({
          ...day,
          totalHours: body.entries.reduce((total, e) => total + e.hours, 0),
        }),
      ),
    };
    TestBed.configureTestingModule({
      imports: [DailyHours],
      providers: [provideRouter([]), { provide: TimeEntriesService, useValue: service }],
    });
    fixture = TestBed.createComponent(DailyHours);
    fixture.componentRef.setInput('date', date);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function inputs(element: HTMLElement): HTMLInputElement[] {
    return [...element.querySelectorAll<HTMLInputElement>('input[type=number]')];
  }

  async function type(input: HTMLInputElement, value: string): Promise<void> {
    input.value = value;
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  it('shows the day, its projects and the week', async () => {
    const element = await render(dayOf('2026-10-29'));

    expect(service.getMine).toHaveBeenCalledWith('2026-10-29');
    expect(element.querySelector('.day-label')?.textContent?.trim()).toBe(
      'Thursday, 29 October 2026',
    );
    expect(
      [...element.querySelectorAll('.project-name')].map((e) => e.textContent?.trim()),
    ).toEqual(['Alpha', 'Beta']);
    expect(inputs(element).map((i) => i.value)).toEqual(['6', '0']);
    expect(element.querySelector('.total-value')?.textContent?.trim()).toBe('6 h');

    const days = [...element.querySelectorAll<HTMLButtonElement>('.week-day')];
    expect(days).toHaveLength(7);
    expect(days[3].classList).toContain('selected');
    // Tuesday is a weekday with nothing logged; the weekend isn't flagged.
    expect(days.map((d) => d.classList.contains('missing'))).toEqual([
      false,
      true,
      false,
      false,
      false,
      false,
      false,
    ]);
  });

  it('updates the total as you type and saves the day', async () => {
    const element = await render(dayOf('2026-10-29'));

    await type(inputs(element)[1], '1.5');
    expect(element.querySelector('.total-value')?.textContent?.trim()).toBe('7.5 h');
    expect(element.querySelectorAll('.week-day .day-hours')[3].textContent?.trim()).toBe('7.5 h');

    element.querySelector<HTMLButtonElement>('button[type=submit]')!.click();
    await fixture.whenStable();

    expect(service.saveMine).toHaveBeenCalledWith('2026-10-29', {
      entries: [
        { projectId: 'p1', hours: 6 },
        { projectId: 'p2', hours: 1.5 },
      ],
    });
    expect(element.querySelector('.saved')?.textContent?.trim()).toBe('Saved');
  });

  it('does not save hours that are not in half hours or go over a day', async () => {
    const element = await render(dayOf('2026-10-29'));

    await type(inputs(element)[1], '1.25');
    element.querySelector<HTMLButtonElement>('button[type=submit]')!.click();
    await fixture.whenStable();
    expect(element.querySelector('.form-error')?.textContent).toContain('steps of 0.5');

    await type(inputs(element)[1], '20');
    element.querySelector<HTMLButtonElement>('button[type=submit]')!.click();
    await fixture.whenStable();
    expect(element.querySelector('.form-error')?.textContent).toContain('at most 24 h');

    expect(service.saveMine).not.toHaveBeenCalled();
  });

  it('shows a 409 with Reload', async () => {
    const element = await render(dayOf('2026-10-29'));
    service.saveMine.mockReturnValue(
      throwError(
        () => new HttpErrorResponse({ status: 409, error: { detail: 'Your hours changed.' } }),
      ),
    );

    element.querySelector<HTMLButtonElement>('button[type=submit]')!.click();
    await fixture.whenStable();

    expect(element.querySelector('.banner-error')?.textContent).toContain('Your hours changed.');
  });

  it('makes a future day read-only', async () => {
    const element = await render(dayOf('2026-11-01', { isEditable: false }), '2026-11-01');

    expect(inputs(element).every((i) => i.disabled)).toBe(true);
    expect(element.querySelector('.note')?.textContent).toContain('hasn’t happened yet');
    expect(element.querySelector('button[type=submit]')).toBeNull();
  });

  it('moves between days with the query param', async () => {
    const element = await render(dayOf('2026-10-29'));
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    element.querySelector<HTMLButtonElement>('[aria-label="Next day"]')!.click();
    expect(navigate).toHaveBeenLastCalledWith([], {
      queryParams: { date: '2026-10-30' },
      queryParamsHandling: 'merge',
    });

    element.querySelectorAll<HTMLButtonElement>('.week-day')[0].click();
    expect(navigate).toHaveBeenLastCalledWith([], {
      queryParams: { date: '2026-10-26' },
      queryParamsHandling: 'merge',
    });

    element.querySelector<HTMLButtonElement>('button.today')!.click();
    expect(navigate).toHaveBeenLastCalledWith([], {
      queryParams: { date: today() },
      queryParamsHandling: 'merge',
    });
  });

  it('says when there are no projects to log hours on', async () => {
    const element = await render(dayOf('2026-10-29', { projects: [], totalHours: 0 }));

    expect(element.querySelector('.status')?.textContent).toContain('aren’t on any projects');
    expect(element.querySelector('form')).toBeNull();
  });
});
