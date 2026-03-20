# Styling

## Approach

The CSS system uses three layers with increasing specificity: **tokens**, **layout**, and **components**. All visual values flow from design tokens. No hardcoded values in component or layout styles.

## CSS Layers

CSS `@layer` is used to establish a deterministic specificity order:

```css
@layer tokens, reset, layout, components, utilities;
```

| Layer        | Purpose                                        | Specificity |
|--------------|------------------------------------------------|-------------|
| `tokens`     | CSS custom property definitions                | Lowest      |
| `reset`      | Minimal reset / normalize                      | Low         |
| `layout`     | Page structure: grid, sidebar, content areas   | Medium      |
| `components` | Region styles, cards, lists, skeletons, etc.   | High        |
| `utilities`  | Rare overrides (e.g., `.visually-hidden`)      | Highest     |

Styles in a lower layer never accidentally override styles in a higher layer. This eliminates specificity wars.

## Design Tokens

Tokens are CSS custom properties defined on `:root` in the `tokens` layer. They are the single source of truth for all visual values.

### Color

```css
@layer tokens {
  :root {
    --color-bg-primary: #101216;
    --color-bg-secondary: #171a21;
    --color-bg-surface: #1c1f28;
    --color-bg-elevated: #24272f;

    --color-text-primary: #d4d7e0;
    --color-text-secondary: #7d8290;
    --color-text-muted: #50545f;

    --color-border-default: #282c35;
    --color-border-subtle: #1f222a;

    --color-accent-blue: #4a90d9;
    --color-accent-green: #2faa6f;
    --color-accent-red: #d45454;
    --color-accent-yellow: #c99a2e;
    --color-accent-purple: #8a7abf;

    --color-status-passing: var(--color-accent-green);
    --color-status-failing: var(--color-accent-red);
    --color-status-pending: var(--color-accent-yellow);
    --color-status-unknown: var(--color-text-muted);
  }
}
```

Color is reserved for status and interaction. Neutral surfaces dominate. Accent colors are desaturated to avoid a decorative feel.

### Spacing

A tight scale optimized for information density:

```css
@layer tokens {
  :root {
    --space-1: 0.125rem;  /* 2px */
    --space-2: 0.25rem;   /* 4px */
    --space-3: 0.5rem;    /* 8px */
    --space-4: 0.75rem;   /* 12px */
    --space-5: 1rem;      /* 16px */
    --space-6: 1.5rem;    /* 24px */
    --space-7: 2rem;      /* 32px */
    --space-8: 3rem;      /* 48px */
  }
}
```

### Border Radius

Tight radius scale. No pills. No fully rounded elements.

```css
@layer tokens {
  :root {
    --radius-sm: 2px;
    --radius-md: 3px;
    --radius-lg: 4px;
  }
}
```

### Typography

```css
@layer tokens {
  :root {
    --font-family-body: "Inter", -apple-system, BlinkMacSystemFont, system-ui, sans-serif;
    --font-family-mono: "JetBrains Mono", "Fira Code", monospace;

    --font-size-xs: 0.6875rem;  /* 11px */
    --font-size-sm: 0.8125rem;  /* 13px */
    --font-size-base: 0.875rem; /* 14px */
    --font-size-lg: 1rem;       /* 16px */
    --font-size-xl: 1.125rem;   /* 18px */
    --font-size-2xl: 1.25rem;   /* 20px */
    --font-size-3xl: 1.5rem;    /* 24px */

    --font-weight-normal: 400;
    --font-weight-medium: 500;
    --font-weight-semibold: 600;
    --font-weight-bold: 700;

    --line-height-tight: 1.2;
    --line-height-base: 1.4;
    --line-height-relaxed: 1.6;
  }
}
```

### Elevation (Shadows)

```css
@layer tokens {
  :root {
    --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.15);
    --shadow-md: 0 2px 4px rgba(0, 0, 0, 0.15);
    --shadow-lg: 0 4px 8px rgba(0, 0, 0, 0.2);
  }
}
```

### Motion

```css
@layer tokens {
  :root {
    --duration-fast: 80ms;
    --duration-base: 150ms;
    --duration-slow: 300ms;
    --easing-default: cubic-bezier(0.4, 0, 0.2, 1);
    --easing-in: cubic-bezier(0.4, 0, 1, 1);
    --easing-out: cubic-bezier(0, 0, 0.2, 1);
  }
}
```

## How Components Consume Tokens

Components reference tokens exclusively through `var()`:

```css
@layer components {
  .pipeline-card {
    background: var(--color-bg-surface);
    border: 1px solid var(--color-border-default);
    border-radius: var(--radius-md);
    padding: var(--space-4);
  }
}
```

Surfaces are flat and border-defined. Shadows are available but used sparingly — prefer border contrast over elevation.

**Rules:**

- Never use a raw color value (like `#fff` or `rgb(...)`) in component or layout CSS.
- Never use a raw spacing value (like `16px` or `1rem`) in component or layout CSS.
- If you need a value that doesn't exist in the token set, add it to the tokens first.
- Token names should describe purpose, not visual value (`--color-status-failing`, not `--color-red`).

## Skeleton / Placeholder Styles

Skeletons are styled in the `components` layer:

```css
@layer components {
  .skeleton {
    background: var(--color-bg-elevated);
    border-radius: var(--radius-sm);
    animation: skeleton-pulse var(--duration-slow) var(--easing-default) infinite alternate;
  }

  .skeleton-row {
    height: var(--space-4);
    margin-bottom: var(--space-3);
  }

  @keyframes skeleton-pulse {
    from { opacity: 0.3; }
    to { opacity: 0.6; }
  }
}
```

## File Organization

```
src/
  styles/
    tokens.css          ← all token definitions
    reset.css           ← minimal reset
    layout.css          ← page structure styles
    components.css      ← shared component styles (skeletons, cards, etc.)
    utilities.css       ← rare utility classes
    main.css            ← imports all above in layer order
```

`main.css` is the single entry point:

```css
@layer tokens, reset, layout, components, utilities;

@import "./tokens.css" layer(tokens);
@import "./reset.css" layer(reset);
@import "./layout.css" layer(layout);
@import "./components.css" layer(components);
@import "./utilities.css" layer(utilities);
```
