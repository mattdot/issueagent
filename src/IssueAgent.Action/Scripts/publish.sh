#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: publish.sh <version> [registry/image]

Builds the Issue Agent Docker image using the multi-stage Dockerfile and optionally pushes it.
- <version>: SemVer tag to apply to the image (e.g. v1.0.0)
- [registry/image]: Optional override for the target image reference (default: ghcr.io/mattdot/issueagent)

Set PUSH=false to skip pushing the image after a successful build.
EOF
}

if [[ ${1:-} == "-h" || ${1:-} == "--help" ]]; then
  usage
  exit 0
fi

if [[ $# -lt 1 ]]; then
  usage >&2
  exit 1
fi

VERSION="$1"
IMAGE_REF_DEFAULT="ghcr.io/mattdot/issueagent"
IMAGE_REF="${2:-$IMAGE_REF_DEFAULT}"
PUSH=${PUSH:-true}
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
DOCKERFILE_PATH="${ROOT_DIR}/containers/action/Dockerfile"

IMAGE_TAG="${IMAGE_REF}:${VERSION}"

echo "Building ${IMAGE_TAG}"

docker buildx build \
  --platform linux/amd64 \
  --file "${DOCKERFILE_PATH}" \
  --tag "${IMAGE_TAG}" \
  "${ROOT_DIR}"

echo "Built ${IMAGE_TAG}"

if [[ "${PUSH}" == "true" ]]; then
  echo "Pushing ${IMAGE_TAG}"
  docker push "${IMAGE_TAG}"
fi
