
# Implementation Plan: Core GitHub Action with Minimal AOT .NET Agent

**Branch**: `001-the-core-action` | **Date**: October 4, 2025 | **Spec**: [/workspaces/issueagent/specs/001-the-core-action/spec.md](spec.md)
**Input**: Feature specification from `/specs/001-the-core-action/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path
   → If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detect Project Type from file system structure or context (web=frontend+backend, mobile=app+api)
   → Set Structure Decision based on project type
3. Fill the Constitution Check section based on the content of the constitution document.
4. Evaluate Constitution Check section below
   → If violations exist: Document in Complexity Tracking
   → If no justification possible: ERROR "Simplify approach first"
   → Update Progress Tracking: Initial Constitution Check
5. Execute Phase 0 → research.md
   → If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, agent-specific template file (e.g., `CLAUDE.md` for Claude Code, `.github/copilot-instructions.md` for GitHub Copilot, `GEMINI.md` for Gemini CLI, `QWEN.md` for Qwen Code, or `AGENTS.md` for all other agents).
7. Re-evaluate Constitution Check section
   → If new violations: Refactor design, return to Phase 1
   → Update Progress Tracking: Post-Design Constitution Check
8. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary
Deliver a Marketplace-published GitHub Action that uses a prebuilt minimal Docker image to run an AOT-compiled .NET agent. When triggered by `issue_comment` created or `issues` opened/reopened events, the agent issues a GraphQL query (authenticated with the default `GITHUB_TOKEN`) to retrieve the triggering issue and its comments, logging clear success or actionable failure output so future automation can reuse the issue context. Scope is limited to issue-context retrieval; richer agent behaviors are deferred.

## Technical Context
**Language/Version**: .NET 8.0 LTS (AOT publish)  
**Primary Dependencies**: Microsoft Agent Framework SDK, Octokit.GraphQL (AOT-compatible), GitHub Actions metadata schema  
**Storage**: None (in-memory only)  
**Testing**: xUnit for unit tests, integration harness using GitHub Actions Toolkit mocks  
**Target Platform**: GitHub Actions runners (`ubuntu-latest`) with Marketplace distribution
**Project Type**: Single-service GitHub Action with supporting agent library  
**Performance Goals**: Monitor cold start; no hard threshold this release but aim to stay within constitution’s <3s expectation when practical  
**Constraints**: Minimal container footprint (<150 MB compressed), no outbound calls beyond GitHub GraphQL API, default `GITHUB_TOKEN` only  
**Scale/Scope**: Single-repository event processing for issue triage signals

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

1. **Security-First GitHub Operations** – Use workflow-provided `GITHUB_TOKEN`, request only `issues:read` scope, redact GraphQL payloads in logs. ✅
2. **Minimum Viable Delivery** – Delivery limited to issue-context retrieval, measurable via logged success/fail metrics. ✅
3. **Performance-Ready Execution** – AOT publish, prebuilt container, monitor cold-start metrics; no additional dependencies that risk >3s. ✅ (monitor)
4. **Human-Centered Agent Experience** – Log actionable guidance on failure; no conversational automation yet. ✅
5. **Customizable Extensibility** – Document configurable inputs (`github-token`, future expansions) in README; default works out of the box. ✅
6. **C#/.NET + Microsoft Agent Framework Mandate** – Agent built on Microsoft Agent Framework targeting .NET 8 LTS. ✅

*Result*: Initial Constitution Check PASS

## Project Structure

### Documentation (this feature)
```
specs/[###-feature]/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
src/
├── IssueAgent.Action/                # GitHub Action entrypoint (action.yml, Dockerfile)
│   ├── action.yml
│   ├── Dockerfile
│   └── Scripts/
├── IssueAgent.Agent/                 # AOT .NET agent (Microsoft Agent Framework)
│   ├── IssueAgent.Agent.csproj
│   ├── GraphQL/
│   │   └── IssueContextQuery.cs
│   └── Runtime/
│       └── AgentBootstrap.cs
└── IssueAgent.Shared/                # Shared options & logging abstractions
   └── IssueAgent.Shared.csproj

tests/
├── IssueAgent.UnitTests/
│   └── Agent/
├── IssueAgent.IntegrationTests/
│   └── GitHubGraphQL/
└── IssueAgent.ContractTests/
   └── GraphQL/
       └── IssueContextQueryTests.cs

containers/
└── action/
   └── Dockerfile
```

**Structure Decision**: Adopt a three-project solution (Action wrapper, Agent runtime, Shared utilities) to isolate reusable logic and keep Docker image footprint minimal; dedicated test projects mirror runtime layers for clarity and constitutional compliance.
## Phase 0: Outline & Research
Completed research tasks captured in `research.md`:
- Validated Octokit.GraphQL as the AOT-friendly client with `GITHUB_TOKEN` support; confirmed issue-context query structure using environment variables (`GITHUB_REPOSITORY`, event payload path).
- Compared container base images (distroless vs. Alpine) for AOT publishing; selected `mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine` with trimming to stay under 150 MB.
- Reviewed Microsoft Agent Framework bootstrap requirements inside GitHub Actions to ensure graceful logging and exit codes.
- Documented guidance for monitoring cold-start metrics without enforcing thresholds.

All unknowns resolved; no remaining clarifications.

## Phase 1: Design & Contracts
Artifacts produced during this phase:
- `data-model.md` – Defines `IssueContextResult` and `IssueSnapshot` structures plus validation expectations.
- `/contracts/issue-context-query.graphql` – GraphQL contract fetching issue and comments; `/contracts/issue-context-query.tests.md` outlines failing contract tests.
- `quickstart.md` – Step-by-step workflow showing how to consume the action, required permissions, and expected logs.
- `.github/copilot-instructions.md` updated via mandated script with new technologies (Microsoft Agent Framework, Octokit.GraphQL, minimal container strategy).

These outputs satisfy Phase 1 prerequisites and maintain constitutional alignment. Post-Design Constitution Check PASS.

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
- Load `.specify/templates/tasks-template.md` as base
- Generate tasks from Phase 1 design docs (contracts, data model, quickstart)
- Each contract → contract test task [P]
- Each entity → model creation task [P] 
- Each user story → integration test task (GraphQL success + failure paths)
- Implementation tasks to make tests pass (Docker image, action.yml, agent runtime)

**Ordering Strategy**:
- TDD order: Tests before implementation 
- Dependency order: Models before services before UI
- Mark [P] for parallel execution (independent files)

**Estimated Output**: 25-30 numbered, ordered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| _None_ | — | — |


## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command)
- [x] Phase 1: Design complete (/plan command)
- [ ] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved
- [x] Complexity deviations documented

---
*Based on Constitution v1.0.0 - See `/memory/constitution.md`*
