/** A project in a list (`GET /api/projects/managed` and `/mine`). */
export interface ProjectSummary {
  id: string;
  name: string;
  description: string | null;
  ownerId: string;
  ownerName: string;
  memberCount: number;
}

export interface ProjectMember {
  employeeId: string;
  firstName: string;
  lastName: string;
  jobTitle: string;
  /** False once the member has moved to another manager; they stay until the owner removes them. */
  isDirectReport: boolean;
}

export interface Project {
  id: string;
  name: string;
  description: string | null;
  ownerId: string;
  ownerName: string;
  members: ProjectMember[];
}

/** Body of `POST /api/projects` and `PUT /api/projects/{id}`; the PUT replaces the members. */
export interface ProjectSave {
  name: string;
  description: string | null;
  memberIds: string[];
}
