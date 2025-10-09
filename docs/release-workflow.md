# Release Workflow - Container Image Publishing

The `release.yml` workflow publishes container images to GitHub Container Registry (ghcr.io) with different tagging strategies based on the trigger type.

## Publishing Scenarios

### Version Tag Releases (e.g., `v1.2.3`)

When a version tag matching `v*.*.*` is pushed, the workflow creates:

- **Semver tags**: `1.2.3`, `1.2`, `1`
- **SHA tag**: `sha-<commit-sha>`
- **latest tag**: Updated to this version
- **canary tag**: Updated to this version

**Example:**
```bash
git tag v1.2.3
git push origin v1.2.3
```

**Published tags:**
- `ghcr.io/mattdot/issueagent:1.2.3`
- `ghcr.io/mattdot/issueagent:1.2`
- `ghcr.io/mattdot/issueagent:1`
- `ghcr.io/mattdot/issueagent:latest`
- `ghcr.io/mattdot/issueagent:canary`
- `ghcr.io/mattdot/issueagent:sha-abc123`

### Main Branch Pushes

When code is pushed to the `main` branch, the workflow creates:

- **SHA tag**: `sha-<commit-sha>`
- **canary tag**: Updated to this commit
- **No semver tags**
- **No latest tag update**

**Example:**
```bash
git push origin main
```

**Published tags:**
- `ghcr.io/mattdot/issueagent:canary`
- `ghcr.io/mattdot/issueagent:sha-abc123`

## Tag Usage Recommendations

| Tag | Purpose | Updated By |
|-----|---------|------------|
| `latest` | Production-ready stable release | Version tags only |
| `canary` | Bleeding edge (main branch HEAD) | Both main pushes and version tags |
| `sha-<hash>` | Specific commit for reproducibility | Both main pushes and version tags |
| Semver (`1.2.3`, `1.2`, `1`) | Version-specific pulls | Version tags only |

## Using the Images

**For production use:**
```yaml
uses: docker://ghcr.io/mattdot/issueagent:latest
# or pin to a specific version
uses: docker://ghcr.io/mattdot/issueagent:1.2.3
```

**For testing bleeding edge changes:**
```yaml
uses: docker://ghcr.io/mattdot/issueagent:canary
```

**For reproducible builds:**
```yaml
uses: docker://ghcr.io/mattdot/issueagent:sha-abc123def
```

## Workflow Requirements

The workflow requires:
- **Permissions**: `contents: read`, `packages: write`
- **Secrets**: Uses `GITHUB_TOKEN` (automatically provided)
- **Registry**: GitHub Container Registry (ghcr.io)

## Release Process

1. **Development**: Work on feature branches, merge to main
   - Triggers canary build on merge
   
2. **Release**: Create and push version tag
   - Triggers full release build with all tags
   
3. **Hotfix**: Push directly to main (if needed)
   - Updates canary tag only

## Monitoring

Check workflow runs at:
- https://github.com/mattdot/issueagent/actions/workflows/release.yml

Published images at:
- https://github.com/mattdot/issueagent/pkgs/container/issueagent
