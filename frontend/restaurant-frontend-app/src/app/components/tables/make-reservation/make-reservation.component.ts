import {
  Component,
  EventEmitter,
  Output,
  ChangeDetectionStrategy,
  inject,
  input,
  model,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { CustomButtonComponent } from '@app/shared/custom-components/custom-button/custom-button.component';
import { CustomNumberComponent } from '@app/shared/custom-components/custom-number/custom-number.component';
import { CustomSelectComponent } from '@app/shared/custom-components/custom-select/custom-select.component';
import { Table } from '@app/models/tables/table.model';
import { TimeRange } from '@app/models/tables/time-range.model';
import { formatTime } from '@app/shared/helpers/date-helpers';
import { ToastService } from '@app/services/toast/toast.service';

@Component({
  selector: 'app-make-reservation',
  imports: [
    DatePipe,
    CustomButtonComponent,
    CustomNumberComponent,
    CustomSelectComponent,
  ],
  templateUrl: './make-reservation.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './make-reservation.component.scss',
})
export class MakeReservationComponent {
  readonly title = input<string>('Make a Reservation');
  readonly locationName = input.required<string>();
  readonly table = input.required<Table>();
  readonly selectedTimeRange = model.required<TimeRange>();
  readonly guests = model<number>(1);

  private toast = inject(ToastService);

  @Output() closeModal = new EventEmitter<void>();
  @Output() makeReservation = new EventEmitter<{
    tableId: string;
    timeRange: TimeRange;
    guests: number;
  }>();

  onMakeReservation() {
    this.makeReservation.emit({
      tableId: this.table().id,
      timeRange: this.selectedTimeRange(),
      guests: this.guests(),
    });
    this.toast.success('Reservation created successfully.', 'Success');
  }

  onClose() {
    this.closeModal.emit();
  }

  onGuestsChange(guests: number | undefined): void {
    if (guests === undefined) {
      return;
    }
    this.guests.set(guests);
  }

  startTimeOptions() {
    return this.table().timeRanges.map((range) => formatTime(range.start));
  }

  selectStartTime() {
    if (this.selectedTimeRange()) {
      return formatTime(this.selectedTimeRange().start);
    }
    return '';
  }

  selectEndTime() {
    if (this.selectedTimeRange()) {
      return formatTime(this.selectedTimeRange().end);
    }
    return '';
  }

  endTimeOptions() {
    return this.table().timeRanges.map((range) => formatTime(range.end));
  }

  onStartTimeChange(startTime: string) {
    const range = this.table().timeRanges.find((r) => formatTime(r.start) === startTime);
    if (range) {
      this.selectedTimeRange.set(range);
    }
  }

  onEndTimeChange(endTime: string) {
    const range = this.table().timeRanges.find((r) => formatTime(r.end) === endTime);
    if (range) {
      this.selectedTimeRange.set(range);
    }
  }
}
