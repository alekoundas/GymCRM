import { jwtDecode } from "jwt-decode";
import { LocalStorageService } from "./LocalStorageService";
import { UserRefreshTokenDto } from "../model/entities/user/UserRefreshTokenDto";

interface TokenData {
  exp?: number;
  Id?: string;
  userName?: string;
  RoleName?: string;
  roleClaim?: string[];
  Permission?: string[];
}

export class TokenService {
  public static isRefreshTokenExpired = (): boolean => {
    const expireDateString = LocalStorageService.getRefreshTokenExpireDate();
    if (!expireDateString) {
      return true;
    }

    const expireDate = new Date(expireDateString);

    return new Date() > expireDate;
  };

  public static isTokenExpired = (): boolean => {
    const token = LocalStorageService.getAccessToken();
    if (!token) {
      return false;
    }

    try {
      const decodedToken = jwtDecode<TokenData>(token);

      // Check if the token has an 'exp' claim
      if (decodedToken.exp) {
        // Convert 'exp' (seconds) to milliseconds and compare with the current time
        const expirationDate = new Date(decodedToken.exp * 1000);
        const currentDate = new Date();

        return currentDate > expirationDate;
      }

      return false;
    } catch (error) {
      console.error("Error decoding token:", error);
      return true; // If there's an error decoding the token, assume it's invalid/expired
    }
  };

  public static getTokenClaims = (): TokenData | undefined => {
    const token = LocalStorageService.getAccessToken();
    if (!token) {
      return undefined;
    }

    try {
      const decodedToken = jwtDecode<TokenData>(token);
      return decodedToken;
    } catch (error) {
      console.error("Error decoding token:", error);
      return undefined;
    }
  };

  // Get specific claim by key
  public static getClaim = (claimKey: keyof TokenData): any => {
    const claims = TokenService.getTokenClaims();
    const result = claims ? claims[claimKey] : undefined;
    return result;
  };

  // Get role claims
  public static getRoleClaims = (): string[] | undefined => {
    return TokenService.getClaim("roleClaim") || undefined;
  };

  // Get user ID
  public static getUserId = (): string | undefined => {
    return TokenService.getClaim("Id") || undefined;
  };

  // Get username
  public static getUserName = (): string | undefined => {
    return TokenService.getClaim("userName") || undefined;
  };

  // Get RoleName
  public static getRoleName = (): string | undefined => {
    return TokenService.getClaim("RoleName") || undefined;
  };

  // Is user allowed
  public static isUserAllowed = (claim: string): boolean => {
    const permissions: string[] = TokenService.getClaim("Permission");
    if (permissions) {
      const isAllowed = permissions
        .map((x) => x.toLocaleLowerCase())
        .includes(claim.toLowerCase());
      return isAllowed;
    }

    return false;
  };

  // Logout
  public static logout = () => {
    LocalStorageService.setRefreshToken();
    LocalStorageService.setAccessToken();
    LocalStorageService.setRefreshTokenExpireDate();
    LocalStorageService.setProfileImage();
    LocalStorageService.setFirstName();
    LocalStorageService.setLastName();
  };

  // Login
  public static setAccessToken = (value: string) =>
    LocalStorageService.setAccessToken(value);

  public static setRefreshToken = (value: string) =>
    LocalStorageService.setRefreshToken(value);

  public static setRefreshTokenExpireDate = (value: string) =>
    LocalStorageService.setRefreshTokenExpireDate(value);

  public static setProfileImage = (value: string) =>
    LocalStorageService.setProfileImage(value);

  public static setFirstName = (value: string) =>
    LocalStorageService.setFirstName(value);

  public static setLastName = (value: string) =>
    LocalStorageService.setLastName(value);

  // Refresh tokens
  public static getUserRefreshTokenDto = (): UserRefreshTokenDto => ({
    accessToken: LocalStorageService.getAccessToken() ?? "",
    refreshToken: LocalStorageService.getRefreshToken() ?? "",
  });
}
