/** The signed-in employee's hours on one of their projects on a day. */
export interface DayProjectHours {
  projectId: string;
  projectName: string;
  ownerName: string;
  hours: number;
}

/** A day's total, for the week strip. */
export interface DayTotal {
  /** `yyyy-MM-dd`. */
  date: string;
  hours: number;
}

/** `GET /api/time-entries/mine?date=`: the signed-in employee's hours on one day. */
export interface DayTimeEntries {
  /** `yyyy-MM-dd`. */
  date: string;
  /** False for days after today, which can't have hours yet. */
  isEditable: boolean;
  /** The most hours a day can hold (24). */
  maxHours: number;
  /** The projects they're on, plus any they still have hours on that day, sorted by name. */
  projects: DayProjectHours[];
  totalHours: number;
  /** Monday to Sunday of the day's week. */
  week: DayTotal[];
}

export interface TimeEntrySave {
  projectId: string;
  hours: number;
}

/** Replaces the day's hours; projects left out are removed. */
export interface DayTimeEntriesSave {
  entries: TimeEntrySave[];
}
