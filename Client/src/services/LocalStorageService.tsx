export class LocalStorageService {
  //
  //    Retrive data.
  //
  public static getThemeScale = (): number | undefined =>
    this.returnNumber("themeScale");

  public static getThemeName = (): string | undefined =>
    this.returnString("themeName");

  public static getLanguage = (): string | undefined =>
    this.returnString("language");

  public static getAccessToken = (): string | undefined =>
    this.returnString("accessToken");

  public static getRefreshToken = (): string | undefined =>
    this.returnString("refreshToken");

  public static getRefreshTokenExpireDate = (): string | undefined =>
    this.returnString("refreshTokenExpireDate");

  //
  //    Set data.
  //
  public static setThemeScale = (value: number) =>
    localStorage.setItem("themeScale", value.toString());

  public static setThemeName = (value: string = "") =>
    localStorage.setItem("themeName", value);

  public static setLanguage = (value: string = "") =>
    localStorage.setItem("language", value);

  public static setAccessToken = (value: string = "") =>
    localStorage.setItem("accessToken", value);

  public static setRefreshToken = (value: string = "") =>
    localStorage.setItem("refreshToken", value);

  public static setRefreshTokenExpireDate = (value: string = "") =>
    localStorage.setItem("refreshTokenExpireDate", value);

  //
  //  Retrieve data logic.
  //
  private static returnString = (fieldName: string): string | undefined => {
    const value = localStorage.getItem(fieldName);
    if (!value || value.length === 0) {
      return undefined;
    }

    return value.length > 0 ? value : undefined;
  };

  private static returnNumber = (fieldName: string): number | undefined => {
    const value = localStorage.getItem(fieldName);
    if (!value || value.length === 0) {
      return undefined;
    }

    return +value;
  };
}
