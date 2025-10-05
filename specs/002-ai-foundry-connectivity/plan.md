
# Implementation Plan: AI Foundry Connectivity

**Branch**: `002-ai-foundry-connectivity` | **Date**: 2025-10-05 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/workspaces/issueagent/specs/002-ai-foundry-connectivity/spec.md`

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
Enable IssueAgent GitHub Action to connect to Azure AI Foundry using API key authentication. The action will accept Azure AI Foundry connection parameters (endpoint URL, API key, model deployment name, API version) through GitHub Actions inputs or environment variables, establish authenticated connections using the Microsoft Agent Framework, and validate connectivity during startup with fail-fast error handling (30-second timeout, no retries). The implementation must support future extensibility for additional authentication methods (service principal, managed identity) without breaking changes.

## Technical Context
**Language/Version**: .NET 8.0 LTS (AOT publish)  
**Primary Dependencies**: Microsoft Agent Framework SDK (Microsoft.Agents.AI, Microsoft.Agents.AI.AzureAI), Azure.Identity, Azure.AI.Projects  
**Storage**: N/A (configuration-only feature)  
**Testing**: xUnit for unit tests, integration tests with Azure AI Foundry test endpoints  
**Target Platform**: GitHub Actions runner (ubuntu-latest Docker container)  
**Project Type**: Single (GitHub Action with multiple C# projects)  
**Performance Goals**: <3 second cold-start latency for connection initialization  
**Constraints**: 30-second connection timeout, fail-fast (no retry logic), connection events only logging (no API interaction logs)  
**Scale/Scope**: Single action execution per workflow run, supports concurrent workflows

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**I. Security-First GitHub Operations**
- ✅ API keys accepted only via GitHub Actions secrets (inputs or environment variables)
- ✅ All credential logging redacted via existing RedactionMiddleware
- ✅ No hardcoded credentials or plaintext logging
- ✅ Extensible design for future service principal/managed identity auth

**II. Minimum Viable Delivery**
- ✅ API key authentication only in MVP
- ✅ Success metric: Complete one issue analysis workflow with Azure AI Foundry connection
- ✅ Smallest change: Connection initialization and validation only

**III. Performance-Ready Execution**
- ✅ Connection established once during startup (<3s cold-start target)
- ✅ Fail-fast validation (30s timeout, no retries)
- ✅ .NET 8.0 AOT compilation enabled
- ✅ No runtime dependency resolution

**IV. Human-Centered Agent Experience**
- ✅ Clear error messages for configuration issues with actionable guidance
- ✅ Logs report connection success/failure states clearly
- ✅ Error messages reference specific input parameters or environment variables

**V. Customizable Extensibility**
- ✅ All connection parameters exposed as action inputs with defaults
- ✅ Environment variable alternative for all inputs
- ✅ Forward-compatible design for additional auth methods
- ✅ No breaking changes required for future enhancements

**VI. C#/.NET + Microsoft Agent Framework Mandate**
- ✅ .NET 8.0 LTS exclusively
- ✅ Microsoft Agent Framework SDK for all Azure AI Foundry interactions
- ✅ Follows Agent Framework authentication patterns from official samples

**Initial Assessment**: ✅ PASS - No constitutional violations identified

**Post-Design Assessment**: ✅ PASS - Design maintains constitutional compliance
- Security: Credential handling via IAzureFoundryAuthenticationProvider abstraction with API key redaction
- Minimum Viable: API key auth only, clear success metric in quickstart.md
- Performance: Single connection initialization with 30s timeout, <3s latency target
- Human-Centered: Comprehensive error messages in data-model.md with actionable guidance
- Extensibility: Strategy pattern for auth providers, environment variable support
- .NET/Framework: Uses Azure.AI.Agents.Persistent per research.md, .NET 8.0 AOT

## Project Structure

### Documentation (this feature)
```
specs/002-ai-foundry-connectivity/
├── plan.md              # This file (/plan command output)
├── spec.md              # Feature specification
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
src/
├── IssueAgent.Action/           # Main action entry point (.csproj)
│   ├── Program.cs
│   └── Scripts/
├── IssueAgent.Agent/            # Agent orchestration and runtime (.csproj)
│   ├── Runtime/
│   │   ├── AgentBootstrap.cs
│   │   └── IssueContextAgent.cs
│   ├── GraphQL/
│   ├── Instrumentation/
│   ├── Logging/
│   └── Security/
└── IssueAgent.Shared/           # Shared models and contracts (.csproj)
    └── Models/
        ├── IssueContextRequest.cs
        └── IssueContextResult.cs

tests/
├── IssueAgent.UnitTests/
│   ├── Runtime/
│   ├── Security/
│   ├── Logging/
│   └── Shared/
├── IssueAgent.IntegrationTests/
│   ├── GitHubGraphQL/
│   └── Docker/
└── IssueAgent.ContractTests/
    └── GraphQL/

