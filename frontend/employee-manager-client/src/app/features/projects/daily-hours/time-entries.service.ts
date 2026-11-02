import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../../core/services/api.service';
import { DayTimeEntries, DayTimeEntriesSave } from './time-entry.model';

/** The hours the signed-in employee logs each day on their projects. `date` is `yyyy-MM-dd`. */
@Injectable({ providedIn: 'root' })
export class TimeEntriesService {
  private readonly api = inject(ApiService);

  /** Your hours on the day on each of your projects, with that week's daily totals. */
  getMine(date: string): Observable<DayTimeEntries> {
    return this.api.get<DayTimeEntries>('time-entries/mine', { date });
  }

  /** Replaces your hours on the day and returns the updated day. */
  saveMine(date: string, body: DayTimeEntriesSave): Observable<DayTimeEntries> {
    return this.api.put<DayTimeEntries>(`time-entries/mine?date=${encodeURIComponent(date)}`, body);
  }
}
