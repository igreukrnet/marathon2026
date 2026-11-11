# Restaurant AQA framework

## Обраний напрям та підхід (Homework Assignment)

- **Напрям:** **Напрям 1. Груповий pre-order** (Рівні складності 1, 2 та 3).
- **Підхід / Архітектура:** **BDD (Reqnroll / Gherkin) + Screenplay Pattern** (Actor, Abilities, Tasks, Questions, Memory) + **TUnit** + **Playwright for .NET**.
- **Піраміда тестів:**
  - **UI-тести (`@ui`):** Наскрізні сценарії користувача на сторінці огляду групового замовлення (`/reservations/:id/group-order`): перегляд учасників та динамічний перерахунок суми, видалення гостя через модальне діалогове вікно, підтвердження консолідованого замовлення, відображення замовлення у інтерфейсі ресторану/офіціанта.
  - **API-тести (`@api`):** Авторизація, валідація бізнес-правил, крайові випадки (не-Owner, неавторизований, неіснуюча група, чужий гість, повторний submit) та підготовка тестових даних (бронювання, гості, кошики).

### Зафіксована поведінка для невизначених специфікацією випадків

| Випадок | Статус код | Очікувана поведінка / Повідомлення |
|---|---|---|
| Група без учасників | `200 OK` | `participants: []`, `total: 0` |
| Перегляд сесій не від Owner (інший клієнт) | `403 Forbidden` | `Only the reservation owner can perform this action.` |
| Перегляд або дії без авторизації | `401 Unauthorized` | Відмова в доступі |
| Неіснуюча група або бронювання | `404 Not Found` | Повідомлення про відсутність ресурсу |
| Підтвердження без підтверджених кошиків | `400 Bad Request` | `No submitted guest sessions found.` |
| Підтвердження кошиків без страв | `400 Bad Request` | `No dishes found in submitted guest sessions.` |
| Повторний submit уже надісланої групи | `400 Bad Request` | Заборона повторного submit |
| Submit не від Owner | `403 Forbidden` | `Only the reservation owner can perform this action.` |
| Видалення гостя з іншої групи | `400 Bad Request` | `Guest session does not belong to this group reservation.` |
| Видалення неіснуючого гостя | `404 Not Found` | `Guest session with id ... not found.` |
| Видалення гостя не від Owner | `403 Forbidden` | `Only the reservation owner can perform this action.` |
| Видалення останнього гостя | `200 OK` | Група стає порожньою (`participants: []`, `total: 0`) |

---

## Run the tests

```bash
cp .env.example .env        # then fill in your own values (see "Configuration")
dotnet build
dotnet run --no-build -- --report-trx                                      # everything
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=level1]"     # Level 1: Review
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=level2]"     # Level 2: Confirmation
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=level3]"     # Level 3: Removal
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=ui]"         # UI scenarios only
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=api]"        # API scenarios only
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=smoke]"      # Smoke tests only
```

Everything a run produces lands in `TestResults/`:
- **TUnit Interactive Report:** `TestResults/*-report.html` (interactive web view) and `.trx`
- **Reqnroll Report:** `TestResults/reqnroll/`
- **Playwright Traces:** `TestResults/traces/*.zip` (open with `npx playwright show-trace <file>.zip`)

---

## Reporting & SDLC (Завдання з зірочкою)

### 1. Report Portal
- **Локальний запуск:** `docker compose -f docker-compose.reportportal.yml up -d` (доступно на `http://localhost:8080`).
- **Конфігурація:** `ReportPortal.config.json` підтримує змінні оточення `RP_SERVER_URL`, `RP_API_KEY`, `RP_PROJECT`, `RP_LAUNCH`.

### 2. AWS CodeBuild Pipeline
- **Специфікація:** `buildspec.yml` запускає тести у хмарі AWS CodeBuild.
- **Вбудовані звіти:** TRX автоматично публікується у вкладку "Reports" AWS CodeBuild.
- **Артефакти:** HTML-звіти та traces зберігаються у бакеті Amazon S3.

---

Some scenarios are expected to fail (`@known-defect`, bugs in the application) or are skipped (`@wip`, no admin account configured). That is intentional.

## Quality gates

| Where | What runs |
|---|---|
| Git hooks (local) | `pre-commit`: `dotnet format` check and Gherkin lint on staged files · `commit-msg`: Conventional Commits · `pre-push`: build with analyzers |
| `aqa-quality.yml` | on every push and pull request: no generated files tracked, format, build (warnings are errors), Gherkin lint, test discovery |
| `aqa-tests.yml` | the scenarios themselves, nightly and on demand (Actions → AQA Tests → Run workflow) |

