# Styling

## Design Philosophy

**High-contrast light theme.** Bright, sharp, highly readable. Structure comes from borders and shadows, not color flooding. Status colors are strong and saturated against white.

Core principles:
- **White base** — pure white main content, cool gray sidebar/headers for separation
- **Near-black text** — maximum readability, wide hierarchy gaps
- **Strong status colors** — saturated enough to stand out instantly on white
- **Border-driven structure** — panels are distinct blocks, not floating text

## Approach

The CSS system uses layers with increasing specificity. All visual values flow from design tokens.

## CSS Layers

```css
@layer tokens, reset, layout, components, utilities, responsive;
```

| Layer        | Purpose                                        | Specificity |
|--------------|------------------------------------------------|-------------|
| `tokens`     | CSS custom property definitions                | Lowest      |
| `reset`      | Minimal reset / normalize                      | Low         |
| `layout`     | Page structure: grid, sidebar, content areas   | Medium      |
| `components` | Region styles, cards, lists, skeletons, etc.   | High        |
| `utilities`  | Rare overrides (e.g., `.visually-hidden`)      | Higher      |
| `responsive` | Breakpoint adaptations                         | Highest     |

## Design Tokens

### Color — Backgrounds

White main content. Cool gray for sidebar, topbar, panel headers:

```css
--color-bg-primary: #ffffff;     /* main content — pure white */
--color-bg-secondary: #f4f5f8;  /* sidebar, topbar, panel headers */
--color-bg-surface: #ffffff;    /* cards — white, separated by border/shadow */
--color-bg-elevated: #eef0f4;  /* hover states, active elements */
--color-bg-inset: #e8eaef;     /* inputs, table headers — recessed */
```

### Color — Text

Near-black primary. Wide gap enforces hierarchy:

```css
--color-text-primary: #1a1d26;  /* body text */
--color-text-secondary: #545b6e; /* clearly dimmer but readable */
--color-text-muted: #848c9e;    /* metadata — lighter but legible */
--color-text-bright: #0c0e14;   /* emphasis — near-black */
```

### Color — Borders

Visible against white. Structure the layout:

```css
--color-border-default: #d4d8e0;
--color-border-subtle: #e2e5eb;
--color-border-hard: #bfc4d0;
```

### Color — Status (HIGH CONTRAST)

Darkened and saturated for strong visibility on white backgrounds:

```css
--color-accent-green: #0e8c4a;   /* success — strong green */
--color-accent-red: #d42a2a;     /* failure — strong red */
--color-accent-yellow: #a67b00;  /* degraded/pending — strong amber */
--color-accent-blue: #2570c2;    /* info */
--color-accent-cyan: #0c7f94;    /* active/running */
--color-accent-purple: #7248b8;  /* special */
```

### Status Emphasis

On light backgrounds, glows are replaced with subtle colored rings:

```css
--glow-green: 0 0 0 2px rgba(14, 140, 74, 0.2);
--glow-red: 0 0 0 2px rgba(212, 42, 42, 0.2);
/* etc. */
```

### Status Tints

Subtle background tints for badges:

```css
--tint-green: rgba(14, 140, 74, 0.08);
--tint-red: rgba(212, 42, 42, 0.07);
/* etc. */
```

### Spacing

```css
--space-1: 0.1875rem;  /* 3px */
--space-2: 0.375rem;   /* 6px */
--space-3: 0.625rem;   /* 10px */
--space-4: 1rem;        /* 16px */
--space-5: 1.25rem;     /* 20px */
--space-6: 2rem;        /* 32px */
--space-7: 2.5rem;      /* 40px */
--space-8: 4rem;        /* 64px */
```

### Border Radius

```css
--radius-sm: 4px;
--radius-md: 6px;
--radius-lg: 8px;
```

### Typography

```css
--font-size-xs: 0.8125rem;   /* 13px */
--font-size-sm: 0.875rem;    /* 14px */
--font-size-base: 1rem;      /* 16px */
--font-size-lg: 1.25rem;     /* 20px */
--font-size-xl: 1.375rem;    /* 22px */
--font-size-2xl: 1.5rem;     /* 24px */
--font-size-3xl: 1.875rem;   /* 30px */
```

### Elevation

Light, crisp shadows define panels on white:

```css
--shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.05);
--shadow-md: 0 2px 6px rgba(0, 0, 0, 0.08);
--shadow-lg: 0 4px 12px rgba(0, 0, 0, 0.1);
--shadow-panel: 0 1px 3px rgba(0, 0, 0, 0.06), 0 0 0 1px rgba(0, 0, 0, 0.04);
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
    box-shadow: var(--shadow-panel);
  }
}
```

**Rules:**

- Never use a raw color value in component or layout CSS.
- Never use a raw spacing value in component or layout CSS.
- Status dots use `background` (color token) and `box-shadow` (ring token).
- If a value doesn't exist in the token set, add it to the tokens first.
- Token names describe purpose, not visual value.

## File Organization

```
src/
  styles/
    tokens.css          ← all token definitions
    reset.css           ← minimal reset
    layout.css          ← page structure styles
    components.css      ← shared component styles
    utilities.css       ← rare utility classes
    responsive.css      ← responsive breakpoints
    main.css            ← imports all above in layer order
```

`main.css` is the single entry point:

```css
@layer tokens, reset, layout, components, utilities, responsive;

@import "./tokens.css" layer(tokens);
@import "./reset.css" layer(reset);
@import "./layout.css" layer(layout);
@import "./components.css" layer(components);
@import "./utilities.css" layer(utilities);
@import "./responsive.css" layer(responsive);
```
