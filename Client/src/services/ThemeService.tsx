// Updated ThemeService.tsx – Class-based with static methods, no hooks inside setTheme
import { PrimeTheme } from "../model/PrimeTheme";
import { LocalStorageService } from "./LocalStorageService";

export default class ThemeService {
  public static setDefaultTheme() {
    const localStorageThemeName = LocalStorageService.getThemeName();
    const defaultThemeName = localStorageThemeName || "lara-light-blue"; // Match your initial link

    LocalStorageService.setThemeName(defaultThemeName); // Ensure stored
  }

  public static getDarkThemes(): PrimeTheme[] {
    const themes: PrimeTheme[] = [];

    themes.push(new PrimeTheme("lara-dark-purple"));
    themes.push(new PrimeTheme("lara-dark-blue"));
    themes.push(new PrimeTheme("lara-dark-indigo"));
    themes.push(new PrimeTheme("bootstrap4-dark-blue"));
    themes.push(new PrimeTheme("bootstrap4-dark-purple"));

    return themes;
  }

  public static getLightThemes(): PrimeTheme[] {
    const themes: PrimeTheme[] = [];

    themes.push(new PrimeTheme("lara-light-purple"));
    themes.push(new PrimeTheme("lara-light-blue"));
    themes.push(new PrimeTheme("lara-light-indigo"));
    themes.push(new PrimeTheme("bootstrap4-light-blue"));
    themes.push(new PrimeTheme("bootstrap4-light-purple"));

    return themes;
  }

  public static setDefaultThemeScale() {
    const localStorageThemeScale = LocalStorageService.getThemeScale();
    if (localStorageThemeScale) {
      document.documentElement.style.fontSize = `${localStorageThemeScale}px`;
    } else {
      // document.documentElement.style.fontSize = `${14}px`;
    }
  }

  public static setThemeScale(size: number) {
    document.documentElement.style.fontSize = `${size}px`;
    LocalStorageService.setThemeScale(size.toString());
  }

  /**
   * Enhanced setTheme – Pass changeTheme from your component's useContext.
   * Call from components only (e.g., onClick handler).
   * Callback for post-load actions (e.g., update FullCalendar).
   */
  public static setTheme(
    newThemeName: string,
    changeTheme?: (
      from?: string,
      to?: string,
      linkElementId?: string,
      onFinish?: () => void
    ) => void
  ) {
    if (!changeTheme) {
      console.warn(
        "changeTheme function not provided – theme switch skipped. Ensure called from a component with PrimeReactContext."
      );
      return;
    }

    const currentThemeName = LocalStorageService.getThemeName();
    const newTheme = new PrimeTheme(newThemeName);

    changeTheme(
      currentThemeName || "bootstrap4-dark-blue", // Optional 'from'
      newThemeName, // Optional 'to'
      "theme-link", // Optional linkElementId
      () => {
        LocalStorageService.setThemeName(newThemeName);
        const palette = newTheme.getColorPalette();

        document.dispatchEvent(
          new CustomEvent("primeThemeChange", {
            detail: { palette },
          })
        );
      }
    );
  }

  /**
   * Gets the current theme name and dynamically extracts its color palette.
   */
  public static getCurrentThemeColors(): { [key: string]: string } {
    const currentThemeName = LocalStorageService.getThemeName();
    if (!currentThemeName) {
      return {};
    }
    const currentTheme = new PrimeTheme(currentThemeName);
    return currentTheme.getColorPalette();
  }
}
