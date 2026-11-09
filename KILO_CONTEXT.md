# marathon2026 — AI Agent Context File

> Load this file at the start of a session to skip project exploration.
> Authoritative detail for the frontend lives in `frontend/restaurant-frontend-app/CLAUDE.md`.

---

## Project at a glance

Restaurant booking & pre-order SPA for three roles: **client**, **waiter**, **admin**.
EPAM training project (marathon2026). Stack: Angular 22 SPA → S3/CloudFront + .NET 10 Lambdas → API Gateway + DynamoDB, all on AWS `eu-north-1`.

```
marathon2026-main/
├── .github/workflows/        pr.yml, deploy.yml, terraform.yml, aqa-quality.yml, aqa-tests.yml
├── aqa/                      .NET 10 + Reqnroll + Playwright QA framework
├── backend/                  .NET 10 Lambda backend (AWS Syndicate)
├── frontend/restaurant-frontend-app/   Angular 22 SPA
├── infra/terraform/          Terraform IaC (all shared AWS resources)
├── signals-migration.md      Signals migration conventions
├── KILO_CONTEXT.md           ← this file
└── README.md                 Deployment order guide
```

---

## Frontend — Angular 22 SPA

**Key facts:**
- Angular **22.1.x**, TypeScript **~6.0.3** (strict), Node `^22.22.3 || ^24.15.0`
- Build: `@angular/build:application` (esbuild), default config = **production**
- Standalone components only — no NgModules
- `ChangeDetectionStrategy.Eager` on all 45 components (migration in progress, do not change individually)
- Zone.js still used (migration to zoneless in progress)
- Path alias: `@app/*` → `src/app/*` (206 existing violations still use `../../` — ESLint warns, not errors)
- Initial bundle: **915.96 kB** / 1 MB hard budget → ~84 kB headroom

**Commands:**
```bash
npm ci                                                    # NOT npm install
npm start                                                 # dev server localhost:4200
BACKEND_URL=https://<api-id>.execute-api.<region>.amazonaws.com/develop npm run build
npm run lint                                              # 216 pre-existing violations
npx tsc -p tsconfig.app.json --noEmit                   # type-check app
npx tsc -p tsconfig.spec.json --noEmit                  # type-check specs
```

**Before any PR — run all 4 gates:**
```bash
npx tsc -p tsconfig.app.json --noEmit
npx tsc -p tsconfig.spec.json --noEmit
BACKEND_URL=https://example.com npm run build            # must end "Bundle check passed"
npm test -- --watch=false
```

**Build hooks:**
- `prebuild` → `scripts/set-env.mjs` reads `$BACKEND_URL`, generates `src/environments/environment.production.ts` (git-ignored, never commit)
- `postbuild` → `scripts/check-bundle.mjs` scans dist for `BACKEND_URL_PLACEHOLDER`, exits 1 if found

**Directory map:**
```
src/app/
  auth/           login, register, guards, interceptor, models, authentication.service
  components/     feature components (cart, dish-modal, place-order-waiter, reservations, tables)
  pages/          route-level screens (main-page, menu-page, reservation-dashboard, waiter-dashboard,
                  reporting-dashboard, waiter-pre-order-page, guest-pre-order-page)
  models/         domain interfaces (never inline in component files)
  services/       HTTP services, one per domain; toast.service.ts for user errors
  shared/         reusable: custom-components, helpers, constants, layouts, config, styles
```

**Where to put new files:**
| What | Location | Naming |
|---|---|---|
| Route-level screen | `pages/<name>-page/` | `<name>-page.component.ts` |
| Feature component (one domain) | `components/<domain>/` | `dish-modal.component.ts` |
| Reusable dumb component | `shared/custom-components/<name>/` | `custom-button.component.ts` |
| HTTP service | `services/<domain>/` | `feedbacks.service.ts` |
| Shared model/interface | `models/<domain>/` | `cart.model.ts` |
| Local interface (one component) | same folder, in `models.ts` | — |
| Pure helpers | `shared/helpers/` | `date-helpers.ts` |

**Key routes:**
| Path | Component | Guard |
|---|---|---|
| `main` | `MainPageComponent` | — |
| `menu` | `MenuPageComponent` | — |
| `tables` | `TablesComponent` | — |
| `reservations` | `ReservationDashboardComponent` | `AuthGuard(authorized:true)` |
| `waiter-reservations` | `WaiterDashboardComponent` | `AuthGuard(authorized:true)` |
| `reports` | `ReportingDashboardComponent` | `AdminGuard` |
| `profile` | `ProfileComponent` | `AuthGuard(authorized:true)` |
| `waiter-pre-order` | `WaiterPreOrderPageComponent` | Lazy |
| `join/:token` | `GuestPreOrderPageComponent` | Lazy |

