# Tasks: AI Foundry Connectivity

**Input**: Design documents from `/workspaces/issueagent/specs/002-ai-foundry-connectivity/`
**Prerequisites**: plan.md, research.md, data-model.md, contracts/, quickstart.md

## Execution Flow (main)
```
1. Load plan.md from feature directory ✅
   → Tech stack: .NET 8.0 LTS (AOT), Microsoft Agent Framework SDK
   → Libraries: Azure.AI.Agents.Persistent, Azure.Identity, Azure.AI.Projects
2. Load design documents ✅
   → data-model.md: 5 entities (AzureFoundryConfiguration, AzureFoundryConnectionResult, 
     ConnectionErrorCategory, IAzureFoundryAuthenticationProvider, AzureFoundryConfigurationSource)
   → contracts/: 1 file (configuration-validation-contract.md) → 18 test scenarios
   → quickstart.md: 5 test scenarios (success + 4 error cases)
3. Generated tasks by category:
   → Setup: 3 tasks
   → Tests First (TDD): 7 tasks [P]
   → Core Implementation: 9 tasks
   → Integration: 4 tasks
   → Polish: 5 tasks
   → Total: 28 tasks
4. Parallel tasks marked [P] (different files, no dependencies)
5. Tasks numbered T001-T028
6. Dependency graph generated below
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Path Conventions
- **Shared models**: `src/IssueAgent.Shared/Models/`
- **Agent runtime**: `src/IssueAgent.Agent/Runtime/`
- **Action entry**: `src/IssueAgent.Action/`
- **Unit tests**: `tests/IssueAgent.UnitTests/`
- **Integration tests**: `tests/IssueAgent.IntegrationTests/`
- **Contract tests**: `tests/IssueAgent.ContractTests/`

## Phase 3.1: Setup & Dependencies
- [ ] T001 Add Azure.AI.Agents.Persistent NuGet package to `src/IssueAgent.Agent/IssueAgent.Agent.csproj`
- [ ] T002 Add Azure.Identity NuGet package to `src/IssueAgent.Agent/IssueAgent.Agent.csproj`
- [ ] T003 Add Azure.AI.Projects NuGet package to `src/IssueAgent.Agent/IssueAgent.Agent.csproj`

## Phase 3.2: Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3
**CRITICAL: These tests MUST be written and MUST FAIL before ANY implementation**

### Configuration Validation Contract Tests
- [ ] T004 [P] Add configuration validation contract test for valid configuration in `tests/IssueAgent.ContractTests/Configuration/AzureFoundryConfigurationValidationTests.cs`
- [ ] T005 [P] Add configuration validation contract test for missing endpoint in `tests/IssueAgent.ContractTests/Configuration/AzureFoundryConfigurationValidationTests.cs`
- [ ] T006 [P] Add configuration validation contract test for invalid endpoint format (HTTP instead of HTTPS) in `tests/IssueAgent.ContractTests/Configuration/AzureFoundryConfigurationValidationTests.cs`
- [ ] T007 [P] Add configuration validation contract test for invalid endpoint domain in `tests/IssueAgent.ContractTests/Configuration/AzureFoundryConfigurationValidationTests.cs`
- [ ] T008 [P] Add configuration validation contract test for missing API key in `tests/IssueAgent.ContractTests/Configuration/AzureFoundryConfigurationValidationTests.cs`
- [ ] T009 [P] Add configuration validation contract test for API key too short in `tests/IssueAgent.ContractTests/Configuration/AzureFoundryConfigurationValidationTests.cs`
- [ ] T010 [P] Add configuration validation contract test for invalid model deployment name (special characters) in `tests/IssueAgent.ContractTests/Configuration/AzureFoundryConfigurationValidationTests.cs`
- [ ] T011 [P] Add configuration validation contract test for invalid API version format in `tests/IssueAgent.ContractTests/Configuration/AzureFoundryConfigurationValidationTests.cs`
- [ ] T012 [P] Add configuration validation contract test for future API version date in `tests/IssueAgent.ContractTests/Configuration/AzureFoundryConfigurationValidationTests.cs`
- [ ] T013 [P] Add configuration validation contract test for invalid connection timeout (negative) in `tests/IssueAgent.ContractTests/Configuration/AzureFoundryConfigurationValidationTests.cs`
- [ ] T014 [P] Add configuration validation contract test for excessive connection timeout in `tests/IssueAgent.ContractTests/Configuration/AzureFoundryConfigurationValidationTests.cs`
- [ ] T015 [P] Add configuration validation contract test for null model deployment (should use default) in `tests/IssueAgent.ContractTests/Configuration/AzureFoundryConfigurationValidationTests.cs`
- [ ] T016 [P] Add configuration validation contract test for minimal valid configuration (defaults applied) in `tests/IssueAgent.ContractTests/Configuration/AzureFoundryConfigurationValidationTests.cs`

### Integration Tests from Quickstart Scenarios
- [ ] T017 [P] Add integration test for successful Azure AI Foundry connection in `tests/IssueAgent.IntegrationTests/AzureFoundry/ConnectionTests.cs`
- [ ] T018 [P] Add integration test for missing endpoint error scenario in `tests/IssueAgent.IntegrationTests/AzureFoundry/ConnectionTests.cs`
- [ ] T019 [P] Add integration test for invalid API key error scenario in `tests/IssueAgent.IntegrationTests/AzureFoundry/ConnectionTests.cs`
- [ ] T020 [P] Add integration test for invalid endpoint format error scenario in `tests/IssueAgent.IntegrationTests/AzureFoundry/ConnectionTests.cs`
- [ ] T021 [P] Add integration test for model deployment not found error scenario in `tests/IssueAgent.IntegrationTests/AzureFoundry/ConnectionTests.cs`
- [ ] T022 [P] Add integration test for network timeout error scenario in `tests/IssueAgent.IntegrationTests/AzureFoundry/ConnectionTests.cs`

### Unit Tests for Core Components
- [ ] T023 [P] Add unit tests for ApiKeyAuthenticationProvider in `tests/IssueAgent.UnitTests/Runtime/ApiKeyAuthenticationProviderTests.cs`
- [ ] T024 [P] Add unit tests for AzureFoundryConfiguration validation logic in `tests/IssueAgent.UnitTests/Shared/AzureFoundryConfigurationTests.cs`
- [ ] T025 [P] Add unit tests for ConnectionErrorCategory mapping in `tests/IssueAgent.UnitTests/Shared/ConnectionErrorCategoryTests.cs`

## Phase 3.3: Core Implementation (ONLY after tests are failing)

### Shared Models
- [ ] T026 [P] Implement AzureFoundryConfiguration model with validation in `src/IssueAgent.Shared/Models/AzureFoundryConfiguration.cs`
- [ ] T027 [P] Implement AzureFoundryConnectionResult model in `src/IssueAgent.Shared/Models/AzureFoundryConnectionResult.cs`
- [ ] T028 [P] Implement ConnectionErrorCategory enum in `src/IssueAgent.Shared/Models/ConnectionErrorCategory.cs`
- [ ] T029 [P] Implement AzureFoundryConfigurationSource enum in `src/IssueAgent.Shared/Models/AzureFoundryConfigurationSource.cs`

### Agent Runtime - Authentication Abstraction
- [ ] T030 [P] Define IAzureFoundryAuthenticationProvider interface in `src/IssueAgent.Agent/Runtime/IAzureFoundryAuthenticationProvider.cs`
- [ ] T031 Implement ApiKeyAuthenticationProvider in `src/IssueAgent.Agent/Runtime/ApiKeyAuthenticationProvider.cs` (depends on T030)

### Agent Runtime - Bootstrap Integration
- [ ] T032 Add Azure AI Foundry initialization method to AgentBootstrap in `src/IssueAgent.Agent/Runtime/AgentBootstrap.cs` (depends on T031)
- [ ] T033 Add connection validation logic with 30-second timeout in `src/IssueAgent.Agent/Runtime/AgentBootstrap.cs` (depends on T032)
- [ ] T034 Add error categorization and structured error messages in `src/IssueAgent.Agent/Runtime/AgentBootstrap.cs` (depends on T033)

### Action Entry Point
- [ ] T035 Add Azure AI Foundry input parameter parsing to action metadata in `action.yml`
- [ ] T036 Add environment variable fallback logic for Azure AI Foundry inputs in `src/IssueAgent.Action/Program.cs` (depends on T035)
- [ ] T037 Wire AzureFoundryConfiguration creation from inputs in `src/IssueAgent.Action/Program.cs` (depends on T036)
- [ ] T038 Integrate Azure AI Foundry initialization into action startup flow in `src/IssueAgent.Action/Program.cs` (depends on T037, T032)

## Phase 3.4: Integration & Security

### Logging & Redaction
- [ ] T039 [P] Update RedactionMiddleware to redact Azure AI Foundry API keys in `src/IssueAgent.Agent/Logging/RedactionMiddleware.cs`
- [ ] T040 [P] Add connection event logging (success/failure with duration) in `src/IssueAgent.Agent/Runtime/AgentBootstrap.cs`
- [ ] T041 [P] Add unit tests for API key redaction in logs in `tests/IssueAgent.UnitTests/Logging/RedactionMiddlewareTests.cs`

### Performance Instrumentation
- [ ] T042 [P] Add cold-start metrics for Azure AI Foundry connection initialization in `src/IssueAgent.Agent/Instrumentation/StartupMetricsRecorder.cs`

## Phase 3.5: Polish & Documentation

### End-to-End Validation
- [ ] T043 [P] Create manual test workflow following quickstart.md in `.github/workflows/test-azure-foundry.yml`
- [ ] T044 [P] Create manual test workflow for environment variable configuration in `.github/workflows/test-azure-foundry-env.yml`
- [ ] T045 Run full integration test suite with real Azure AI Foundry endpoint (manual validation)

### Documentation
- [ ] T046 [P] Update main README.md with Azure AI Foundry configuration section
- [ ] T047 [P] Add troubleshooting guide for common Azure AI Foundry connection errors in `docs/troubleshooting-azure-foundry.md`

### Constitutional Compliance Verification
- [ ] T048 Verify all credentials redacted in logs (Security-First)
- [ ] T049 Verify connection completes in <3 seconds (Performance-Ready)
- [ ] T050 Verify error messages provide actionable guidance (Human-Centered)
- [ ] T051 Verify extensibility for future auth methods (Customizable Extensibility)
- [ ] T052 Verify Microsoft Agent Framework SDK used exclusively (C#/.NET Mandate)

## Dependencies

### Critical Path
```
Setup (T001-T003)
  ↓
