import { Component, EventEmitter, Output, ChangeDetectionStrategy, input } from '@angular/core';
import { DishListResponse } from '@app/models/dishes/dish-list-response.model';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-dish-select-card',
  imports: [CommonModule],
  templateUrl: './dish-select-card.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './dish-select-card.component.scss',
})
export class DishSelectCardComponent {
  readonly dish = input.required<DishListResponse>();
  readonly quantity = input<number>(1);
  @Output() quantityChanged = new EventEmitter<number>();
  @Output() remove = new EventEmitter<void>();

  onIncrement() {
    this.quantityChanged.emit(this.quantity() + 1);
  }

  onDecrement() {
    this.quantityChanged.emit(Math.max(1, this.quantity() - 1));
  }

  canGoDown() {
    return this.quantity() > 1;
  }

  onRemove() {
    this.remove.emit();
  }
}
