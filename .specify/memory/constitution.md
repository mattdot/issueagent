<!--
Sync Impact Report
Version: 0.0.0 → 1.0.0
Modified Principles: None (initial adoption)
Added Sections: Core Principles, Platform and Delivery Constraints, Development Workflow & Quality Gates, Governance
Removed Sections: None
Templates Requiring Updates:
	✅ .specify/templates/plan-template.md
	✅ .specify/templates/spec-template.md
	✅ .specify/templates/tasks-template.md
Follow-up TODOs: None
-->

# IssueAgent Constitution

## Core Principles

### I. Security-First GitHub Operations
The action MUST authenticate with GitHub using secretless mechanisms such as OIDC tokens, request only the minimum permissions required for each workflow, and redact or avoid logging any value that could reveal repository secrets or personal data. Security reviews (threat model, dependency audit, workflow policy) are mandatory before publishing or updating marketplace releases. *Rationale: Marketplace adoption depends on uncompromising trust and containment of blast radius for every automation run.*

### II. Minimum Viable Delivery
Every enhancement MUST deliver the smallest possible change that demonstrably improves issue authoring or triage outcomes, with measurable success criteria captured in specs or release notes. Pull requests that bundle unrelated capabilities or lack user-facing metrics are blocked until they are split or instrumented. *Rationale: Tight delivery loops preserve focus, reduce regression risk, and accelerate marketplace feedback.*

### III. Performance-Ready Execution
Implementations MUST prefer ahead-of-time compiled .NET code, prebuild Docker images, and cache warm paths so cold-start latency remains under 3 seconds for default workflows. Any new dependency or network request needs a quantified impact analysis and mitigation (parallelization, caching, or removal) before merge. *Rationale: Fast activations sustain natural conversation pacing and prevent GitHub workflow timeouts.*

### IV. Human-Centered Agent Experience
The agent MUST communicate through GitHub-native surfaces (issues, pull requests, comments) with tone, formatting, and cadence matching human maintainers. Responses require clear next steps, minimal command syntax, and accessible language for global contributors. *Rationale: A human-like experience raises trust, keeps teams in their existing flow, and reduces onboarding overhead.*

### V. Customizable Extensibility
All behaviors MUST be configurable through documented action inputs or repository-level configuration files, with sensible defaults committed to version control. Breaking changes to configuration schemas require migration tooling or automated fallbacks. *Rationale: Diverse projects need tailored automation while safeguarding existing adopters.*

### VI. C#/.NET + Microsoft Agent Framework Mandate
All runtime code MUST target supported .NET LTS versions and exclusively use the Microsoft Agent Framework for orchestration, messaging, and tool integration. Alternative frameworks, languages, or dynamic runtimes are prohibited unless wrapped behind framework-compliant adapters vetted for security and performance. *Rationale: A single, supported stack streamlines tooling, security validation, and long-term maintenance.*

## Platform and Delivery Constraints
- Package the action as a reusable GitHub Action with marketplace metadata, semantic version tags, and signed release artifacts.
- Prebuild and publish container images during release so the action executes without runtime restores; document image provenance and supply chain controls.
- Enforce least-privilege GitHub token scopes in the action metadata, and gate optional elevated scopes behind opt-in inputs with prominent warnings.
- Maintain a release checklist covering security scanning, performance regression verification, and documentation updates aligned with marketplace requirements.

## Development Workflow & Quality Gates
- Start every feature with a user-centric spec that records measurable value, success metrics, and configuration impacts.
- Threat-model and performance-profile all new integrations before implementation begins; unresolved risks block promotion beyond planning.
- Maintain automated test suites covering unit, integration with GitHub APIs, configuration permutations, and conversational UX snapshots; tests MUST run in CI before release signing.
- Enforce code reviews that checklist constitution compliance (security, minimum viable delivery, performance, UX, extensibility, .NET stack) with documented evidence per pull request.
- After merge, capture learnings and telemetry in release notes to feed future Minimum Viable Delivery decisions.

## Governance
This constitution supersedes conflicting project guidance. Amendments require consensus from maintainers responsible for security, performance, and product experience, plus recorded rationale in the repository history. Versioning follows semantic rules: MAJOR for principle removal or scope changes, MINOR for new principles or substantial policy expansion, PATCH for clarifications. Every quarter (or before marketplace releases), conduct a compliance review documenting adherence, remediation plans, and updated metrics.

**Version**: 1.0.0 | **Ratified**: 2025-10-04 | **Last Amended**: 2025-10-04