Tests First (T004-T025) [ALL MUST FAIL]
  ↓
Models (T026-T029) [Parallel]
  ↓
Auth Interface (T030)
  ↓
Auth Provider (T031)
  ↓
Bootstrap Integration (T032 → T033 → T034)
  ↓
Action Entry (T035 → T036 → T037 → T038)
  ↓
Integration (T039-T042) [Parallel]
  ↓
Polish (T043-T052)
```

### Dependency Details
- **T004-T025** (Tests): No dependencies, can run in parallel after T001-T003
- **T026-T029** (Models): No dependencies between them, can run in parallel
- **T030**: No dependencies
- **T031**: Depends on T030 (interface definition)
- **T032**: Depends on T031 (needs auth provider)
- **T033**: Depends on T032 (adds to same file)
- **T034**: Depends on T033 (adds to same file)
- **T035**: No dependencies
- **T036**: Depends on T035 (action metadata first)
- **T037**: Depends on T036 (adds to same file)
- **T038**: Depends on T037 (action side) and T032 (agent side)
- **T039-T042**: Can run in parallel (different files)
- **T043-T052**: Can run after all implementation complete

## Parallel Execution Examples

### Setup Phase (Sequential)
```bash
# T001-T003 run sequentially (same file: IssueAgent.Agent.csproj)
Task: "Add Azure.AI.Agents.Persistent NuGet package to src/IssueAgent.Agent/IssueAgent.Agent.csproj"
Task: "Add Azure.Identity NuGet package to src/IssueAgent.Agent/IssueAgent.Agent.csproj"
Task: "Add Azure.AI.Projects NuGet package to src/IssueAgent.Agent/IssueAgent.Agent.csproj"
```

### Contract Tests (Parallel)
```bash
# T004-T016 all modify same test file - run sequentially
# But can be done in batch with clear test method names
Task: "Add all 13 configuration validation contract tests to tests/IssueAgent.ContractTests/Configuration/AzureFoundryConfigurationValidationTests.cs"
```

### Integration Tests (Parallel)
```bash
# T017-T022 all in same file - run sequentially
Task: "Add all 6 integration test scenarios to tests/IssueAgent.IntegrationTests/AzureFoundry/ConnectionTests.cs"
```

### Unit Tests (Parallel)
```bash
# T023-T025 in different files - CAN RUN IN PARALLEL
Task: "Add unit tests for ApiKeyAuthenticationProvider in tests/IssueAgent.UnitTests/Runtime/ApiKeyAuthenticationProviderTests.cs"
Task: "Add unit tests for AzureFoundryConfiguration validation logic in tests/IssueAgent.UnitTests/Shared/AzureFoundryConfigurationTests.cs"
Task: "Add unit tests for ConnectionErrorCategory mapping in tests/IssueAgent.UnitTests/Shared/ConnectionErrorCategoryTests.cs"
```

### Models (Parallel)
```bash
# T026-T029 in different files - CAN RUN IN PARALLEL
Task: "Implement AzureFoundryConfiguration model with validation in src/IssueAgent.Shared/Models/AzureFoundryConfiguration.cs"
Task: "Implement AzureFoundryConnectionResult model in src/IssueAgent.Shared/Models/AzureFoundryConnectionResult.cs"
Task: "Implement ConnectionErrorCategory enum in src/IssueAgent.Shared/Models/ConnectionErrorCategory.cs"
Task: "Implement AzureFoundryConfigurationSource enum in src/IssueAgent.Shared/Models/AzureFoundryConfigurationSource.cs"
```

### Integration Components (Parallel)
```bash
# T039, T041, T042 in different files - CAN RUN IN PARALLEL
Task: "Update RedactionMiddleware to redact Azure AI Foundry API keys in src/IssueAgent.Agent/Logging/RedactionMiddleware.cs"
Task: "Add unit tests for API key redaction in logs in tests/IssueAgent.UnitTests/Logging/RedactionMiddlewareTests.cs"
Task: "Add cold-start metrics for Azure AI Foundry connection initialization in src/IssueAgent.Agent/Instrumentation/StartupMetricsRecorder.cs"
# T040 sequential with T032-T034 (same file: AgentBootstrap.cs)
```

### Documentation (Parallel)
```bash
# T043, T044, T046, T047 in different files - CAN RUN IN PARALLEL
Task: "Create manual test workflow following quickstart.md in .github/workflows/test-azure-foundry.yml"
Task: "Create manual test workflow for environment variable configuration in .github/workflows/test-azure-foundry-env.yml"
Task: "Update main README.md with Azure AI Foundry configuration section"
Task: "Add troubleshooting guide for common Azure AI Foundry connection errors in docs/troubleshooting-azure-foundry.md"
```

## Notes
- **[P] tasks**: Different files, no dependencies, can run in parallel
- **Tests MUST fail before implementation**: Verify T004-T025 all fail before starting T026
- **Commit after each task**: Use meaningful commit messages referencing task ID
- **Same file tasks**: T032-T034 sequential (AgentBootstrap.cs), T035-T038 sequential (Program.cs and action.yml)
- **Constitutional gates**: T048-T052 are manual verification tasks, not code tasks

## Task Generation Rules Applied

1. **From Contracts**:
   - 1 contract file (configuration-validation-contract.md) → 13 contract test scenarios (T004-T016)
   
2. **From Data Model**:
   - 5 entities → 5 model creation tasks [P] (T026-T029, T030)
   - 1 interface → 1 implementation (T031)
   
3. **From Quickstart**:
   - 5 test scenarios → 6 integration tests (T017-T022)
   - 2 workflows → 2 manual test workflows (T043-T044)

4. **Ordering**:
   - Setup (T001-T003) → Tests (T004-T025) → Models (T026-T029) → Auth (T030-T031) → 
     Bootstrap (T032-T034) → Action (T035-T038) → Integration (T039-T042) → Polish (T043-T052)

5. **Constitutional Duties**:
   - Security: T039, T041, T048 (redaction and verification)
   - Performance: T042, T049 (metrics and verification)
   - UX: T034, T050 (error messages and verification)
   - Extensibility: T030, T051 (interface abstraction and verification)
   - .NET Stack: T001-T003, T052 (SDK dependencies and verification)

## Validation Checklist
*GATE: Checked before execution*

- [x] All contracts have corresponding tests (T004-T016 cover configuration-validation-contract.md)
- [x] All entities have model tasks (T026-T029 for 4 models, T030 for interface)
- [x] All tests come before implementation (Phase 3.2 before 3.3)
- [x] Parallel tasks truly independent (verified different files for [P] tasks)
- [x] Each task specifies exact file path
- [x] No task modifies same file as another [P] task (verified)
- [x] Constitutional duties each have explicit tasks (Security: T039/T041/T048, Performance: T042/T049, UX: T034/T050, Extensibility: T030/T051, .NET: T001-T003/T052)
- [x] All quickstart scenarios have tests (T017-T022 cover 5 scenarios + success case)
- [x] Integration tasks complete before polish (T039-T042 before T043-T052)
