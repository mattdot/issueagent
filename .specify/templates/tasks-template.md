# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/

## Execution Flow (main)
```
1. Load plan.md from feature directory
   → If not found: ERROR "No implementation plan found"
   → Extract: tech stack, libraries, structure
2. Load optional design documents:
   → data-model.md: Extract entities → model tasks
   → contracts/: Each file → contract test task
   → research.md: Extract decisions → setup tasks
3. Generate tasks by category:
   → Setup & Tooling: solution updates, dependency declarations, runner configuration
   → Security & Permissions: secretless auth, scope reviews, redaction audits
   → Tests: xUnit/unit, integration with GitHub API, conversational snapshot tests
   → Core Agent Implementation: Microsoft Agent Framework orchestration, domain services
   → Extensibility & Configuration: action inputs, config files, migration scripts
   → Performance & Packaging: cold-start optimization, prebuilt container updates, release signing
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Same file = sequential (no [P])
   → Tests before implementation (TDD)
5. Number tasks sequentially (T001, T002...)
6. Generate dependency graph
7. Create parallel execution examples
8. Validate task completeness:
   → All contracts have tests?
   → All entities have models?
   → All endpoints implemented?
9. Return: SUCCESS (tasks ready for execution)
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Path Conventions
- **Action runtime**: `src/IssueAgent/`
- **Agent orchestration**: `src/IssueAgent.Agent/` (or equivalent namespace)
- **Configuration defaults**: `src/IssueAgent.Configuration/`
- **Tests**: `tests/IssueAgent.UnitTests/`, `tests/IssueAgent.IntegrationTests/`, `tests/IssueAgent.PerformanceTests/`
- **Containers**: `containers/action/`
- Adjust names to match the actual solution (.sln) structure recorded in plan.md

## Phase 3.1: Setup
- [ ] T001 Create project structure per implementation plan
- [ ] T002 Initialize [language] project with [framework] dependencies
- [ ] T003 [P] Configure linting and formatting tools

## Phase 3.2: Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3
**CRITICAL: These tests MUST be written and MUST FAIL before ANY implementation**
- [ ] T004 [P] Add xUnit unit tests for secretless authentication guard in `tests/IssueAgent.UnitTests/Security/AuthenticationGuardTests.cs`
- [ ] T005 [P] Add integration test covering GitHub issue comment handshake in `tests/IssueAgent.IntegrationTests/GitHub/IssueCommentFlowTests.cs`
- [ ] T006 [P] Add conversational snapshot test validating human-like response formatting in `tests/IssueAgent.IntegrationTests/Conversation/ConversationSnapshotTests.cs`
- [ ] T007 [P] Add configuration contract tests for action inputs in `tests/IssueAgent.UnitTests/Configuration/ActionInputContractTests.cs`

## Phase 3.3: Core Implementation (ONLY after tests are failing)
- [ ] T008 [P] Implement authentication guard using Microsoft Agent Framework in `src/IssueAgent/Security/AuthenticationGuard.cs`
- [ ] T009 [P] Build issue processing pipeline in `src/IssueAgent/Agent/IssuePipeline.cs`
- [ ] T010 [P] Implement conversational renderer respecting UX guidelines in `src/IssueAgent/Conversation/ResponseFormatter.cs`
- [ ] T011 Wire configuration loader for action inputs and defaults in `src/IssueAgent.Configuration/ActionSettingsLoader.cs`
- [ ] T012 Implement extensibility hooks for custom workflows in `src/IssueAgent/Agent/Extensions/ExtensionRegistry.cs`
- [ ] T013 Harden logging so sensitive content is redacted before emission in `src/IssueAgent/Infrastructure/Logging/RedactionMiddleware.cs`

## Phase 3.4: Integration
- [ ] T014 Implement GitHub API client adapter with retry/performance policies in `src/IssueAgent/Infrastructure/GitHub/GitHubClientAdapter.cs`
- [ ] T015 Update prebuilt container specification in `containers/action/Dockerfile` with caching layers and publish script
- [ ] T016 Add configuration migration helpers for new inputs in `src/IssueAgent.Configuration/Migrations/*.cs`
- [ ] T017 Capture telemetry for cold-start timing and marketplace metrics in `src/IssueAgent/Infrastructure/Telemetry/ColdStartMetrics.cs`

## Phase 3.5: Polish
- [ ] T018 [P] Expand performance suite in `tests/IssueAgent.PerformanceTests/ColdStart/ColdStartBenchmarks.cs`
- [ ] T019 Document new inputs and UX expectations in `docs/action-config.md`
- [ ] T020 [P] Update release notes with measurable outcomes and security review checklist in `docs/release-notes.md`
- [ ] T021 Run manual conversational regression script in `scripts/manual/conversation-validation.md`
- [ ] T022 Finalize redaction wordlists and add snapshot verification in `tests/IssueAgent.UnitTests/Infrastructure/RedactionTests.cs`

## Dependencies
- Tests (T004-T007) must fail before implementation tasks (T008-T013)
- T008 blocks T009 and T012
- T011 blocks T016 (configuration migrations)
- T014 and T015 complete before performance validation (T018, T020)
- Implementation tasks precede polish (T018-T022)

## Parallel Example
```
# Launch T004-T007 together:
Task: "Add xUnit unit tests for secretless authentication guard"
Task: "Add integration test covering GitHub issue comment handshake"
Task: "Add conversational snapshot test validating human-like response formatting"
Task: "Add configuration contract tests for action inputs"
```

## Notes
- [P] tasks = different files, no dependencies
- Verify tests fail before implementing
- Commit after each task
- Avoid: vague tasks, same file conflicts

## Task Generation Rules
*Applied during main() execution*

1. **From Contracts**:
   - Each contract file → contract test task [P]
   - Each endpoint → implementation task
   
2. **From Data Model**:
   - Each entity → model creation task [P]
   - Relationships → service layer tasks
   
3. **From User Stories**:
   - Each story → integration test [P]
   - Quickstart scenarios → validation tasks

4. **Ordering**:
   - Setup → Tests → Models → Services → Endpoints → Polish
   - Dependencies block parallel execution

5. **Constitutional Duties**:
   - Security, permissions, performance, UX, extensibility, and stack compliance each require at least one explicit task with measurable acceptance criteria

## Validation Checklist
*GATE: Checked by main() before returning*

- [ ] All contracts have corresponding tests
- [ ] All entities have model tasks
- [ ] All tests come before implementation
- [ ] Parallel tasks truly independent
- [ ] Each task specifies exact file path
- [ ] No task modifies same file as another [P] task
- [ ] Constitutional duties (security, performance, UX, extensibility, .NET stack) each have explicit tasks with acceptance criteria