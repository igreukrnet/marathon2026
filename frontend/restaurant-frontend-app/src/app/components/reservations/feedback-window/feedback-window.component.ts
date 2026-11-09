import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  HostBinding,
  inject,
  input,
  OnInit,
  Output,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CustomButtonComponent } from '@app/shared/custom-components/custom-button/custom-button.component';
import { RatingComponent } from '@app/shared/custom-components/rating/rating.component';
import { Reservation, ReservationStatus } from '@app/models/reservations/reservation.model';
import { FeedbacksService } from '@app/services/feedbacks/feedbacks.service';
import { CreateFeedbackRequest } from '@app/models/feedbacks/create-feedback-request.model';
import { ReviewType } from '@app/models/enums/review-type.model';

import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { FeedbackReservationResponseItem } from '@app/models/feedbacks/feedback-reservation-response-item.model';
import { ToastService } from '@app/services/toast/toast.service';
import { AuthenticationService } from '@app/auth/services/authentication.service';
import { UsersService } from '@app/services/users/users.service';
import { FeedbackUpdatedEvent } from '@app/models/feedbacks/feedback.model';

@Component({
    selector: 'app-feedback-window',
    imports: [CustomButtonComponent, RatingComponent, FormsModule],
    templateUrl: './feedback-window.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    styleUrl: './feedback-window.component.scss',
    standalone: true
})
export class FeedbackWindowComponent implements OnInit, AfterViewInit {
  readonly customClass = input<string>('');
  readonly selectedReservation = input.required<Reservation>();
  readonly feedbackForUpdate = input<FeedbackReservationResponseItem | undefined>(undefined);
  readonly anonymousToken = input<string>('');
  @Output() closedFeedback = new EventEmitter<void>();

  @Output() feedbackUpdated = new EventEmitter<FeedbackUpdatedEvent>();

  @HostBinding('class.fade-out') isFading = false;

  hovered: 'left' | 'right' | '' = '';
  selectedTab: 'service' | 'cuisine' = 'service';

  elRef = inject(ElementRef);

  serviceRating = 0;
  cuisineRating = 0;

  serviceComment = '';
  cuisineComment = '';
  // for anonymous
  showCuisineForm = true;
  showServiceForm = true;
  isAnonymous = false;

  waiterName = 'Mario Jast';
  waiterProfileImage = '/assets/img/avatar.png';

  feedbacksService = inject(FeedbacksService);
  toastr = inject(ToastService);
  authService = inject(AuthenticationService);
  userService = inject(UsersService);

  // constructor(
  //   private feedbacksService: FeedbacksService,
  //   private toastr: ToastrService
  // ) {}

  ngOnInit(): void {
    const feedbackForUpdate = this.feedbackForUpdate();
    const selectedReservation = this.selectedReservation();
    const isAnon =
      selectedReservation?.customerId === null || selectedReservation?.customerId === '';
    this.isAnonymous = isAnon;

    if (feedbackForUpdate) {
      if (feedbackForUpdate.service) {
        this.serviceRating = feedbackForUpdate.service.rating;
        this.serviceComment = feedbackForUpdate.service.comment;

        if (isAnon) {
          this.showServiceForm = false;
        }
      }

      if (feedbackForUpdate.cuisineExperience) {
        this.cuisineRating = feedbackForUpdate.cuisineExperience.rating;
        this.cuisineComment = feedbackForUpdate.cuisineExperience.comment;

        if (isAnon) {
          this.showCuisineForm = false;
        }
      }

      if (isAnon && feedbackForUpdate.service && feedbackForUpdate.cuisineExperience) {
        this.showServiceForm = false;
        this.showCuisineForm = false;
        this.closedFeedback.emit();
        return;
      }
    }

    if (this.isAnonymous) {
      if (!this.showServiceForm && this.showCuisineForm) {
        this.selectedTab = 'cuisine';
      } else if (!this.showCuisineForm && this.showServiceForm) {
        this.selectedTab = 'service';
      }
    }

    if (selectedReservation && selectedReservation.waiterId) {
      this.authService.getUserNameById$(selectedReservation.waiterId).subscribe({
        next: (name) => {
          this.waiterName = name;
        },
        error: (err) => {
          console.error('Error fetching waiter name:', err);
          this.toastr.error('Failed to load waiter information.');
        },
      });
      this.userService.getUserProfileImage(selectedReservation.waiterId).subscribe({
        next: (response) => {
          this.waiterProfileImage = response.presignedUrl ?? '/assets/img/avatar.png';
        },
        error: (err) => {
          console.error('Error fetching waiter profile image:', err);
          this.toastr.error('Failed to load waiter profile image.');
        },
      });
    }
  }

