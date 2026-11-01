import { TeamMember, lineColour, teamLines } from './team-lines';

function person(id: string, managerId: string | null, firstName = id, lastName = ''): TeamMember {
  return { id, managerId, firstName, lastName };
}

describe('teamLines', () => {
  it('numbers team leads by name and puts their whole team on their line', () => {
    const lines = teamLines([
      person('ceo', null),
      person('zoe', 'ceo', 'Zoe'),
      person('adam', 'ceo', 'adam'),
      person('dev', 'zoe'),
      person('intern', 'dev'),
    ]);

    expect(lines.get('ceo')).toBeUndefined();
    expect(lines.get('adam')).toBe(0);
    expect(lines.get('zoe')).toBe(1);
    expect(lines.get('dev')).toBe(1);
    expect(lines.get('intern')).toBe(1);
  });

  it('breaks ties on last name and repeats colours after the last one', () => {
    const leads = ['F', 'E', 'D', 'C', 'B', 'A'].map((last) => person(last, 'ceo', 'Sam', last));
    const lines = teamLines([person('ceo', null), ...leads]);

    expect(['A', 'B', 'C', 'D', 'E', 'F'].map((id) => lines.get(id))).toEqual([0, 1, 2, 3, 4, 0]);
  });

  it('numbers each top-level manager’s teams separately', () => {
    const lines = teamLines([
      person('one', null),
      person('two', null),
      person('a', 'one'),
      person('b', 'two'),
    ]);

    expect(lines.get('a')).toBe(0);
    expect(lines.get('b')).toBe(0);
  });

  it('leaves out people in a manager cycle', () => {
    const lines = teamLines([person('a', 'b'), person('b', 'a')]);

    expect(lines.size).toBe(0);
  });
});

describe('lineColour', () => {
  it('returns the line’s custom property, or null', () => {
    const lines = new Map([['a', 3]]);

    expect(lineColour(lines, 'a')).toBe('var(--line-3)');
    expect(lineColour(lines, 'b')).toBeNull();
  });
});
