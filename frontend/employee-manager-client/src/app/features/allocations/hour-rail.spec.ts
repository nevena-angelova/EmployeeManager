import { hourRail, projectColour, RailPart } from './hour-rail';

const part = (key: string, hours: number): RailPart => ({
  key,
  label: key,
  hours,
  colour: 'red',
});

describe('hourRail', () => {
  it('lays out the hours against capacity, with the rest free', () => {
    const rail = hourRail([part('Atlas', 40), part('Beacon', 20)], 20, 160);

    expect(rail.segments.map((s) => [s.key, s.percent])).toEqual([
      ['Atlas', 25],
      ['Beacon', 12.5],
    ]);
    expect(rail.elsewherePercent).toBe(12.5);
    expect(rail.freePercent).toBe(50);
    expect(rail.capacityPercent).toBe(100);
    expect(rail.totalHours).toBe(80);
    expect(rail.isOver).toBe(false);
    expect(rail.description).toBe('80 of 160 hours: Atlas 40, Beacon 20, elsewhere 20');
  });

  it('stretches to the total when over capacity, so the mark sits inside', () => {
    const rail = hourRail([part('Atlas', 150)], 50, 160);

    expect(rail.segments[0].percent).toBe(75);
    expect(rail.elsewherePercent).toBe(25);
    expect(rail.freePercent).toBe(0);
    expect(rail.capacityPercent).toBe(80);
    expect(rail.isOver).toBe(true);
  });

  it('leaves out empty projects and copes with no hours or no capacity', () => {
    const empty = hourRail([part('Atlas', 0)], 0, 160);
    expect(empty.segments).toEqual([]);
    expect(empty.freePercent).toBe(100);
    expect(empty.description).toBe('0 of 160 hours');

    const none = hourRail([], 0, 0);
    expect(none.freePercent).toBe(0);
    expect(none.capacityPercent).toBe(100);
    expect(none.isOver).toBe(false);
  });
});

describe('projectColour', () => {
  it('cycles through the palette', () => {
    expect(projectColour(0)).toEqual({ edge: 'var(--project-0)', tint: 'var(--project-0-tint)' });
    expect(projectColour(7).edge).toBe('var(--project-1)');
  });
});
