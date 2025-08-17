export class LocalStorageService {
  //
  //    Retrive data.
  //
  public static getThemeName = (): string | null =>
    this.returnValue("themeName");

  public static getThemeScale = (): string | null =>
    this.returnValue("themeScale");

  public static getAccessToken = (): string | null =>
    this.returnValue("accessToken");

  public static getRefreshToken = (): string | null =>
    this.returnValue("refreshToken");

  public static getRefreshTokenExpireDate = (): string | null =>
    this.returnValue("refreshTokenExpireDate");

  //
  //    Set data.
  //
  public static setThemeScale = (value: string = "") =>
    localStorage.setItem("themeScale", value);

  public static setThemeName = (value: string = "") =>
    localStorage.setItem("themeName", value);

  public static setAccessToken = (value: string = "") =>
    localStorage.setItem("accessToken", value);

  public static setRefreshToken = (value: string = "") =>
    localStorage.setItem("refreshToken", value);

  public static setRefreshTokenExpireDate = (value: string = "") =>
    localStorage.setItem("refreshTokenExpireDate", value);

  private static returnValue = (fieldName: string) => {
    const value = localStorage.getItem(fieldName);
    if (!value) {
      return null;
    }

    return value.length > 0 ? value : null;
  };
}
