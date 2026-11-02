/** How many project colours `styles.scss` defines (`--project-0` … `--project-5`). */
export const PROJECT_COLOUR_COUNT = 6;

/** The CSS colour pair for the project at `index` in a list, repeating after the palette runs out. */
export function projectColour(index: number): { edge: string; tint: string } {
  const n = ((index % PROJECT_COLOUR_COUNT) + PROJECT_COLOUR_COUNT) % PROJECT_COLOUR_COUNT;
  return { edge: `var(--project-${n})`, tint: `var(--project-${n}-tint)` };
}

/** Hours on one project, to draw as a coloured segment. */
export interface RailPart {
  key: string;
  label: string;
  hours: number;
  colour: string;
}

export interface RailSegment extends RailPart {
  /** Share of the rail's width, 0–100. */
  percent: number;
}

/** A month's hours as a bar: project segments, then hours elsewhere, then free time or overflow. */
export interface HourRail {
  segments: RailSegment[];
  elsewherePercent: number;
  freePercent: number;
  /** Where the capacity mark sits, 0–100: 100 unless the hours go over. */
  capacityPercent: number;
  totalHours: number;
  capacityHours: number;
  isOver: boolean;
  /** A text version of the bar, for screen readers. */
  description: string;
}

/**
 * Lays out a month's hours against capacity. Under capacity the rail is the capacity wide; over it, the
 * rail stretches to the total so the overflow shows past the capacity mark.
 */
export function hourRail(
  parts: readonly RailPart[],
  otherHours: number,
  capacityHours: number,
): HourRail {
  const filled = parts.filter((part) => part.hours > 0);
  const elsewhere = Math.max(otherHours, 0);
  const totalHours = filled.reduce((total, part) => total + part.hours, 0) + elsewhere;
  const capacity = Math.max(capacityHours, 0);
  const scale = Math.max(totalHours, capacity);
  const percent = (hours: number) => (scale > 0 ? (hours / scale) * 100 : 0);

  const described = filled.map((part) => `${part.label} ${round(part.hours)}`);
  if (elsewhere > 0) {
    described.push(`elsewhere ${round(elsewhere)}`);
  }
  const summary = `${round(totalHours)} of ${round(capacity)} hours`;

  return {
    segments: filled.map((part) => ({ ...part, percent: percent(part.hours) })),
    elsewherePercent: percent(elsewhere),
    freePercent: percent(Math.max(capacity - totalHours, 0)),
    capacityPercent: scale > 0 ? percent(capacity) : 100,
    totalHours,
    capacityHours: capacity,
    isOver: totalHours > capacity,
    description: described.length ? `${summary}: ${described.join(', ')}` : summary,
  };
}

function round(hours: number): string {
  return String(Math.round(hours * 10) / 10);
}