---

## Signals migration (in progress — `signals-migration.md`)

**Current state:** Migration from `@Input()`/`@Output()` decorators to signal APIs is ongoing.

**Rules for new code:**
- Local mutable state → `signal()`, derived → `computed()`, side effects → `effect()`
- New component inputs → `input()` / `input.required<T>()`, outputs → `output()`
- Signal properties **must be `readonly`** (ESLint `prefer-signals` warns otherwise)
- `viewChild()` / `viewChildren()` instead of `@ViewChild` / `@ViewChildren` decorators
- No new `Subject`/`BehaviorSubject` for UI state — use signals
- HTTP streams stay RxJS; convert to signal at component boundary with `toSignal(obs$, { initialValue })`
- `effect()` must be created in injection context (constructor or field initializer)

**ESLint signal rules (currently `warn`):**
- `@angular-eslint/prefer-signals` — signals over `@Input()`/`@Output()`
- Signal properties not marked `readonly` → warning
- `@ViewChild` decorator instead of `viewChild()` function → warning
- Relative imports crossing >1 directory level → warning (use `@app/*`)

**Trap — `input.required<T>()` vs nullable parent bindings:**
If a parent passes `null`/`undefined` to a `required` input, the Angular compiler catches this at **production build** time (not in `tsc --noEmit`). Make inputs optional (`input<T | null>(null)`) when the value can legitimately be absent.

---

## ESLint key rules (`eslint.config.js`)

| Rule | Severity | Note |
|---|---|---|
| `@angular-eslint/prefer-inject` | error | Use `inject()`, not constructor params |
| `@angular-eslint/prefer-signals` | warn | Signal APIs over decorators |
| `@typescript-eslint/no-restricted-imports` | warn | No `../../**` paths — use `@app/*` |
| `click-events-have-key-events` | error | 50 violations (debt) |
| `interactive-supports-focus` | error | 48 violations (debt) |
| `no-explicit-any` | error | 11 violations (debt) |

