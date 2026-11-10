import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { forkJoin, interval, Observable, of, Subscription } from 'rxjs';
import { catchError, exhaustMap, switchMap, tap } from 'rxjs/operators';
import { ParticipantCardComponent } from '@app/components/group-orders/participant-card/participant-card.component';
import { GroupReservationStatus } from '@app/models/group-orders/group-status.model';
import { Participant } from '@app/models/group-orders/participant.model';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { ToastService } from '@app/services/toast/toast.service';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';
import { consolidatePreOrder } from '@app/shared/helpers/consolidate-pre-order';

const SYNC_INTERVAL_MS = 15_000;

@Component({
  selector: 'app-group-order-page',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, ParticipantCardComponent, ModalComponent],
  providers: [DatePipe],
  templateUrl: './group-order-page.component.html',
  styleUrl: './group-order-page.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class GroupOrderPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(GroupOrdersService);
  private readonly toastr = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly datePipe = new DatePipe('en-US');

  private syncSubscription: Subscription | null = null;

  readonly isLoading = signal(true);
  readonly isRefreshing = signal(false);
  readonly isSubmitting = signal(false);
  readonly isRemoving = signal(false);
  readonly error = signal<string | null>(null);
  readonly is404 = signal(false);
  readonly refreshError = signal<string | null>(null);

  readonly invite = signal<GuestInvite | null>(null);
  readonly participants = signal<Participant[]>([]);
  readonly ownerSessionId = signal<string>('');
  readonly groupStatus = signal<GroupReservationStatus>('Open');
  readonly lastUpdatedAt = signal<Date | null>(null);

  readonly showSubmitModal = signal(false);
  readonly guestToRemove = signal<Participant | null>(null);

  readonly consolidated = computed(() => consolidatePreOrder(this.participants()));

  readonly unsubmittedCount = computed(() => {
    return this.participants().filter((person) => !person.isSubmitted).length;
  });

  readonly hasSubmittedDishes = computed(() => {
    return this.consolidated().items.length > 0;
  });

  readonly totalGuestsCount = computed(() => this.participants().length);

  readonly maxCapacity = computed(() => this.invite()?.MaxGuestCount ?? 5);

  readonly isEmpty = computed(() => {
    const list = this.participants();
    if (list.length === 0) {
      return true;
    }
    return list.every((person) => person.items.length === 0);
  });

  private get reservationId(): string {
    return (
      this.route.snapshot.paramMap.get('reservationId') ??
      this.route.parent?.snapshot.paramMap.get('reservationId') ??
      ''
    );
  }

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.stopSync();
    });
  }

  ngOnInit(): void {
    this.loadInitialData();
  }

  loadInitialData(): void {
    const reservationId = this.reservationId;
    this.stopSync();
    this.isLoading.set(true);
    this.error.set(null);
    this.is404.set(false);

    this.service
      .getGroupInvite$(reservationId)
      .pipe(
        switchMap((invite) => {
          this.invite.set(invite);
          const groupId = invite.GroupReservationId;
          return forkJoin({
            ownerSessionId: this.service.getOwnerSessionId$(reservationId),
            sessions: this.service.getSessions$(groupId),
            status: this.service.getStatus$(groupId),
          });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: ({ ownerSessionId, sessions, status }) => {
          this.ownerSessionId.set(ownerSessionId);
          this.participants.set(sessions.participants);
          this.groupStatus.set(status.status);
          this.lastUpdatedAt.set(new Date());
          this.isLoading.set(false);
          this.error.set(null);
          this.is404.set(false);
          if (status.status === 'Open') {
            this.startSync();
          }
        },
        error: (err: HttpErrorResponse) => {
          this.isLoading.set(false);
          if (err.status === 404) {
            this.is404.set(true);
            this.error.set('Group reservation not found');
          } else {
            this.error.set('Failed to load group reservation data');
          }
        },
      });
  }

  startSync(): void {
    this.stopSync();
    if (this.groupStatus() === 'Submitted') {
      return;
    }

    this.syncSubscription = interval(SYNC_INTERVAL_MS)
      .pipe(exhaustMap(() => this.performSync(true)))
      .subscribe();
  }

  stopSync(): void {
    if (this.syncSubscription) {
      this.syncSubscription.unsubscribe();
      this.syncSubscription = null;
    }
  }

  private performSync(silent = true): Observable<unknown> {
    const invite = this.invite();
    const groupId = invite?.GroupReservationId;
    if (!groupId || this.groupStatus() === 'Submitted') {
      return of(null);
    }

    return forkJoin({
      sessions: this.service.getSessions$(groupId, { silent }),
      status: this.service.getStatus$(groupId, { silent }),
    }).pipe(
      tap({
        next: ({ sessions, status }) => {
          this.participants.set(sessions.participants);
          this.groupStatus.set(status.status);
          this.lastUpdatedAt.set(new Date());
          this.refreshError.set(null);
          if (status.status === 'Submitted') {
            this.stopSync();
          }
        },
        error: () => {
          const timeString = this.datePipe.transform(this.lastUpdatedAt(), 'HH:mm:ss') ?? '';
          this.refreshError.set(`Couldn’t refresh. Showing data from ${timeString}`);
        },
      }),
      catchError(() => of(null))
    );
  }

  refreshNow(): void {
    if (this.isRefreshing() || this.groupStatus() === 'Submitted') {
      return;
    }

    this.isRefreshing.set(true);
    this.performSync(true).subscribe({
      next: () => this.isRefreshing.set(false),
      error: () => this.isRefreshing.set(false),
    });
  }

  canRemoveGuest(person: Participant): boolean {
    if (this.groupStatus() === 'Submitted') {
      return false;
    }
    return person.type === 'Guest' && person.id !== this.ownerSessionId();
  }

  openSubmitModal(): void {
    this.showSubmitModal.set(true);
  }

  closeSubmitModal(): void {
    if (this.isSubmitting()) {
      return;
    }
    this.showSubmitModal.set(false);
  }

  confirmSubmit(): void {
    if (this.isSubmitting()) {
      return;
    }

    const groupId = this.invite()?.GroupReservationId;
    const reservationId = this.reservationId;
    if (!groupId || !reservationId) {
      return;
    }

    this.isSubmitting.set(true);
    this.service.submit$(groupId, reservationId).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.showSubmitModal.set(false);
        this.groupStatus.set('Submitted');
        this.stopSync();
        this.toastr.success('Pre-order sent to the restaurant');
      },
      error: () => {
        this.isSubmitting.set(false);
        this.showSubmitModal.set(false);
      },
    });
  }

  openRemoveModal(person: Participant): void {
    this.guestToRemove.set(person);
  }

  closeRemoveModal(): void {
    if (this.isRemoving()) {
      return;
    }
    this.guestToRemove.set(null);
  }

  confirmRemove(): void {
    const guest = this.guestToRemove();
    const groupId = this.invite()?.GroupReservationId;
    if (!guest || !groupId || this.isRemoving()) {
      return;
    }

    this.isRemoving.set(true);
    this.service.removeSession$(groupId, guest.id).subscribe({
      next: (response) => {
        this.isRemoving.set(false);
        this.guestToRemove.set(null);
        this.participants.set(response.participants);
        this.toastr.success(`Removed ${guest.name}`);
      },
      error: () => {
        this.isRemoving.set(false);
        this.guestToRemove.set(null);
      },
    });
  }
}
