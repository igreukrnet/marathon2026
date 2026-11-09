import { Component, EventEmitter, Output, ChangeDetectionStrategy, input } from '@angular/core';
import { TimeSlotComponent } from '../time-slot/time-slot.component';
import { DatePipe } from '@angular/common';
import { TimeRange } from '@app/models/tables/time-range.model';
import { Table } from '@app/models/tables/table.model';

@Component({
  selector: 'app-time-slots-modal',
  imports: [TimeSlotComponent, DatePipe],
  templateUrl: './time-slots-modal.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './time-slots-modal.component.scss',
})
export class TimeSlotsModalComponent {
  readonly locationName = input.required<string>();
  readonly table = input.required<Table>();
  readonly date = input.required<string>();

  @Output() closeModal = new EventEmitter<void>();
  @Output() timeSlotSelected = new EventEmitter<{ table: Table; timeRange: TimeRange }>();

  onClose() {
    this.closeModal.emit();
  }

  onTimeSlotSelected(timeSlot: TimeRange) {
    this.timeSlotSelected.emit({ table: this.table(), timeRange: timeSlot });
  }
}
