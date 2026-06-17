#!/usr/bin/env bash

# Poll GitHub Actions for required workflows on a commit.

# Usage: scripts/check-github-ci.sh [REF] [--wait SECONDS] [--require-pages] [--require-worker]

# Requires: gh CLI authenticated to the repo.

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

cd "$ROOT"



REQUIRE_PAGES=false

REQUIRE_WORKER=false

WAIT=0

REF="HEAD"

POSITIONAL=()



while [ $# -gt 0 ]; do

  case "$1" in

    --require-pages) REQUIRE_PAGES=true; shift ;;

    --require-worker) REQUIRE_WORKER=true; shift ;;

    --wait)

      WAIT="${2:-300}"

      shift 2

      ;;

    --)

      shift

      POSITIONAL+=("$@")

      break

      ;;

    -*)

      echo "Unknown option: $1"

      echo "Usage: $0 [REF] [--wait SECONDS] [--require-pages] [--require-worker]"

      exit 1

      ;;

    *)

      POSITIONAL+=("$1")

      shift

      ;;

  esac

done



if [ "${#POSITIONAL[@]}" -gt 0 ]; then

  REF="${POSITIONAL[0]}"

fi



REF="$(git rev-parse "$REF")"



REQUIRED=("CI" "Security Scan" "CodeQL")

PATH_TRIGGERED=("GitHub Pages" "Steam library worker")



if ! command -v gh >/dev/null 2>&1; then

  echo "ERROR: gh CLI required (https://cli.github.com/)"

  exit 1

fi



REPO="$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null || true)"

if [ -z "$REPO" ]; then

  echo "ERROR: run from a git repo with gh auth, or export GITHUB_REPO=owner/name"

  exit 1

fi



echo "GitHub Actions status for ${REPO} @ ${REF:0:7}"

if [ "$REQUIRE_PAGES" = true ] || [ "$REQUIRE_WORKER" = true ]; then

  echo "Required path workflows: pages=${REQUIRE_PAGES} worker=${REQUIRE_WORKER}"

fi



deadline=$((SECONDS + WAIT))

while true; do

  mapfile -t RUNS < <(gh run list --repo "$REPO" --commit "$REF" \

    --json workflowName,conclusion,status,url -q '.[] | [.workflowName,.status,.conclusion,.url] | @tsv')



  declare -A SEEN=()

  PENDING=0

  FAILED=0



  for wf in "${REQUIRED[@]}"; do

    line="$(printf '%s\n' "${RUNS[@]}" | grep "^${wf}"$'\t' | head -1 || true)"

    if [ -z "$line" ]; then

      echo "WAIT ${wf}: no run yet"

      PENDING=$((PENDING + 1))

      continue

    fi

    IFS=$'\t' read -r _ status conclusion url <<<"$line"

    SEEN["$wf"]=1

    case "$conclusion" in

      success) echo "OK   ${wf}: ${url}" ;;

      failure|cancelled|timed_out|action_required)

        echo "FAIL ${wf} (${conclusion}): ${url}"

        FAILED=$((FAILED + 1))

        ;;

      *)

        if [ "$status" = "completed" ]; then

          echo "FAIL ${wf} (${conclusion:-unknown}): ${url}"

          FAILED=$((FAILED + 1))

        else

          echo "WAIT ${wf} (${status}): ${url}"

          PENDING=$((PENDING + 1))

        fi

        ;;

    esac

  done



  if [ "$FAILED" -gt 0 ]; then

    echo "${FAILED} required workflow(s) failed on GitHub"

    exit 1

  fi



  PATH_FAILED=0

  for wf in "${PATH_TRIGGERED[@]}"; do

    required=false

    case "$wf" in

      "GitHub Pages") [ "$REQUIRE_PAGES" = true ] && required=true ;;

      "Steam library worker") [ "$REQUIRE_WORKER" = true ] && required=true ;;

    esac



    line="$(printf '%s\n' "${RUNS[@]}" | grep "^${wf}"$'\t' | head -1 || true)"

    if [ -z "$line" ]; then

      if [ "$required" = true ]; then

        echo "WAIT ${wf}: required but no run yet"

        PENDING=$((PENDING + 1))

      else

        echo "SKIP ${wf}: no run on this commit"

      fi

      continue

    fi

    IFS=$'\t' read -r _ status conclusion url <<<"$line"

    case "$conclusion" in

      success) echo "OK   ${wf}: ${url}" ;;

      failure|cancelled|timed_out|action_required)

        echo "FAIL ${wf} (${conclusion}): ${url}"

        PATH_FAILED=$((PATH_FAILED + 1))

        ;;

      *)

        if [ "$status" = "completed" ]; then

          echo "FAIL ${wf} (${conclusion:-unknown}): ${url}"

          PATH_FAILED=$((PATH_FAILED + 1))

        else

          echo "WAIT ${wf} (${status}): ${url}"

          PENDING=$((PENDING + 1))

        fi

        ;;

    esac

  done



  if [ "$PATH_FAILED" -gt 0 ]; then

    echo "${PATH_FAILED} path-triggered workflow(s) failed on GitHub"

    exit 1

  fi



  if [ "$PENDING" -eq 0 ]; then

    echo "All ${#REQUIRED[@]} required workflows passed on GitHub"

    if [ "${#PATH_TRIGGERED[@]}" -gt 0 ]; then

      echo "Path-triggered workflows checked when present"

    fi

    exit 0

  fi

  if [ "$WAIT" -eq 0 ] || [ "$SECONDS" -ge "$deadline" ]; then

    echo "INCOMPLETE: ${PENDING} workflow(s) still pending (re-run with --wait 300)"

    exit 1

  fi

  sleep 15

done

