# Feature Specification: Core GitHub Action with Minimal AOT .NET Agent

**Feature Branch**: `001-the-core-action`  
**Created**: October 4, 2025  
**Status**: Draft  
**Input**: User description: "the core action that has an action.yml at the root of the repo, that runs in a minimal docker image that is small and fast, that runs a minimal AOT dotnet agent."

## Clarifications

### Session 2025-10-04
- Q: Which GitHub event(s) should invoke this core action in its initial release? → A: issue_comment created and issues opened/reopened
- Q: Which GraphQL operation should the agent execute to retrieve issue context? → A: Query the issue and its comments that triggered the workflow using standard GitHub env vars
- Q: Which token should the action use for the GraphQL request in this release? → A: Default GITHUB_TOKEN
- Q: What cold-start time target should the action meet on GitHub-hosted runners? → A: No specific target for this release

## Execution Flow (main)
```
1. Product team packages and publishes the core GitHub Action to the Marketplace with an `action.yml` manifest at the repository root.
   → If Marketplace submission lacks required metadata: ERROR "action manifest must be discoverable by default"
2. Consuming repositories reference the published Marketplace listing and the workflow runner pulls the associated minimal container image.
   → If image exceeds agreed size/performance budget: WARN "image does not meet minimal footprint requirement"
3. Action boots the precompiled AOT .NET agent within the container.
   → If agent fails readiness check: ERROR "agent did not initialize successfully"
4. Agent executes the minimal issue-context task set for the current release scope.
   → Additional agent responsibilities are tracked separately for future features; defer without blocking the current release.
5. Action processes the triggering GitHub event, issues a GitHub GraphQL request (using standard action environment variables) that retrieves the issue and associated comments responsible for the event, and records the outcome so downstream automation can reuse the context.
   → If the GraphQL request fails: WARN "issue context retrieval must report observable failure details"
   → If the .NET agent encounters an error or lacks permissions: ERROR "surface a descriptive, actionable remediation message to maintainers"
```

---

## ⚡ Quick Guidelines
- ✅ Deliver a repo-level action manifest that onboarding teams can reference without additional configuration.
- ✅ Prioritize fast startup times and minimal resource usage for repeatable automation execution.
- ✅ Keep the initial feature scope limited to retrieving issue context via GitHub GraphQL; defer advanced agent behaviors to future features.
- ❌ Avoid hard-coding environment assumptions beyond the published container contract.

### Section Requirements
- **Mandatory sections**: Complete for this release to capture user-facing expectations.
- **Optional sections**: Introduce only if the action scope expands beyond orchestration.
- When a section does not apply, omit it entirely rather than labeling "N/A".

### For AI Generation
When refining this specification:
1. **Mark all ambiguities** with [NEEDS CLARIFICATION: …] so stakeholders can resolve them quickly.
2. **Do not infer** container base image, runtime tasks, or publishing channels without explicit approval.
3. **Think like a tester**: each requirement must be verifiable via observable action behavior.
4. **Common follow-ups** include:
   - Additional responsibilities to delegate to the AOT agent beyond issue-context retrieval
   - Image size and startup performance thresholds
   - Observability and error reporting expectations
   - Governance for updating the core action over time

---

## User Scenarios & Testing *(mandatory)*

### Primary User Story
As a platform maintainer, I want a Marketplace-published core GitHub Action available at the repository root so that downstream workflows across many repositories can invoke a lightweight .NET agent that retrieves issue context from the GitHub GraphQL API without bespoke setup.

### Acceptance Scenarios
1. **Given** a repository that consumes the core action manifest at the root, **When** the workflow executes on the default GitHub runner in response to an `issue_comment` created or `issues` opened/reopened event, **Then** the action initializes the minimal container, boots the AOT .NET agent, executes the GraphQL request that retrieves the triggering issue and its comments using standard environment configuration, and logs an `IssueContextResult` success within the defined startup budget.
2. **Given** a consuming repository pins the Marketplace version of the action, **When** the product team ships a new release, **Then** change logs and upgrade guidance are available so downstream maintainers can adopt updates intentionally.
3. **Given** the action encounters a runtime failure or permission issue while processing the GraphQL request, **When** the workflow completes, **Then** the action outputs a descriptive, actionable error message (via `IssueContextResult.Status`) that tells maintainers what failed and the next remediation step.

