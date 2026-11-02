import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../core/services/api.service';
import { Project, ProjectSave, ProjectSummary } from './project.model';

/** Projects that managers run with their direct reports. */
@Injectable({ providedIn: 'root' })
export class ProjectsService {
  private readonly api = inject(ApiService);

  /** The signed-in manager's own projects, sorted by name. Managers only. */
  listManaged(): Observable<ProjectSummary[]> {
    return this.api.get<ProjectSummary[]>('projects/managed');
  }

  /** The projects the signed-in employee is a member of, sorted by name. */
  listMine(): Observable<ProjectSummary[]> {
    return this.api.get<ProjectSummary[]>('projects/mine');
  }

  /** A project its owner or a member can see; 404 for anyone else. */
  get(id: string): Observable<Project> {
    return this.api.get<Project>(`projects/${encodeURIComponent(id)}`);
  }

  create(project: ProjectSave): Observable<Project> {
    return this.api.post<Project>('projects', project);
  }

  update(id: string, project: ProjectSave): Observable<Project> {
    return this.api.put<Project>(`projects/${encodeURIComponent(id)}`, project);
  }

  delete(id: string): Observable<void> {
    return this.api.delete<void>(`projects/${encodeURIComponent(id)}`);
  }
}
