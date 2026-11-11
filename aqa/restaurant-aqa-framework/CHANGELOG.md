# Changelog

All notable changes to the Restaurant AQA framework will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-11-11

### Added
- **Direction 1: Group Pre-Order Test Automation**:
  - **Level 1 (Review)**: E2E and API scenarios for owner review of joined participants, dishes, quantities, and dynamic total recalculation.
  - **Level 2 (Confirmation)**: Consolidated pre-order submission, dish aggregation, and restaurant/waiter view verification.
  - **Level 3 (Removal)**: Guest removal with total recalculation, cross-group security checks, and end-to-end flow.
- **Screenplay Layer**:
  - Tasks: `SetUpGroupReservation`, `JoinGroupAsGuest`, `AddDishToGuestCart`, `SubmitGuestCart`, `OpenGroupOrderPage`, `SubmitGroupPreOrder`, `RemoveGuestFromGroup`, `ViewSessionsForGroup`, `RegisterAndSignInCustomer`.
  - Questions: `TheGroupParticipants`, `TheGroupGrandTotal`, `TheGroupStatus`, `TheConsolidatedOrder`.
- **UI Page Objects**:
  - `GroupOrderPage` and `ParticipantCardComponent`.
- **API Clients & DTOs**:
  - `GroupReservationsApi`, `GuestSessionsApi`, and full DTO models for group reservations and sessions.
- **Reporting & SDLC (Bonus)**:
  - ReportPortal integration: `ReportPortal.config.json` and `docker-compose.reportportal.yml`.
  - AWS CodeBuild integration: `buildspec.yml` with test reporting and S3 artifact publishing.
