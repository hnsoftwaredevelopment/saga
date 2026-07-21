# Saga Settings Behavior

Saga settings should feel like a live desktop experience instead of a restart-driven configuration file.

## Default Behavior

When a user changes a setting, Saga should apply the choice as early as safely possible:

1. **Immediately while Settings is open** when the change can be previewed without data risk, such as language, theme, and active library view.
2. **When Settings closes with Save** when immediate preview is not useful or when the setting affects the next operation, such as import defaults or duplicate overview defaults.
3. **After application restart only when unavoidable**. In that case Saga must clearly tell the user that the setting requires a restart before the user leaves Settings.

## Cancel Behavior

If a setting is previewed live while Settings is open, Cancel should restore the previous visible state when restoration is safe and understandable. Theme and active view changes are examples of reversible live previews.

## Design Rule

Prefer live application and visible feedback. Avoid restart requirements unless a setting changes startup-only infrastructure or cannot be applied safely to the current session.
