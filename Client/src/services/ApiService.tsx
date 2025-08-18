import { ApiResponse } from "../model/ApiResponse";
import { DataTableDto } from "../model/DataTableDto";
import { LookupDto } from "../model/lookup/LookupDto";
import { UserLoginRequestDto } from "../model/UserLoginRequestDto";
import { LocalStorageService } from "./LocalStorageService";
import { ToastService } from "./ToastService";
import { TokenService } from "./TokenService";

export default class ApiService {
  // static serverUrl = "https://alexps.gr/api/";
  static serverUrl = "/api/";
  // static serverUrl = "http://localhost:8080/api/";

  public static async get<TEntity>(
    controller: string,
    id: number | string
  ): Promise<TEntity | null> {
    const url = this.serverUrl + controller + "/" + id;
    return await this.apiRequest(url, "GET");
  }

  public static async getDataLookup(
    controller: string,
    data: LookupDto
  ): Promise<LookupDto | null> {
    const url = this.serverUrl + controller + "/Lookup";
    return await this.apiRequest(url, "POST", data);
  }

  public static async getDataGrid<TEntity>(
    controller: string,
    data: DataTableDto<TEntity>
  ): Promise<DataTableDto<TEntity> | null> {
    const url = this.serverUrl + controller + "/GetDataTable";
    return await this.apiRequest(url, "POST", data);
  }

  public static async create<TEntity>(
    controller: string,
    data: TEntity
  ): Promise<TEntity | null> {
    const url = this.serverUrl + controller;
    return await this.apiRequest(url, "POST", data);
  }

  public static async delete<TEntity>(
    controller: string,
    id: number | string
  ): Promise<TEntity | null> {
    const url = this.serverUrl + controller + "/" + id;
    return await this.apiRequest(url, "DELETE");
  }

  public static async update<TEntity>(
    controller: string,
    data: TEntity,
    id: number | string
  ): Promise<TEntity | null> {
    const url = this.serverUrl + controller + "/" + id;
    return await this.apiRequest(url, "PUT", data);
  }

  public static async login(data: UserLoginRequestDto, authLogin: () => void) {
    const url = this.serverUrl + "users/login";

    await this.apiRequest(url, "POST", data).then((result) => {
      if (result) {
        const expireDate = new Date(new Date().getTime() + 604800 * 1000);

        TokenService.setAccessToken(result.accessToken);
        TokenService.setRefreshToken(result.refreshToken);
        TokenService.setRefreshTokenExpireDate(expireDate.toString());
        authLogin();

        ToastService.showSuccess("Login was successfull!");
      } else {
        ToastService.showError("Login Failed!");
      }
    });
  }

  public static async register(
    data: UserLoginRequestDto,
    authLogin: () => void
  ) {
    const url = this.serverUrl + "users/register";
    await this.apiRequest(url, "POST", data).then((result) => {
      if (result) {
        ToastService.showInfo("ssss");
        this.login(data, authLogin);
      }
    });
  }

  public static async logout(authLogout: () => void) {
    const url = this.serverUrl + "users/logout";
    await this.apiRequest<ApiResponse<boolean>>(url, "POST").then((result) => {
      if (result) {
        TokenService.logout();
        authLogout();
        ToastService.showSuccess("Logout was successfull!");
      } else {
        ToastService.showError("Logout Failed!");
      }
    });
  }

  public static async test<TEntity>() {
    let url = this.serverUrl + "users/profile";
    await this.apiRequest<TEntity>(url, "POST");

    url = this.serverUrl + "tickets/5";
    await this.apiRequest<TEntity>(url, "GET");
  }

  private static async apiRequest<TEntity>(
    url: string,
    method: string,
    data?: TEntity
  ): Promise<TEntity | null> {
    let token = LocalStorageService.getAccessToken();
    const result = await this.apiFetch<TEntity>(url, method, token, data).then(
      (response) => {
        if (!response?.data || !response?.isSucceed) {
          if (response?.messages) {
            // Loop in dictionary to display errors.
            Object.keys(response.messages).forEach((key) =>
              response.messages[key].forEach((value) =>
                ToastService.showError(value)
              )
            );
          }
          return response?.isSucceed;
        }

        // Loop in dictionary to display errors.
        Object.keys(response.messages).forEach((key) =>
          response.messages[key].forEach((value) =>
            ToastService.showSuccess(value)
          )
        );
        return response.data;
      }
    );

    return result;
  }

  private static async refreshUserToken(): Promise<boolean> {
    const url = this.serverUrl + "users/refreshToken";
    const refreshTokenDto = TokenService.getUserRefreshTokenDto();
    return await this.apiFetch(url, "POST", null, refreshTokenDto).then(
      (response) => {
        if (!response || !response.data) {
          return false;
        }

        // authLogin();
        TokenService.setAccessToken(response.data.accessToken);
        TokenService.setRefreshToken(response.data.refreshToken);
        TokenService.setRefreshTokenExpireDate(response.toString());
        return true;
      }
    );
  }

  private static async handle401<TEntity>(
    url: string,
    method: string,
    token?: string | null,
    data?: TEntity | null
  ): Promise<ApiResponse<TEntity> | null> {
    // Refresh Token Expired!
    if (TokenService.isRefreshTokenExpired()) {
      ToastService.showWarn("Token expired. Login Required.");
      TokenService.logout();
      return null;
    }

    // Access Token Expired!
    // Try to renew token and re-execute earlier query.
    if (TokenService.isTokenExpired()) {
      ToastService.showInfo("Token expired. Trying to renew.");
      return this.refreshUserToken().then((isSuccess) => {
        if (isSuccess) {
          ToastService.showInfo("Renewal success. Re-executing query.");

          token = LocalStorageService.getAccessToken();
          return this.apiFetch(url, method, token, data);
        }
        return null;
      });
    }
    return null;
  }

  private static async apiFetch<TEntity>(
    url: string,
    method: string,
    token?: string | null,
    data?: TEntity | null
  ): Promise<ApiResponse<TEntity> | null> {
    try {
      const response: Response = await fetch(url, {
        method: method,
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        credentials: "include",
        // body: JSON.stringify(data),
        body: data ? JSON.stringify(data) : undefined, // Avoid sending body for GET requests
      });

      // Check Refresh and Access Tokens!
      if (response.status === 401) {
        return this.handle401(url, method, token, data);
      }

      if (response.ok) {
        const result = (await response.json()) as Promise<ApiResponse<TEntity>>;
        return result;
      }

      ToastService.showError(
        "Something unexpected happend! API call was not successfull..."
      );
      console.log(1);
      return null;
    } catch (error) {
      ToastService.showError(
        "Something unexpected happend! API call was not successfull..."
      );
      console.error(error);
      return null;
    }
  }
}
