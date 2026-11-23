import type { CSSProperties } from "react";

// Train group category colours, shared by the category form and both calendars.
//
// The app runs on whichever PrimeReact theme the user picked, light or dark, so the
// presets are mid-tones: each keeps at least 3:1 contrast against every theme's card
// colour, from white to the darkest. Text never sits on a full-strength colour - labels
// use a light tint of it under the theme's own text colour, which reads on any theme
// whatever colour the admin picks.

export const CATEGORY_COLOR_PRESETS: string[] = [
  "#0d9488", // teal
  "#3b82f6", // blue
  "#a855f7", // purple
  "#ec4899", // pink
  "#ef4444", // red
  "#ea580c", // orange
  "#d97706", // amber
  "#16a34a", // green
];

// Sessions in no category.
export const NO_CATEGORY_COLOR = "#868e96";

export const isCategoryColor = (value?: string | null): value is string =>
  !!value && /^#[0-9a-f]{6}$/i.test(value);

// What a category shows as. Categories made before colours existed have none stored,
// so they get a preset by id - the same one every time - until the admin picks one.
export const categoryColorOf = (category?: {
  id?: number;
  color?: string | null;
}): string => {
  if (!category) return NO_CATEGORY_COLOR;
  if (isCategoryColor(category.color)) return category.color.toLowerCase();

  const index = Math.max((category.id ?? 1) - 1, 0) % CATEGORY_COLOR_PRESETS.length;
  return CATEGORY_COLOR_PRESETS[index];
};

// The first preset no other category uses yet, for a new one.
export const nextFreeCategoryColor = (usedColors: string[]): string => {
  const used = usedColors.map((x) => x.toLowerCase());
  return (
    CATEGORY_COLOR_PRESETS.find((x) => !used.includes(x)) ??
    CATEGORY_COLOR_PRESETS[used.length % CATEGORY_COLOR_PRESETS.length]
  );
};

export const withAlpha = (color: string, alpha: number): string => {
  const hex = isCategoryColor(color) ? color.slice(1) : NO_CATEGORY_COLOR.slice(1);
  const r = parseInt(hex.slice(0, 2), 16);
  const g = parseInt(hex.slice(2, 4), 16);
  const b = parseInt(hex.slice(4, 6), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
};

// A label or a session in the colour: a light tint with a stronger edge, the text in
// the theme's own colour.
export const categoryTintStyle = (color: string): CSSProperties => ({
  background: withAlpha(color, 0.16),
  border: `1px solid ${withAlpha(color, 0.5)}`,
  color: "var(--text-color)",
});
