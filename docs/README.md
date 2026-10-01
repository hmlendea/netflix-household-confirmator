# Repository Documentation

This directory contains implementation-grounded documentation for Netflix Household Confirmator.

## Documents

- [Runtime Behaviour](runtime-behaviour.md) explains startup, polling, message selection, URL extraction, browser interaction, state, and failure semantics.
- [Development And Operations](development-and-operations.md) explains configuration, dependencies, local execution, release, security boundaries, and operational constraints.
- [Testing Model](testing-model.md) maps test suites to production behaviour and records current verification gaps.

The higher-level architecture overview remains in [ARCHITECTURE.md](../ARCHITECTURE.md). User-facing setup and usage remain in [README.md](../README.md). Security reporting remains in [SECURITY.md](../SECURITY.md).

## Reading Order

1. Read [Runtime Behaviour](runtime-behaviour.md) before changing the polling or confirmation path.
2. Read [Testing Model](testing-model.md) before changing production behaviour or its test seams.
3. Read [Development And Operations](development-and-operations.md) before changing configuration, deployment, logging, or release behaviour.

## Evidence Convention

Statements in these documents describe the current implementation and tests. A limitation is intentional documentation of observed behaviour, not a proposed design. Source links identify the owning implementation surface; test links identify executable evidence.
