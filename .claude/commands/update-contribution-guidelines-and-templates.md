---
name: update-contribution-guidelines-and-templates
description: Workflow command scaffold for update-contribution-guidelines-and-templates in RunCat365.
allowed_tools: ["Bash", "Read", "Write", "Grep", "Glob"]
---

# /update-contribution-guidelines-and-templates

Use this workflow when working on **update-contribution-guidelines-and-templates** in `RunCat365`.

## Goal

Updates project contribution guidelines and GitHub issue/pr templates to clarify processes or add requirements.

## Common Files

- `CONTRIBUTING.md`
- `.github/ISSUE_TEMPLATE/bug_report.yml`
- `.github/ISSUE_TEMPLATE/feature_request.yml`
- `.github/pull_request_template.md`

## Suggested Sequence

1. Understand the current state and failure mode before editing.
2. Make the smallest coherent change that satisfies the workflow goal.
3. Run the most relevant verification for touched files.
4. Summarize what changed and what still needs review.

## Typical Commit Signals

- Edit CONTRIBUTING.md to update guidelines.
- Edit or add files in .github/ISSUE_TEMPLATE/ and .github/pull_request_template.md to update templates.

## Notes

- Treat this as a scaffold, not a hard-coded script.
- Update the command if the workflow evolves materially.