### Edge Cases
- The .NET agent encounters a runtime failure after initialization; action must surface the failure details via `IssueContextResult`, fail the workflow step, and document remediation guidance in logs.
- The action cannot reach the GitHub GraphQL endpoint because of network issues or missing permissions; action must log the reason, emit an actionable error, and avoid partial or stale issue-context states.

## Requirements *(mandatory)*

### Functional Requirements
- **FR-001**: The repo MUST provide a discoverable `action.yml` manifest at the repository root so integrators can include the core action without additional path configuration.
- **FR-002**: The repo MUST distribute a container image that meets the "minimal" footprint agreed with stakeholders, including quantified limits on image size and cold-start latency.
- **FR-003**: The product MUST publish the action to the GitHub Marketplace with clear versioning, documentation, and release notes so external repositories can consume it safely.
- **FR-004**: The action MUST launch a precompiled AOT .NET agent that includes the Microsoft agent framework and an approved GitHub client library as soon as the workflow executes.
- **FR-005**: The action MUST emit clear success, warning, and error signals that downstream workflows can interpret, including descriptive, actionable remediation guidance whenever the .NET agent fails or lacks permission to complete its task.
- **FR-006**: The .NET agent MUST perform a GraphQL request against the endpoint defined by GitHub-provided environment variables to retrieve the triggering issue and its comments, storing the resulting context in workflow logs when triggered by `issue_comment` created or `issues` opened/reopened events.
- **FR-007**: The release scope MUST remain limited to issue-context retrieval (no autonomous or AI-style behaviors) while documenting future expansion as out of scope for this iteration.
- **FR-008**: The repo MUST update the README with usage guidance, including Marketplace installation, required permissions, configuration inputs, and example workflows before each release.
- **FR-009**: The repo MUST document the limited responsibilities delegated to the core agent (issue-context retrieval only) so consuming workflows know which tasks remain out of scope for this release.

### Key Entities *(include if feature involves data)*
- **Core Action Manifest**: Defines the user-facing entry point, including metadata, inputs, and outputs required for invoking the core automation.
- **Minimal Agent Container**: Represents the published runtime environment containing the AOT .NET agent, encapsulating performance budgets, observability signals, and the approved GitHub client library required to perform the issue-context GraphQL request, distributed alongside Marketplace releases.

---

## Constitutional Alignment *(mandatory)*
- **Security-First GitHub Operations**: Ensure the action runs with least-privilege tokens by defaulting to the workflow-provided `GITHUB_TOKEN`, documenting scopes and how secrets are avoided or minimized during agent execution.
- **Minimum Viable Delivery**: Initial release delivers a functional core action manifest plus Marketplace listing and container image validated against agreed performance thresholds, with the .NET agent successfully retrieving issue context via GraphQL on supported events; success measured by the percentage of external repositories adopting the Marketplace action without bespoke setup.
- **Performance-Ready Execution**: Cold-start latency should be monitored on GitHub-hosted runners, but no specific target is enforced for this release; publish observed metrics in release notes for future tuning to ensure timely issue-context delivery.
- **Human-Centered Agent Experience**: Action status messages should be concise, actionable, and surfaced in GitHub workflow logs with remediation guidance for failure cases.
- **Customizable Extensibility**: Document default inputs and describe how consuming workflows can override behavior without editing the action manifest; outline migration path for existing actions to adopt this core asset.
- **C#/.NET + Microsoft Agent Framework Mandate**: Confirm the embedded agent remains on the approved .NET stack with AOT compilation and aligns with the Microsoft agent framework governance.

---

## Review & Acceptance Checklist
*GATE: Automated checks run during main() execution*

### Content Quality
- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

### Requirement Completeness
- [ ] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous  
- [ ] Success criteria are measurable
- [x] Scope is clearly bounded
- [ ] Dependencies and assumptions identified
- [ ] Constitutional Alignment section completed (no unresolved [NEEDS CLARIFICATION])

---

## Execution Status
*Updated by main() during processing*

- [x] User description parsed
- [x] Key concepts extracted
- [x] Ambiguities marked
- [x] User scenarios defined
- [x] Requirements generated
- [x] Entities identified
- [ ] Review checklist passed

---
