/** A project column in the team grid: one of the signed-in manager's own projects. */
export interface AllocationProject {
  id: string;
  name: string;
}

/** Hours allocated on one project in the month. */
export interface ProjectHours {
  projectId: string;
  hours: number;
}

/** A direct report's row in the team grid. */
export interface TeamMemberHours {
  employeeId: string;
  firstName: string;
  lastName: string;
  jobTitle: string;
  /** Which of the manager's projects the report is on. */
  projectIds: string[];
  hours: ProjectHours[];
  /** Hours on other managers' projects this month: they count toward the cap but can't be edited here. */
  otherHours: number;
  /** The month's sum of the report's daily time entries on the manager's projects; projects without any are left out. */
  logged: ProjectHours[];
  /** Hours the report logged this month on other managers' projects. */
  otherLoggedHours: number;
}

/** A manager's allocations for their direct reports in one month. */
export interface TeamAllocations {
  /** `yyyy-MM`. */
  month: string;
  /** 8 hours for every weekday in the month. */
  capacityHours: number;
  /** False for past months, which are read-only. */
  isEditable: boolean;
  /** The manager's own projects, sorted by name. */
  projects: AllocationProject[];
  /** The manager's direct reports, sorted by name. */
  members: TeamMemberHours[];
}

export interface AllocationSave {
  employeeId: string;
  projectId: string;
  hours: number;
}

/** Replaces the month's allocations; anything left out is removed. */
export interface TeamAllocationsSave {
  allocations: AllocationSave[];
}

/** One of the signed-in employee's projects and their hours on it. */
export interface MyProjectHours {
  projectId: string;
  projectName: string;
  ownerName: string;
  /** Allocated by their manager for the month. */
  hours: number;
  /** The month's sum of their daily time entries on the project. */
  loggedHours: number;
}

/** The signed-in employee's own hours in one month. */
export interface MyAllocations {
  /** `yyyy-MM`. */
  month: string;
  capacityHours: number;
  projects: MyProjectHours[];
  totalHours: number;
  totalLoggedHours: number;
}
