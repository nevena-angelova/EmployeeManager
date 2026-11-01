/** How many team colours `styles.scss` defines (`--line-0` … `--line-4`). */
export const LINE_COUNT = 5;

/** The fields needed to place someone on a team line. */
export interface TeamMember {
  id: string;
  managerId: string | null;
  firstName: string;
  lastName: string;
}

/**
 * Gives every team its own colour, like lines on a transit map. A team is everyone under one person who
 * reports to a top-level manager. Leads are numbered in org-chart order (first name, then last name) under
 * each top-level manager, so the chart and the employee table agree. Top-level managers, and anyone stuck
 * in a manager cycle, aren't on a line.
 */
export function teamLines(people: readonly TeamMember[]): Map<string, number> {
  const byId = new Map(people.map((person) => [person.id, person]));

  const leadsByTopManager = new Map<string, TeamMember[]>();
  for (const person of people) {
    const manager = person.managerId ? byId.get(person.managerId) : undefined;
    if (manager && manager.managerId === null) {
      leadsByTopManager.set(manager.id, [...(leadsByTopManager.get(manager.id) ?? []), person]);
    }
  }

  const leadLines = new Map<string, number>();
  for (const leads of leadsByTopManager.values()) {
    leads.sort(byName).forEach((lead, index) => leadLines.set(lead.id, index % LINE_COUNT));
  }

  const lines = new Map<string, number>();
  for (const person of people) {
    // Walk up to the team lead; `seen` stops the walk on a manager cycle.
    const seen = new Set<string>();
    let current: TeamMember | undefined = person;
    while (current && !leadLines.has(current.id) && !seen.has(current.id)) {
      seen.add(current.id);
      current = current.managerId ? byId.get(current.managerId) : undefined;
    }
    const line = current ? leadLines.get(current.id) : undefined;
    if (line !== undefined) {
      lines.set(person.id, line);
    }
  }
  return lines;
}

/** The CSS colour for someone's line, or null when they aren't on one. */
export function lineColour(lines: ReadonlyMap<string, number>, id: string): string | null {
  const line = lines.get(id);
  return line === undefined ? null : `var(--line-${line})`;
}

// Matches the API's org-chart order (ordinal, ignoring case).
function byName(a: TeamMember, b: TeamMember): number {
  return compare(a.firstName, b.firstName) || compare(a.lastName, b.lastName);
}

function compare(a: string, b: string): number {
  const [x, y] = [a.toUpperCase(), b.toUpperCase()];
  return x < y ? -1 : x > y ? 1 : 0;
}
