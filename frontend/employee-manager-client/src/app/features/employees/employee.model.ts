import { Role } from '../../core/auth/auth.model';

export interface Employee {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  jobTitle: string;
  /** `YYYY-MM-DD`. */
  hireDate: string;
  managerId: string | null;
  managerName: string | null;
  role: Role;
}

/** Body of `PUT /api/employees/{id}`. */
export interface EmployeeUpdate {
  firstName: string;
  lastName: string;
  email: string;
  jobTitle: string;
  /** `YYYY-MM-DD`. */
  hireDate: string;
  managerId: string | null;
  role: Role;
}

/** Body of `POST /api/employees`: also sets the new account's password. */
export interface EmployeeCreate extends EmployeeUpdate {
  password: string;
}
