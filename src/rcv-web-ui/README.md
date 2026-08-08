# RCV Web UI

React and TypeScript frontend for the ranked choice voting platform.

## Current Status

As of 2026-08-08, the Phase 2 SPA is implemented. It includes:

- Google and Microsoft login, protected routes, and logout
- Creator dashboard, poll creation/editing, and close/delete controls
- Accessible drag-and-drop ranking with keyboard and button alternatives
- Vote creation and revision until effective poll closure
- Live and final aggregate results with charts and accessible tables
- Responsive layouts, loading/error states, component tests, and Playwright smoke tests

## Development

```bash
npm ci
npm run dev
```

The development server runs at `http://localhost:5173`.
It proxies `/api` to `http://localhost:5041`; run the API separately.

## Available Commands

```bash
npm run build
npm run lint
npm run typecheck
npm run test
npm run test:coverage
npm run test:e2e
npm run preview
```

Track implementation progress in [`../../tasks/tasks-phase2-web-app.md`](../../tasks/tasks-phase2-web-app.md).
