export class PrimeTheme {
  themeName: string = "";
  themeURL: string = "";
  themeImage: string = "";

  public constructor(theme: string) {
    this.themeURL = `/themes/${theme}/theme.css`;
    this.themeName = theme;

    if (theme.startsWith("bootstrap4"))
      this.themeImage = "/theme-images/" + theme + ".svg";

    if (theme.startsWith("lara"))
      this.themeImage = "/theme-images/" + theme + ".svg";
  }

  /**
   * Dynamically extracts key CSS variables from the active theme.
   * Call this after the theme <link> has loaded (e.g., in useEffect or after setTheme).
   * Returns an object with vars like { primaryColor: '#3f82f6', surfaceCard: '#ffffff', ... }
   * Expand the 'vars' array for more (e.g., add '--text-color-secondary').
   */
  public getColorPalette(): { [key: string]: string } {
    const root = document.documentElement;
    const computed = getComputedStyle(root);
    const palette: { [key: string]: string } = {};

    // Key vars for FullCalendar (primary for events, surface for bg, text for labels, etc.)
    const vars = [
      { cssVar: "--primary-color", key: "primaryColor" },
      { cssVar: "--primary-50", key: "primary50" }, // Lighter variant for hovers
      { cssVar: "--primary-600", key: "primary600" }, // Darker for hovers
      { cssVar: "--surface-ground", key: "surfaceGround" }, // Overall bg
      { cssVar: "--surface-card", key: "surfaceCard" }, // Card/event bg
      { cssVar: "--surface-section", key: "surfaceSection" }, // Header bg
      { cssVar: "--surface-border", key: "surfaceBorder" }, // Borders/grid lines
      { cssVar: "--text-color", key: "textColor" }, // Primary text
      { cssVar: "--text-color-secondary", key: "textColorSecondary" }, // Secondary text
      { cssVar: "--font-size", key: "fontSize" }, // For scaling if needed
    ];

    vars.forEach(({ cssVar, key }) => {
      const value = computed.getPropertyValue(cssVar).trim();
      if (value) {
        palette[key] = value;
      }
    });

    return palette;
  }
}