  // onRatingChange(value: number) {
  //   this.currentRating = value;
  // }

  ngAfterViewInit(): void {
    const wrapper = this.elRef.nativeElement.querySelector('.feedback-wrapper');
    if (wrapper) {
      wrapper.addEventListener('click', (event: Event) => event.stopPropagation());
    }
  }

  onClose(): void {
    this.closedFeedback.emit();
    this.isFading = true;
  }

  onSubmitFeedback(): void {
    const requests: CreateFeedbackRequest[] = [];
    const feedbackForUpdate = this.feedbackForUpdate();
    const selectedReservation = this.selectedReservation();
    const serviceAlreadyExists = feedbackForUpdate?.service != null;
    const cuisineAlreadyExists = feedbackForUpdate?.cuisineExperience != null;

    if (this.serviceRating > 0 && !serviceAlreadyExists) {
      requests.push({
        reservationId: selectedReservation.id,
        userId: selectedReservation.customerId,
        rating: this.serviceRating,
        comment: this.serviceComment?.trim() || undefined,
        type: ReviewType.Service,
      });
    }

    if (this.cuisineRating > 0 && !cuisineAlreadyExists) {
      requests.push({
        reservationId: selectedReservation.id,
        userId: selectedReservation.customerId,
        rating: this.cuisineRating,
        comment: this.cuisineComment?.trim() || undefined,
        type: ReviewType.CuisineExperience,
      });
    }

    const requests$ = requests.map((req) =>
      this.feedbacksService.leaveFeedback(req, this.anonymousToken())
    );

    // Send both requests in parallel
    forkJoin(requests$).subscribe({
      next: () => {
        if (selectedReservation) {
          selectedReservation.status = ReservationStatus.Finished;
        }
        this.feedbackUpdated.emit({
          reservationId: selectedReservation.id,
          feedback: {
            service:
              this.serviceRating > 0
                ? { rating: this.serviceRating, comment: this.serviceComment }
                : null,
            cuisineExperience:
              this.cuisineRating > 0
                ? { rating: this.cuisineRating, comment: this.cuisineComment }
                : null,
          },
        });
        this.closedFeedback.emit();
      },
      error: (err) => {
        console.error('Error submitting feedback:', err);
      },
    });
  }

  onUpdateFeedback(): void {
    const requests: CreateFeedbackRequest[] = [];
    const feedbackForUpdate = this.feedbackForUpdate();
    const selectedReservation = this.selectedReservation();
    const originalService = feedbackForUpdate?.service;
    const originalCuisine = feedbackForUpdate?.cuisineExperience;

    const serviceChanged =
      this.serviceRating > 0 &&
      (originalService?.rating !== this.serviceRating ||
        originalService?.comment !== this.serviceComment);

    const cuisineChanged =
      this.cuisineRating > 0 &&
      (originalCuisine?.rating !== this.cuisineRating ||
        originalCuisine?.comment !== this.cuisineComment);

    if (serviceChanged) {
      requests.push({
        reservationId: selectedReservation.id,
        userId: selectedReservation.customerId,
        rating: this.serviceRating,
        comment: this.serviceComment?.trim() || undefined,
        type: ReviewType.Service,
      });
    }

    if (cuisineChanged) {
      requests.push({
        reservationId: selectedReservation.id,
        userId: selectedReservation.customerId,
        rating: this.cuisineRating,
        comment: this.cuisineComment?.trim() || undefined,
        type: ReviewType.CuisineExperience,
      });
    }
    if (requests.length === 0) {
      this.toastr.info('No changes to update.');
      return;
    }
    const requests$ = requests.map((req) => this.feedbacksService.updateFeedback(req));

    // Send both requests in parallel
    forkJoin(requests$).subscribe({
      next: () => {
        this.feedbackUpdated.emit({
          reservationId: selectedReservation.id,
          feedback: {
            service:
              this.serviceRating > 0
                ? { rating: this.serviceRating, comment: this.serviceComment }
                : null,
            cuisineExperience:
              this.cuisineRating > 0
                ? { rating: this.cuisineRating, comment: this.cuisineComment }
                : null,
          },
        });
        this.closedFeedback.emit();
      },
      error: (err) => {
        console.error('Error updating feedback:', err);
      },
    });
  }

  getFirstName(fullName?: string): string {
    if (!fullName || fullName === 'Anonymous User') {
      return 'Unknown';
    }
    return fullName.split(' ')[0] || 'Unknown';
  }
}
