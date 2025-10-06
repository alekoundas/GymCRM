import { PrimeTheme } from "../model/PrimeTheme";
import { LocalStorageService } from "./LocalStorageService";

export default class ThemeService {
  public static setDefaultTheme() {
    const localStorageThemeName = LocalStorageService.getThemeName();
    const defaultThemeName = localStorageThemeName || "lara-light-blue";

    LocalStorageService.setThemeName(defaultThemeName);
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
