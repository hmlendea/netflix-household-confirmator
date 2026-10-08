# Netflix Household Confirmator Documentation

## Repository Summary

Netflix Household Confirmator is a self-hosted .NET 10 console application that monitors a configured IMAP inbox for Netflix household update messages and uses Selenium-backed browser automation to confirm each detected request. The application runs as a single long-running process with no inbound API, database, or distributed coordination.

## Root Document Map

- [ARCHITECTURE.md](../ARCHITECTURE.md) — High-level architecture overview
- [SECURITY.md](../SECURITY.md) — Security policy and vulnerability reporting
- [PRIVACY.md](../PRIVACY.md) — Privacy and personal data handling
- [ROADMAP.md](../ROADMAP.md) — Project roadmap (if present)
- [LICENSE](../LICENSE) — License terms

## Documentation Catalogue

### Architecture & Design
- [architecture.md](./architecture.md) — Detailed architecture complementing root ARCHITECTURE.md
- [repository-structure.md](./repository-structure.md) — Source tree layout and module organisation
- [repository-overview.md](./repository-overview.md) — Repository purpose, scope, and entry points

### Components
- [components/presentation.md](./components/presentation.md) — Console host and composition root
- [components/host-and-composition.md](./components/host-and-composition.md) — DI container, service registration, WebDriver lifecycle
- [components/application-services.md](./components/application-services.md) — Orchestration and processor services
- [components/integration-models.md](./components/integration-models.md) — IMAP and browser automation adapters

### Execution Flows
- [flows/startup-and-rendering.md](./flows/startup-and-rendering.md) — Process startup, configuration binding, WebDriver initialisation
- [flows/household-confirmation.md](./flows/household-confirmation.md) — End-to-end mailbox-to-browser confirmation flow

### Behaviour
- [behaviour/email-processing.md](./behaviour/email-processing.md) — IMAP message retrieval, filtering, URL extraction
- [behaviour/browser-confirmation.md](./behaviour/browser-confirmation.md) — Netflix page navigation and interaction

### Reference
- [api-reference/INDEX.md](./api-reference/INDEX.md) — API reference index (internal interfaces)
- [configuration.md](./configuration.md) — Configuration schema, sources, precedence
- [data-model.md](./data-model.md) — Domain entities, relationships, persistence
- [dependencies.md](./dependencies.md) — External and internal dependencies
- [logging.md](./logging.md) — Logging framework, levels, structured fields, correlation, sinks
- [error-handling.md](./error-handling.md) — Error taxonomy, handling patterns, recovery
- [concurrency-and-scheduling.md](./concurrency-and-scheduling.md) — Threading, async, schedulers, locks
- [state-and-persistence.md](./state-and-persistence.md) — State management, stores, caches, migrations
- [integrations.md](./integrations.md) — External system integrations (IMAP, Netflix, Selenium)
- [security.md](./security.md) — Security model, threats, mitigations (complements root SECURITY.md)
- [testing.md](./testing.md) — Test strategy, organisation, coverage
- [build-and-deployment.md](./build-and-deployment.md) — Build pipeline, deployment, environments
- [change-guide.md](./change-guide.md) — How to modify common areas safely
- [troubleshooting.md](./troubleshooting.md) — Common issues and solutions
- [faq.md](./faq.md) — Frequently asked questions
- [ambiguities-and-open-questions.md](./ambiguities-and-open-questions.md) — Unresolved items, TODOs, known gaps
- [documentation-maintenance.md](./documentation-maintenance.md) — How to keep docs current, ownership

### Quick Start
- [quick-start.md](./quick-start.md) — Getting started guide for newcomers

## Navigation Aids

- **Start here** (newcomers): [quick-start.md](./quick-start.md) → [repository-overview.md](./repository-overview.md) → [runtime-behaviour.md](../docs/runtime-behaviour.md)
- **Deep dive** (component owners): [components/](./components/) → [flows/](./flows/) → [behaviour/](./behaviour/)
- **Flows** (debuggers): [flows/household-confirmation.md](./flows/household-confirmation.md) → [behaviour/email-processing.md](./behaviour/email-processing.md) → [behaviour/browser-confirmation.md](./behaviour/browser-confirmation.md)

## Maintenance Metadata

- Last reviewed: 2026-10-08
- Owner: Repository maintainers
- Coverage status: Initial comprehensive documentation generated from implementation and test evidence