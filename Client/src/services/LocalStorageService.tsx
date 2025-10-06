export class LocalStorageService {
  //
  //    Retrive data.
  //
  public static getThemeName = (): string | undefined =>
    this.returnValue("themeName");

  public static getThemeScale = (): string | undefined =>
    this.returnValue("themeScale");

  public static getAccessToken = (): string | undefined =>
    this.returnValue("accessToken");

  public static getRefreshToken = (): string | undefined =>
    this.returnValue("refreshToken");

  public static getRefreshTokenExpireDate = (): string | undefined =>
    this.returnValue("refreshTokenExpireDate");

  public static getLanguage = (): string | undefined =>
    this.returnValue("language");

  //
  //    Set data.
  //
  public static setThemeScale = (value: string = "") =>
    localStorage.setItem("themeScale", value);

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

  private static returnValue = (fieldName: string): string | undefined => {
    const value = localStorage.getItem(fieldName);
    if (!value || value.length === 0) {
      return undefined;
    }

    return value.length > 0 ? value : undefined;
  };
}