Enable the hooks once per clone, from the repository root:

```bash
dotnet tool restore --tool-manifest aqa/restaurant-aqa-framework/.config/dotnet-tools.json
git config core.hooksPath aqa/restaurant-aqa-framework/.husky
```

The hooks only act on commits and pushes that touch `aqa/restaurant-aqa-framework`. Gherkin lint needs Node.js.

## Configuration

There is no shared environment and no committed `.env`: copy `.env.example` to `.env` and fill it in with the data of **your own** deployment (see "Testing your own deployment"). `.env` is git-ignored; never commit it.

Settings come from `.env` or real environment variables (variables win):

| Setting | Meaning |
|---|---|
| `UI_URL` | where the frontend is served |
| `API_URL` | base URL of the backend API, including the stage (for example `…/develop`) |
| `WAITER_USERNAME`, `WAITER_PASSWORD` | a waiter account that already exists in the environment under test |
| `TESTTABLE_ID`, `TESTTABLE_WAITER_*` | a table and the waiter assigned to it (only for scenarios that need them) |
| `ADMIN_USERNAME`, `ADMIN_PASSWORD` | an admin account; without it the admin scenarios are skipped |

The full list, with defaults, is in `.env.example`.

## Testing your own deployment

### If you deployed the backend / frontend / DevOps homework

1. `cp .env.example .env` and set `UI_URL` and `API_URL` to your deployment (API URL includes the stage, for example `…/develop`).
2. Waiters cannot be created by signing up, so create a waiter (and, for the admin scenarios, an admin) in *your* environment and put its credentials in `WAITER_*` / `ADMIN_*`. Optionally add a table and its assigned waiter (`TESTTABLE_*`).
3. Run `dotnet build` and a small slice first (`--treenode-filter "/*/*/*/*[Category=smoke]"`).

### If you have not deployed your own homework

If you deployed the original application, test that: set `UI_URL` / `API_URL` as above. Otherwise point the framework at a comparable public site (Booking, Airbnb, OpenTable, …) and write all the layers for it yourself: Gherkin features, step definitions, Screenplay tasks and questions, page objects with locators, and API clients where the site has a usable API. Use only flows you are allowed to automate (no purchases, no real bookings, respect the site's terms), and set `UI_URL` to that site.

1. **Generate a skeleton with Playwright codegen** (see "Presetup instructions"): record one flow, for example search → open a result → check details. Save the generated code to a scratch file, it is only raw material.
2. **Split it across the layers**: locators and small actions go to `Ui/` page objects, the flow becomes a task in `Screenplay/Tasks/`, the read-backs become questions in `Screenplay/Questions/`, the business wording goes to `Features/` and thin glue to `StepDefinitions/`.
3. **Replace weak locators.** Codegen often emits positional or text-fragile selectors. Prefer role + accessible name, label, test id, then CSS (see [CLAUDE.md](CLAUDE.md)); check them in the Playwright inspector.
4. Tag scenarios `@ui` / `@api`, keep them independent, and run `npx gherkin-lint -c .gherkin-lintrc "Features/*.feature"`.

### CI on your own public repository

Publish your work in your own public Git repository (the link is the deliverable). Workflows are disabled in a fresh fork until you enable them. Add the same values under Settings → Secrets and variables → Actions:

- **Variables:** `AQA_UI_URL`, `AQA_API_URL`, `AQA_TESTTABLE_ID`
- **Secrets:** `AQA_WAITER_USERNAME`, `AQA_WAITER_PASSWORD`, `AQA_ADMIN_USERNAME`, `AQA_ADMIN_PASSWORD`, `AQA_TESTTABLE_WAITER_USERNAME`, `AQA_TESTTABLE_WAITER_PASSWORD`

`aqa-quality.yml` runs on every push and pull request; `aqa-tests.yml` (nightly and on demand) tests whatever those variables point at. Never put credentials in the repository, in `.env.example` or in test code.

## Presetup instructions

This framework contains PlaywriteInstaller script so you don't need to setup it manually.
For full BDD highlighting in your IDE you may need to install Reqnroll extension https://docs.reqnroll.net/latest/ide-integrations/index.html
To start the Playwright code generator open Powershell in the framework directory and enter the line:
```bash
bin/Debug/net10.0/playwright.ps1 codegen "<your UI_URL or the public site you test>"
```
Build first so `playwright.ps1` exists. The URL is optional.
