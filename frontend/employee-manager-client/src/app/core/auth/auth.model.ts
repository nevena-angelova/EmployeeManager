export type Role = 'Employee' | 'Manager';

export interface CurrentUser {
  employeeId: string;
  email: string;
  firstName: string;
  lastName: string;
  jobTitle: string;
  role: Role;
}

export interface LoginResult {
  accessToken: string;
  /** ISO 8601 timestamp. */
  expiresAt: string;
  user: CurrentUser;
}
