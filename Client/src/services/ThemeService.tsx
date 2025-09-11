import { RefObject } from "react";
import { PrimeTheme } from "../model/PrimeTheme";
import { LocalStorageService } from "./LocalStorageService";

export namespace ThemeService {
  let HTMLRef: RefObject<HTMLLinkElement>;

  export function setRef(ref: RefObject<HTMLLinkElement>) {
    HTMLRef = ref;
  }

  export const setDefaultTheme = () => {
    const localStorageThemeName = LocalStorageService.getThemeName();

    if (HTMLRef.current) {
      if (localStorageThemeName) {
        HTMLRef.current.href = new PrimeTheme(localStorageThemeName).themeURL;
      } else {
        HTMLRef.current.href = new PrimeTheme("bootstrap4-dark-blue").themeURL;
      }
    }
  };

  // export const setTheme = (primeTheme: PrimeTheme) => {
  //   if (HTMLRef.current) {
  //     HTMLRef.current.href = primeTheme.themeURL;
  //     LocalStorageService.setThemeName(primeTheme.themeName);
  //   }
  // };

  export const getDarkThemes = (): PrimeTheme[] => {
    const themes: PrimeTheme[] = [];

    themes.push(new PrimeTheme("lara-dark-purple"));
    themes.push(new PrimeTheme("lara-dark-blue"));
    themes.push(new PrimeTheme("lara-dark-indigo"));
    themes.push(new PrimeTheme("bootstrap4-dark-blue"));
    themes.push(new PrimeTheme("bootstrap4-dark-purple"));

    return themes;
  };

  export const getLightThemes = (): PrimeTheme[] => {
    const themes: PrimeTheme[] = [];

    themes.push(new PrimeTheme("lara-light-purple"));
    themes.push(new PrimeTheme("lara-light-blue"));
    themes.push(new PrimeTheme("lara-light-indigo"));
    themes.push(new PrimeTheme("bootstrap4-light-blue"));
    themes.push(new PrimeTheme("bootstrap4-light-purple"));

    return themes;
  };

  export const setDefaultThemeScale = () => {
    const localStorageThemeScale = LocalStorageService.getThemeScale();
    if (localStorageThemeScale) {
      document.documentElement.style.fontSize = `${localStorageThemeScale}px`;
    } else {
      document.documentElement.style.fontSize = `${14}px`;
    }
  };

  export const setThemeScale = (size: number) => {
    document.documentElement.style.fontSize = `${size}px`;
    LocalStorageService.setThemeScale(size.toString());
  };

  /**
   * Enhanced setTheme with optional callback (e.g., for FullCalendar updates after load).
   * Callback receives the extracted color palette.
   */
  export const setTheme = (
    primeTheme: PrimeTheme,
    onThemeLoaded?: (palette: { [key: string]: string }) => void
  ) => {
    if (HTMLRef.current) {
      HTMLRef.current.href = primeTheme.themeURL;
      LocalStorageService.setThemeName(primeTheme.themeName);

      // Wait for CSS to load, then extract palette and call callback
      const checkLoaded = () => {
        requestAnimationFrame(() => {
          const palette = primeTheme.getColorPalette();
          if (onThemeLoaded) {
            onThemeLoaded(palette);
          }
        });
      };
      // Trigger after a short delay to ensure CSS applies
      setTimeout(checkLoaded, 50);
    }
  };

  /**
   * Gets the current theme name and dynamically extracts its color palette.
   * Use this in components (e.g., FullCalendar) to get live vars without hardcoding.
   * Returns { primaryColor: '#3f82f6', surfaceCard: '#ffffff', ... }
   */
  export const getCurrentThemeColors = (): { [key: string]: string } => {
    const currentThemeName = LocalStorageService.getThemeName();
    if (!currentThemeName) {
      return {}; // Fallback empty
    }
    const currentTheme = new PrimeTheme(currentThemeName);
    return currentTheme.getColorPalette();
  };
}