action.yml                        # GitHub Action metadata
Dockerfile                        # Prebuilt container image
```

**Structure Decision**: This feature adds Azure AI Foundry connectivity to the existing three-project structure:
- **IssueAgent.Shared**: Add AI Foundry connection configuration models
- **IssueAgent.Agent**: Add AI Foundry client initialization and bootstrap integration
- **IssueAgent.Action**: Add action input parsing for AI Foundry parameters
- **Tests**: Add unit tests for configuration validation, integration tests for connection scenarios

## Phase 0: Outline & Research
1. **Extract unknowns from Technical Context** above:
   - For each NEEDS CLARIFICATION → research task
   - For each dependency → best practices task
   - For each integration → patterns task

2. **Generate and dispatch research agents**:
   ```
   For each unknown in Technical Context:
     Task: "Research {unknown} for {feature context}"
   For each technology choice:
     Task: "Find best practices for {tech} in {domain}"
   ```

3. **Consolidate findings** in `research.md` using format:
   - Decision: [what was chosen]
   - Rationale: [why chosen]
   - Alternatives considered: [what else evaluated]

**Output**: research.md with all NEEDS CLARIFICATION resolved

## Phase 1: Design & Contracts
*Prerequisites: research.md complete*

1. **Extract entities from feature spec** → `data-model.md`:
   - Entity name, fields, relationships
   - Validation rules from requirements
   - State transitions if applicable

2. **Generate API contracts** from functional requirements:
   - For each user action → endpoint
   - Use standard REST/GraphQL patterns
   - Output OpenAPI/GraphQL schema to `/contracts/`

3. **Generate contract tests** from contracts:
   - One test file per endpoint
   - Assert request/response schemas
   - Tests must fail (no implementation yet)

4. **Extract test scenarios** from user stories:
   - Each story → integration test scenario
   - Quickstart test = story validation steps

5. **Update agent file incrementally** (O(1) operation):
   - Run `.specify/scripts/bash/update-agent-context.sh copilot`
     **IMPORTANT**: Execute it exactly as specified above. Do not add or remove any arguments.
   - If exists: Add only NEW tech from current plan
   - Preserve manual additions between markers
   - Update recent changes (keep last 3)
   - Keep under 150 lines for token efficiency
   - Output to repository root

**Output**: data-model.md, /contracts/*, failing tests, quickstart.md, agent-specific file

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:

1. **Load data-model.md and contracts/** for entity and validation requirements
2. **Extract test-first tasks** from configuration-validation-contract.md (18 test scenarios)
3. **Generate implementation tasks** from data-model.md entities and research.md decisions
4. **Extract integration tasks** from quickstart.md validation checklist

**Ordering Strategy**:

**Phase 1: Models & Validation** (Parallel-friendly)
- [P] Task 001: Create `AzureFoundryConfiguration` model class
- [P] Task 002: Create `AzureFoundryConnectionResult` model class
- [P] Task 003: Create `ConnectionErrorCategory` enum
- [P] Task 004: Create `IAzureFoundryAuthenticationProvider` interface
- [P] Task 005: Create `AzureFoundryConfigurationSource` enum
- Task 006: Implement `AzureFoundryConfigurationValidator` with all validation rules
- [P] Task 007-024: Write unit tests for each configuration validation scenario (18 tests from contract)

**Phase 2: Authentication Provider** (Depends on Phase 1 models)
- Task 025: Implement `ApiKeyAuthenticationProvider`
- [P] Task 026-028: Write unit tests for API key authentication provider

**Phase 3: Bootstrap Integration** (Depends on Phase 2)
- Task 029: Add Azure AI Foundry initialization to `AgentBootstrap.cs`
- Task 030: Implement connection validation logic
- Task 031: Implement timeout handling (30s, no retry)
- Task 032: Add error categorization and message generation
- [P] Task 033-038: Write unit tests for bootstrap connection logic

**Phase 4: Action Entry Point** (Depends on Phase 1 models)
- [P] Task 039: Add Azure AI Foundry input parsing to `Program.cs`
- [P] Task 040: Add environment variable fallback logic
- [P] Task 041: Update `action.yml` metadata with new inputs
- [P] Task 042-045: Write unit tests for configuration parsing

**Phase 5: Logging & Observability** (Parallel with Phase 4)
- [P] Task 046: Add connection logging with credential redaction
- [P] Task 047: Add error logging with category and guidance
- [P] Task 048-050: Write tests for log redaction

**Phase 6: Integration Tests** (Depends on all previous phases)
- Task 051: Write integration test for successful connection
- Task 052: Write integration test for authentication failure
- Task 053: Write integration test for network timeout
- Task 054: Write integration test for model not found
- Task 055: Write integration test for invalid endpoint

**Phase 7: Documentation & Quickstart** (Parallel with testing)
- [P] Task 056: Update main README with Azure AI Foundry setup instructions
- [P] Task 057: Create example workflow in repository
- [P] Task 058: Execute quickstart.md validation scenarios

**Phase 8: Validation** (Depends on all previous)
- Task 059: Run all unit tests (expect 100% pass rate)
- Task 060: Run all integration tests with real Azure AI Foundry endpoint
- Task 061: Execute quickstart end-to-end
- Task 062: Verify constitutional compliance checklist

**Estimated Total**: 62 tasks
- Parallel-capable [P]: ~28 tasks
- Sequential: ~34 tasks
- Estimated completion time: 6-8 hours with parallelization

**Task Dependencies**:
```
Models (001-005) → Validation (006) → Tests (007-024)
                → Auth Provider (025) → Tests (026-028)
                → Bootstrap (029-032) → Tests (033-038)
                → Action Entry (039-041) → Tests (042-045)
                → Logging (046-047) → Tests (048-050)
                → Integration Tests (051-055)
                → Documentation (056-058)
                → Final Validation (059-062)
```

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
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |


## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command)
- [x] Phase 1: Design complete (/plan command)
- [x] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved
- [x] Complexity deviations documented (none required)

**Artifacts Generated**:
- [x] research.md
- [x] data-model.md
- [x] contracts/configuration-validation-contract.md
- [x] quickstart.md
- [x] .github/copilot-instructions.md (updated)
- [ ] tasks.md (awaiting /tasks command)

---
*Based on Constitution v1.0.0 - See `/memory/constitution.md`*