Format check and unit tests are **disabled in CI** (TODOs #57 and #90).

---

## CI/CD — GitHub Actions

### `pr.yml` — Frontend lint + type-check + build + deploy

Triggers: push/PR to `main` on `frontend/**` paths. Concurrency: cancel-in-progress.

Steps (in order):
1. `npm ci`
2. `npm run lint` (ESLint)
3. `npx tsc -p tsconfig.app.json --noEmit`
4. `npx tsc -p tsconfig.spec.json --noEmit`
5. `npm run build` with `BACKEND_URL=${{ vars.BACKEND_URL || 'https://6dt85f01cf.execute-api.eu-north-1.amazonaws.com/develop' }}`
6. AWS credentials → `aws s3 sync dist/.../browser s3://$S3_BUCKET/ --delete` (push to main only)
7. CloudFront invalidation `/*`, distribution `E36HD7CJNGHPGW` (push to main only)

Secrets/vars: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `BACKEND_URL` (var), `AWS_REGION` (var, default `eu-north-1`), `S3_BUCKET` (var, default `restaurant-frontend-586047842843`).

**Common failure causes:**
- `Lint` step — ESLint errors/warnings treated as errors
- `Production build` — can be: (a) `set-env.mjs` blocked (no `BACKEND_URL`), (b) Angular template type error caught by `ngc` but not by `tsc --noEmit`, (c) `check-bundle.mjs` found placeholder, (d) budget exceeded (>1 MB)
- Template errors (like required input not bound) only surface at `ng build`, not `tsc --noEmit`

### `deploy.yml` — Backend (.NET Syndicate)

Triggers: push to `main` on `backend/**`. `syndicate update` on push; `syndicate deploy` only via manual dispatch with `deploy: true`.

### `terraform.yml` — Infrastructure

Triggers: PR/push to `main` on `infra/terraform/**`. `terraform plan` → PR comment; `terraform apply` on merge.

### `aqa-tests.yml` — QA tests (nightly 02:00 UTC + manual dispatch)

---

## Backend — .NET 10 Serverless

**Cannot be run locally.** No Docker/LocalStack. All dev targets deployed API Gateway.

- 3 Lambda functions: `api-handler` (REST), `reports-handler` (SQS), `report-sender` (EventBridge scheduled)
- Route registry: `backend/restaurant-backend-app/dnapp/lambdas/api-handler/Routing/RouteRegistry.cs`
- API docs: `backend/restaurant-backend-app/docs/ApiRoutes.md` — **backend wins when frontend model disagrees**
- Auth: Cognito User Pool, `ALLOW_USER_PASSWORD_AUTH`
- Data: DynamoDB (13 tables)
- Deployment via `aws-syndicate` Python tool, region `eu-north-1`

---

## Infrastructure — Terraform (`infra/terraform/`)

- State: S3 `restaurant-terraform-state-{account_id}`, native locking, encrypted
- Region: `eu-north-1`
- Key resources: S3 (4 buckets), CloudFront (SPA, OAC, `PriceClass_100`), Cognito, SQS + DLQ, EventBridge Scheduler, IAM, CloudWatch alarms (7), SNS, Budget ($5/mo)
- Frontend bucket: private, served via CloudFront OAC
- SPA routing: 403/404 from S3 → 200 → `/index.html`

---

## Deployment order

```
1. terraform apply            (infra/terraform/) — shared AWS infra
2. syndicate deploy           (backend/) — Lambdas + API Gateway
3. BACKEND_URL=... npm run build && aws s3 sync  (frontend/) — SPA to S3 + CloudFront invalidation
```

---

## Known debt & traps

**Frontend debt (do not re-measure, trust these numbers):**
- Bundle: 915.96 kB raw / 210 kB transferred. 84 kB to hard error.
- 5 component stylesheets exceed 4 kB style budget (`header.component.scss` worst at 8.15 kB).
- 216 ESLint errors / 1 warning.
- 66 Prettier-unformatted files.
- 11 spec files are CLI stubs; only 1 configures providers correctly.
- 17 npm audit vulnerabilities (12 high) — do not run `npm audit fix` blindly.
- CommonJS bailouts: `dayjs`, `qr-code-styling`.
- Three date libraries installed (`moment`, `dayjs`, `date-fns`) — reuse, do not add a fourth.

**Traps that have already broken the repo:**
1. Wrong import depth (`../` vs `../../`) — verify before pushing
2. `npm install` vs `npm ci` — always `npm ci`
3. Node version — must be `^22.22.3 || ^24.15.0`
4. Dead code not type-checked — `tsconfig.app.json` entry = `src/main.ts`; unreachable files pass silently
5. macOS case-insensitivity — Linux CI is case-sensitive
6. `input.required<T>()` on a component that gets `null` from parent → Angular build fails, `tsc --noEmit` passes
7. Never use text-replace for type renames — use IDE symbol rename

**Working agreements:**
- Branch: `feature/<desc>`, `bugfix/<desc>`, `fix/<desc>`. Never commit to `main` directly.
- Commit: `<semver> short description`, ≤120 chars. Bump version on user-visible changes.
- One logical change per PR. No mixing lint sweeps with behaviour changes.
- Do not commit: `.idea/`, `node_modules/`, `dist/`, `environment.production.ts`.

---

## AQA Framework (`aqa/restaurant-aqa-framework/`)

- .NET 10 + Reqnroll (BDD/Gherkin) + TUnit + Playwright
- Screenplay Pattern: `Actor` with `BrowseTheWeb`, `CallTheApi`, `RememberThings` abilities
- Tags: `@ui` / `@api` required; `@smoke`, `@negative`, `@validation`, `@known-defect`, `@wip`
- State shared via typed `RememberThings` (backed by `ScenarioContext`) — never static fields
- Test data prefixed `AQA-Sample-`, cleaned up after each scenario
- Nightly at 02:00 UTC; also manual dispatch

---

## Recent fixes applied in this session (Nov 9, 2026)

**Commit `6bbb3c6` — lint fixes:**
- `header.component.ts` — added `readonly` to all 7 signal/computed properties
- `feedback-qr-modal.component.ts` — `@ViewChild` → `viewChild.required<ElementRef>()`, access via `()`
- `guest-preorder-submitted.component.ts` — `../../state/guest-preorder.store` → `@app/pages/guest-pre-order-page/state/guest-preorder.store`
- `guest-name-modal.component.ts` — same relative-to-absolute import fix

**Commit `c799870` — production build fix:**
- Root cause: `time-slot.component.ts` had `input.required<TimeRange>()` but `table-card.component.html` used `<app-time-slot>` without `[timeRange]` binding (slots can be `null` for disabled slots). Angular compiler caught this at `ng build`, not `tsc --noEmit`.
- Fix: `timeRange` → `input<TimeRange | null>(null)`; `[timeRange]="slot.timeRange"` added to `table-card.component.html`; null-guards added to `time-slot.component.html`
