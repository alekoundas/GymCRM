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
}
