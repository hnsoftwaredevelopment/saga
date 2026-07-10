# Saga Project Preferences

## Execution Style

For Saga implementation work, use **Inline Execution** as the default execution style.

Reason:

- it uses fewer credits than subagent-driven execution;
- it keeps the work in the current thread;
- it still allows checkpoints, tests, and focused commits per task.

Use subagent-driven execution only when the user explicitly asks for it or when a task is large enough that the user approves the extra token/credit cost.

