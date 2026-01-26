# RCV Platform Constitution

## Core Principles

### I. Library-First Architecture
Every feature starts with the core library (Rcv.Core NuGet package). The library must be:
- Self-contained and stateless (no DB, UI, or user management logic)
- Independently testable with comprehensive test coverage (≥90%)
- Well-documented with XML comments for all public APIs
- Reusable across multiple integration types (Web, Slack, Teams, other .NET apps)

### II. Test-First Development
TDD is mandatory for all code:
- Write tests first, get user approval, then implement
- Red-Green-Refactor cycle strictly enforced
- No implementation without failing tests first
- All edge cases must have corresponding tests

### III. Clear Separation of Concerns
Strict boundaries between layers:
- **Core Library**: Pure voting logic, calculations, and statistics only
- **Application Layer**: Poll management, user authentication, integrations
- **Data Layer**: Persistence via Entity Framework Core and Azure SQL Database
- No cross-layer pollution (e.g., no database logic in the core library)

### IV. Simplicity & YAGNI
Start simple and avoid premature complexity:
- Build only what's needed for current requirements
- Prefer clear, readable code over clever optimizations
- Add complexity only when justified by real user needs
- Follow SOLID principles for maintainability

### V. API-First Design
All functionality exposed through well-defined, immutable APIs:
- Use records and readonly collections for data models
- Validate all inputs at construction time
- Throw descriptive exceptions for invalid states
- Support JSON serialization for all models (`System.Text.Json`)

## Governance

This constitution supersedes all other development practices. All PRs and code reviews must verify compliance with these principles.

Complexity must be justified by real requirements from tasks/prd-v1.md. When in doubt, prefer simplicity.

For runtime development guidance, refer to CLAUDE.md.

Amendments must bump the version and dates below.

**Version**: 1.0.0 | **Ratified**: 2025-10-26 | **Last Amended**: 2025-10-26
