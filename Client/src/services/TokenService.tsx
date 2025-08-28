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

  public static getTokenClaims = (): TokenData | null => {
    const token = LocalStorageService.getAccessToken();
    if (!token) {
      return null;
    }

    try {
      const decodedToken = jwtDecode<TokenData>(token);
      return decodedToken;
    } catch (error) {
      console.error("Error decoding token:", error);
      return null;
    }
  };

  // Get specific claim by key
  public static getClaim = (claimKey: keyof TokenData): any => {
    const claims = TokenService.getTokenClaims();
    const result = claims ? claims[claimKey] : null;
    return result;
  };

  // Get role claims
  public static getRoleClaims = (): string[] | null => {
    return TokenService.getClaim("roleClaim") || null;
  };

  // Get user ID
  public static getUserId = (): string | null => {
    return TokenService.getClaim("Id") || null;
  };

  // Get username
  public static getUserName = (): string | null => {
    return TokenService.getClaim("userName") || null;
  };

  // Get RoleName
  public static getRoleName = (): string | null => {
    return TokenService.getClaim("RoleName") || null;
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
  };

  // Login
  public static setAccessToken = (value: string) =>
    LocalStorageService.setAccessToken(value);

  public static setRefreshToken = (value: string) =>
    LocalStorageService.setRefreshToken(value);

  public static setRefreshTokenExpireDate = (value: string) =>
    LocalStorageService.setRefreshTokenExpireDate(value);

  // Refresh tokens
  public static getUserRefreshTokenDto = (): UserRefreshTokenDto => ({
    accessToken: LocalStorageService.getAccessToken() ?? "",
    refreshToken: LocalStorageService.getRefreshToken() ?? "",
  });
}
