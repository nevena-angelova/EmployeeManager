import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../core/services/api.service';
import { OrgChartNode } from './org-chart.model';

@Injectable({ providedIn: 'root' })
export class OrgChartService {
  private readonly api = inject(ApiService);

  /** Returns the reporting tree from all top-level managers, or from `rootId` if given. */
  getOrgChart(rootId?: string): Observable<OrgChartNode[]> {
    return this.api.get<OrgChartNode[]>('employees/org-chart', rootId ? { rootId } : undefined);
  }
}
