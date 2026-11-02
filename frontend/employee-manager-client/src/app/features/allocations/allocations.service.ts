import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../core/services/api.service';
import { MyAllocations, TeamAllocations, TeamAllocationsSave } from './allocation.model';

/** Monthly hours that managers allocate to their direct reports on their projects. `month` is `yyyy-MM`. */
@Injectable({ providedIn: 'root' })
export class AllocationsService {
  private readonly api = inject(ApiService);

  /** The signed-in manager's direct reports and projects with that month's hours. Managers only. */
  getTeam(month: string): Observable<TeamAllocations> {
    return this.api.get<TeamAllocations>('allocations/team', { month });
  }

  /** Replaces the month's allocations on the manager's projects and returns the new grid. */
  saveTeam(month: string, body: TeamAllocationsSave): Observable<TeamAllocations> {
    return this.api.put<TeamAllocations>(
      `allocations/team?month=${encodeURIComponent(month)}`,
      body,
    );
  }

  /** The signed-in employee's own hours that month. */
  getMine(month: string): Observable<MyAllocations> {
    return this.api.get<MyAllocations>('allocations/mine', { month });
  }
}
