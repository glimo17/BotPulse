# Requisitos — Motor Multitema (Theming Engine)

## Estado: Planificado (Fase 1 — Quick Win Visual)

Este documento captura los requisitos para implementación próxima. No se ha iniciado.

---

## UI-FR-01: Selector de Temas Global

**Historia de Usuario:** Como usuario, quiero personalizar la apariencia visual de BotPulse con diferentes temas de color, para adaptarlo a mi preferencia y entorno de trabajo.

### Criterios de Aceptación

1. THE **Theme Engine** SHALL support at minimum 4 color themes: Dark (default), Light, Ocean (blue/celeste), Pink (magenta accent).
2. THE **Theme Selector** SHALL be accessible from the Header or navigation bar as a dropdown or color buttons.
3. WHEN the user selects a theme, THE **Theme Engine** SHALL apply it dynamically to the entire application (dashboard, tables, buttons, modals, KPI cards) without page reload.
4. THE **Theme Engine** SHALL persist the user's theme preference in `localStorage` under key `botpulse-theme`.
5. WHEN the application loads, THE **Theme Engine** SHALL read the persisted theme preference and apply it. IF no preference exists, THE default theme SHALL be "Dark".
6. THE **Theme Engine** SHALL implement theming via CSS Custom Properties (CSS variables) on the `<html>` element using a `data-theme` attribute.
7. ALL theme transitions SHALL use `transition-colors duration-300` for smooth visual changes.

---

## UI-FR-02: Tema Dark (Por Defecto)

### Criterios de Aceptación

1. THE **Dark Theme** SHALL use the existing Grafana-inspired palette: dark gray backgrounds (#111217, #181b1f), light text (#d4dce6, #f5f5f5), and accent colors (success #73bf69, warning #f5a623, error #f2495c, accent #3d71e8).
2. THE **Dark Theme** SHALL be the default when no preference is stored.

---

## UI-FR-03: Tema Light (Blanco/Clásico)

### Criterios de Aceptación

1. THE **Light Theme** SHALL use white/light gray backgrounds (#ffffff, #f8f9fa), dark text (#1a1a2e, #333), and adjusted accent colors for contrast on light backgrounds.
2. THE **Light Theme** SHALL maintain WCAG AA contrast ratio (minimum 4.5:1) for all text.
3. THE **Light Theme** SHALL adjust card borders and shadows for depth perception on light backgrounds.

---

## UI-FR-04: Tema Ocean (Celeste/Blue Light)

### Criterios de Aceptación

1. THE **Ocean Theme** SHALL use soft blue/celeste backgrounds (#e8f4fd, #f0f8ff), dark text for readability, and blue-tinted accent colors.
2. THE **Ocean Theme** SHALL be suitable for institutional, medical or financial environments.
3. THE **Ocean Theme** SHALL maintain WCAG AA contrast ratio for all text.

---

## UI-FR-05: Tema Pink (Magenta Accent)

### Criterios de Aceptación

1. THE **Pink Theme** SHALL use a modern vibrant palette with magenta/pink accents (#e91e8c, #ff4da6) on either dark or light backgrounds.
2. THE **Pink Theme** SHALL be useful for differentiating environments (e.g., pink for Production, blue for QA).
3. THE **Pink Theme** SHALL maintain WCAG AA contrast ratio for all text.

---

## Especificaciones Técnicas

### Implementación con CSS Custom Properties

1. THE **Theme Engine** SHALL define CSS custom properties in `:root` / `[data-theme="dark"]` / `[data-theme="light"]` / `[data-theme="ocean"]` / `[data-theme="pink"]` selectors.
2. THE **Tailwind Configuration** SHALL reference these CSS variables in the theme extension (e.g., `colors: { surface: 'var(--color-surface)' }`).
3. THE **Theme Variables** SHALL cover at minimum: `--color-bg-primary`, `--color-bg-secondary`, `--color-bg-card`, `--color-text-primary`, `--color-text-secondary`, `--color-border`, `--color-accent`, `--color-success`, `--color-warning`, `--color-error`.

### Componente ThemeSelector

1. THE **ThemeSelector** SHALL render as a compact UI element (4 colored circles or a dropdown) in the Header component.
2. THE **ThemeSelector** SHALL show the currently active theme with a visual indicator (checkmark or ring).
3. WHEN a theme is selected, THE **ThemeSelector** SHALL set `document.documentElement.dataset.theme` and persist to `localStorage`.

---

## Arquitectura Propuesta

```
ui/src/
├── themes/
│   ├── index.ts          ← Theme manager (get/set/persist)
│   ├── variables.css     ← CSS custom properties for all 4 themes
│   └── themes.ts         ← Theme metadata (name, label, preview color)
├── components/
│   └── ThemeSelector.tsx  ← Dropdown/buttons in Header
└── styles/
    └── globals.css       ← Import variables.css, apply transition-colors
```

---

## Dependencias

- No requiere nuevos paquetes npm (CSS variables nativas + Tailwind theme extension)
- Compatible con el sistema de i18n existente (theme labels traducibles)
- Independiente del backend — es 100% frontend
