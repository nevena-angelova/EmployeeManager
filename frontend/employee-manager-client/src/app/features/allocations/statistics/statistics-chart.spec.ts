import { MonthStatistics } from '../allocation.model';
import { monthBars, shortMonth } from './statistics-chart';

function month(
  index: number,
  planned: number,
  logged: number,
  capacityHours = 160,
): MonthStatistics {
  return {
    month: `2026-${String(index).padStart(2, '0')}`,
    capacityHours,
    planned,
    logged,
    otherPlanned: 0,
    otherLogged: 0,
  };
}

describe('monthBars', () => {
  it('puts every month on one scale set by the tallest bar or capacity', () => {
    const chart = monthBars([month(1, 200, 100), month(2, 400, 500)], 2);

    // Two reports × 160 h = 320 h of capacity; February's 500 logged is the top.
    expect(chart.scaleHours).toBe(500);
    expect(chart.bars.map((b) => [b.plannedPercent, b.loggedPercent, b.capacityPercent])).toEqual([
      [40, 20, 64],
      [80, 100, 64],
    ]);
    expect(chart.bars[0].capacityHours).toBe(320);
    expect(chart.bars.map((b) => b.label)).toEqual(['Jan', 'Feb']);
  });

  it('scales to capacity when the team has fewer hours', () => {
    const chart = monthBars([month(3, 80, 40, 168)], 1);

    expect(chart.scaleHours).toBe(168);
    expect(chart.bars[0].capacityPercent).toBe(100);
  });

  it('copes with an empty year', () => {
    const chart = monthBars([month(1, 0, 0)], 0);

    expect(chart.scaleHours).toBe(0);
    expect(chart.bars[0]).toMatchObject({
      plannedPercent: 0,
      loggedPercent: 0,
      capacityPercent: 0,
    });
  });

  it('describes the chart for screen readers', () => {
    expect(monthBars([month(1, 80, 22.5), month(2, 0, 0)], 1).description).toBe(
      'Planned and logged hours by month. Jan: 80 planned, 22.5 logged of 160; Feb: 0 planned, 0 logged of 160.',
    );
  });

  it('labels months', () => {
    expect(shortMonth('2026-12')).toBe('Dec');
  });
});
