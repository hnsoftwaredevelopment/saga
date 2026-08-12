# Saga Project Preferences

## Execution Style

For Saga implementation work, use **Inline Execution** as the default execution style.

Reason:

- it uses fewer credits than subagent-driven execution;
- it keeps the work in the current thread;
- it still allows checkpoints, tests, and focused commits per task.

Use subagent-driven execution only when the user explicitly asks for it or when a task is large enough that the user approves the extra token/credit cost.

## Branch And Review Workflow

For Saga milestone or feature work, use a branch-based workflow:

- create or continue a feature branch for the milestone;
- push the branch to GitHub;
- open a pull request so CodeRabbit can review the changes;
- address review feedback before merging;
- merge via the pull request;
- delete the feature branch after the merge.

Only merge directly into `main` when the user explicitly asks for a direct local merge.
