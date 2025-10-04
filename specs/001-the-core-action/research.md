# Phase 0 Research — Core GitHub Action Issue Context Retrieval

## Microsoft Agent Framework Integration in GitHub Actions
- **Decision**: Host the Microsoft Agent Framework runtime inside a dedicated `IssueAgent.Agent` project compiled as a self-contained AOT binary.
- **Rationale**: Aligns with constitutional mandate for the Microsoft Agent Framework while enabling predictable startup inside the action container.
- **Alternatives Considered**:
  - Standard ASP.NET Worker Service → rejected due to slower cold starts and heavier dependency graph.
  - Minimal console harness without the framework → rejected because it would violate the mandated orchestration stack.

## GitHub GraphQL Client Selection
- **Decision**: Use `Octokit.GraphQL` with generated AOT stubs to execute the issue-context query.
- **Rationale**: Official GitHub SDK with first-class GraphQL support, token management, and compatibility with trimming/AOT scenarios.
- **Alternatives Considered**:
  - Raw `HttpClient` with manual GraphQL payloads → more boilerplate and riskier maintenance.
  - `GraphQL.Client` community package → slower release cadence and unclear AOT support.

## Container Base Image and Size Constraints
- **Decision**: Publish the action’s Docker image from `mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine` with ready-to-run binaries and file trimming.
- **Rationale**: Alpine-based runtime-deps image balances footprint (<150 MB compressed) with Microsoft support for AOT workloads.
- **Alternatives Considered**:
  - Distroless images → excellent size but limited debugging tools for early iterations.
  - Debian-based runtime-deps → larger image size with no immediate performance benefit.

## Authentication & Permissions Strategy
- **Decision**: Authenticate using the workflow-provided `GITHUB_TOKEN` with minimal scopes (read-only issues).
- **Rationale**: Satisfies security-first principle by avoiding extra secrets and ensuring least-privilege access for GraphQL queries.
- **Alternatives Considered**:
  - Personal Access Tokens → unnecessary secret management burden.
  - GitHub App tokens → overkill for read-only issue-context retrieval.

- **Decision**: Capture and emit cold-start timing in workflow logs using simple stopwatch instrumentation without enforcing a hard SLA this release.
- **Rationale**: Constitution expects visibility into startup latency; telemetry enables future optimization without blocking current MVP and ensures issue context is available quickly.
- **Alternatives Considered**:
  - Enforcing sub-3-second gate immediately → risk of blocking release before baseline instrumentation exists.
  - Ignoring cold-start measurement → would violate performance-readiness expectations.
