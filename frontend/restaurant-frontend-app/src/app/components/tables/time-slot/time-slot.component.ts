import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TimeRange } from '@app/models/tables/time-range.model';
import { formatTime } from '@app/shared/helpers/date-helpers';

@Component({
  selector: 'app-time-slot',
  templateUrl: './time-slot.component.html',
  standalone: true,
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './time-slot.component.scss',
})
export class TimeSlotComponent {
  readonly timeRange = input<TimeRange | null>(null);
  readonly label = input<string | undefined>(undefined);
  readonly disabled = input<boolean>(false);
  readonly showDate = input<boolean>(false);

  formatTimeRange(start: string): string {
    return `${formatTime(start)}`;
  }
}
