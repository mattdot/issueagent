# AI-Powered GitHub Issue Enhancer

## Overview

An AI-powered GitHub Action that analyzes, evaluates, and enhances GitHub issues to improve clarity and readiness for development. It uses Azure OpenAI (via Semantic Kernel) to provide structured feedback, suggest improvements, and help transform raw issue text into well-defined, actionable work items.

## Key Features & Benefits

| Feature | What It Does | Why It Matters |
|---------|--------------|----------------|
| Structured AI Evaluation | Produces a concise summary plus completeness flags (title, description, acceptance criteria), importance, and readiness status. | Quickly surfaces gaps so authors know exactly what to improve—reduces reviewer back‑and‑forth. |
| Context-Aware Enhancement | Generates improved title, description, and acceptance criteria when the original is clear but incomplete. | Avoids noisy rewrites; delivers value-added refinement without overwriting already good work. |
| Existing Label Suggestions | Suggests up to 3 labels strictly from the repository's current label set. | Ensures consistency with existing taxonomy; prevents accidental creation of near-duplicate labels. |
| Deterministic Markdown Round‑Trip | Parses its own prior comments and regenerates output safely. | Allows iterative `/review` cycles without formatting drift or duplicated sections. |
| Command Workflow (`/review`, `/apply`, `/usage`, `/disable`) | Users drive when to re-evaluate, apply changes, view help, or pause automation. | Transparent, reversible control keeps humans in charge of changes to issues. |
| Safe Disable Mechanism | Embeds a hidden HTML marker to stop future automatic evaluations. | Lightweight opt-out per issue with an easy path to re-enable by removing the marker. |
| Minimal Required Inputs | Leverages `GITHUB_REPOSITORY` env and only requires comment ID for comment events. | Reduces configuration errors and speeds adoption. |
| Clear Enhancement Conditions | Skips enhanced content if issue is already "ready" or unclear. | Prevents misleading auto-generated content and focuses effort where it yields ROI. |

> TL;DR: The agent elevates raw issue text into implementable, high-quality issues with targeted, non-intrusive assistance—accelerating triage and grooming while preserving maintainers' control.

## Workflow Usage

The action runs inside a Docker container. Azure OpenAI credentials are required for both `issues` and `issue_comment` events.

### Example Workflow

The action automatically uses GitHub's built-in environment variables:

```yaml
name: AI Issue Enhancer
on:
  issues:
    types: [opened, edited]
  issue_comment:
    types: [created]

jobs:
  enhance:
    runs-on: ubuntu-latest
    permissions:
      issues: write
      contents: read
    steps:
      - uses: actions/checkout@v4
      - name: Run Issue Agent
        uses: mattdot/issueagent@v1
        with:
          azure_openai_api_key: ${{ secrets.AZURE_OPENAI_API_KEY }}
          azure_openai_target_uri: ${{ secrets.AZURE_OPENAI_TARGET_URI }}
```

## Local Development

```bash
pip install -r requirements.txt
```

Run individual modules (e.g. parsing) as needed from `src/`.

## Local Testing with Docker

Issue event simulation using GitHub environment variables:

```bash
docker build -t issueagent .

# Create a GitHub event payload file
cat > /tmp/event.json << 'EOF'
{
  "issue": {
    "number": 123
  }
}
EOF

docker run --rm \
  -e GITHUB_EVENT_NAME=issues \
  -e GITHUB_EVENT_PATH=/tmp/event.json \
  -e GITHUB_TOKEN=ghp_xxx \
  -e GITHUB_REPOSITORY=owner/repo \
  -e INPUT_AZURE_OPENAI_API_KEY=xxx \
  -e INPUT_AZURE_OPENAI_TARGET_URI=https://<resource>.openai.azure.com/openai/deployments/<deployment>/chat/completions?api-version=2024-XX-XX \
  -v /tmp/event.json:/tmp/event.json:ro \
  issueagent
```

Issue comment event simulation (with comment id):

```bash
cat > /tmp/event.json << 'EOF'
{
  "issue": {
    "number": 123
  },
  "comment": {
    "id": 456
  }
}
EOF

docker run --rm \
  -e GITHUB_EVENT_NAME=issue_comment \
  -e GITHUB_EVENT_PATH=/tmp/event.json \
  -e GITHUB_TOKEN=ghp_xxx \
  -e GITHUB_REPOSITORY=owner/repo \
  -e INPUT_AZURE_OPENAI_API_KEY=xxx \
  -e INPUT_AZURE_OPENAI_TARGET_URI=https://<resource>.openai.azure.com/... \
  -v /tmp/event.json:/tmp/event.json:ro \
  issueagent
```

## File Structure

- `src/main.py` – Entry point, event flow
- `src/github_utils.py` – GitHub API helpers & disable marker logic
- `src/openai_utils.py` – Azure OpenAI / Semantic Kernel orchestration
- `src/response_models.py` – JSON ↔ markdown parsing & serialization
- `src/prompts.py` – Prompt construction
- `evaluations/` – Offline evaluation harness (Azure AI evaluators)
- `action.yml` – Action metadata / inputs
- `requirements.txt` – Runtime dependencies

## Inputs

All action inputs map to environment variables `INPUT_<NAME>` automatically. GitHub context is automatically extracted from standard GitHub environment variables. Azure OpenAI credentials are required for both supported events.

| Input Name | Required | Default | Events | Description |
| ---------- | -------- | ------- | ------ | ----------- |
| `github_token` | No | `${{ github.token }}` | all | Token with `issues:write` permission |
| `azure_openai_api_key` | Yes | - | all | Azure OpenAI API key |
| `azure_openai_target_uri` | Yes | - | all | Full chat completions endpoint URL |

Notes:
1. GitHub context (`event_name`, `issue_id`, `issue_comment_id`, `repository`) is automatically extracted from GitHub's standard environment variables (`GITHUB_EVENT_NAME`, `GITHUB_EVENT_PATH`, `GITHUB_TOKEN`, `GITHUB_REPOSITORY`).
2. These values are not configurable through action inputs - they are always read from the GitHub Actions environment.

### Enhancement Conditions

An enhanced issue section is included only when:

- `ready_to_work` is false AND
- `base_story_not_clear` is false

If the issue is already good (`ready_to_work = true`) or unclear (`base_story_not_clear = true`), no enhanced content is generated (an explanatory note is shown for the unclear case).

## Supported Workflow Events

- `issues` – Evaluate newly opened / edited issues.
- `issue_comment` – Execute commands (`/apply`, `/review`, etc.).

## Commands

| Command | Purpose | Notes |
| ------- | ------- | ----- |
| `/review` | Run (or re-run) an evaluation and post results. | Use iteratively as issue evolves. |
| `/apply` | Apply latest AI-enhanced title/body/labels. | Only works if a prior evaluation comment exists. |
| `/usage` | Show help / command list. | Always available. |
| `/disable` | Stop automatic AI evaluations for this issue. | Adds a hidden HTML marker. Remove the marker & comment `/review` to re-enable. |

Disable marker used: `<!-- agent:disabled -->`.

## Evaluations Harness

The `evaluations/` folder provides `evaluator.py` leveraging Azure AI Foundry evaluators (Task Adherence, Intent Resolution, Response Completeness). See `evaluations/README.md` for environment variable requirements and execution steps:

```bash
cd evaluations
python evaluator.py
```

## Security / Permissions

Grant the workflow only the permissions it needs (minimum: `issues: write`, `contents: read`). Secrets required: Azure OpenAI key and target URI.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

MIT – see [LICENSE](LICENSE).