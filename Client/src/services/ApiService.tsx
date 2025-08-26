import { ApiResponse } from "../model/ApiResponse";
import { DataTableDto } from "../model/datatable/DataTableDto";
import { LookupDto } from "../model/lookup/LookupDto";
import { TimeSlotRequestDto } from "../model/TimeSlotRequestDto";
import { TimeSlotResponseDto } from "../model/TimeSlotResponseDto";
import { UserLoginRequestDto } from "../model/UserLoginRequestDto";
import { UserRefreshTokenDto } from "../model/UserRefreshTokenDto";
import { UserRegisterRequestDto } from "../model/UserRegisterRequestDto";
import { LocalStorageService } from "./LocalStorageService";
import { ToastService } from "./ToastService";
import { TokenService } from "./TokenService";

export default class ApiService {
  static serverUrl = "/api/";

  public static async get<TEntity>(
    controller: string,
    id: number | string
  ): Promise<TEntity | null> {
    const url = this.serverUrl + controller + "/" + id;
    return await this.apiRequest<TEntity, TEntity>(url, "GET");
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

  public static async timeslots(
    endpoint: string,
    data: TimeSlotRequestDto
  ): Promise<TimeSlotResponseDto[] | null> {
    const url = this.serverUrl + endpoint;
    return await this.apiRequest<TimeSlotRequestDto, TimeSlotResponseDto[]>(
      url,
      "POST",
      data
    );
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

  public static async login(
    data: UserLoginRequestDto,
    authLogin: () => void
  ): Promise<boolean> {
    const url = this.serverUrl + "users/login";

    return await this.apiRequest<UserLoginRequestDto, UserLoginRequestDto>(
      url,
      "POST",
      data
    ).then((result) => {
      if (result) {
        const expireDate = new Date(new Date().getTime() + 604800 * 1000);

        TokenService.setAccessToken(result.accessToken);
        TokenService.setRefreshToken(result.refreshToken);
        TokenService.setRefreshTokenExpireDate(expireDate.toString());
        authLogin();
        var dsfsdf = TokenService.getUserName();
        ToastService.showSuccess("Login was successfull!");
        return true;
      } else {
        ToastService.showError("Login Failed!");
        return false;
      }
    });
  }

  public static async register(
    data: UserRegisterRequestDto,
    authLogin: () => void
  ): Promise<boolean> {
    const url = this.serverUrl + "users/register";
    return await this.apiRequest(url, "POST", data).then((result) => {
      if (result) {
        var loginDto = new UserLoginRequestDto();
        loginDto.userNameOrEmail = data.email;
        loginDto.password = data.password;
        ToastService.showSuccess("Register was successfull!");
        this.login(loginDto, authLogin);

        return true;
      }
      return false;
    });
  }

  public static async logout(authLogout: () => void) {
    const url = this.serverUrl + "users/logout";
    await this.apiRequest<ApiResponse<boolean>, ApiResponse<boolean>>(
      url,
      "POST"
    ).then((result) => {
      TokenService.logout();
      authLogout();
      if (result) {
        ToastService.showSuccess("Logout was successfull!");
      } else {
        ToastService.showError("Logout Failed!");
      }
    });
  }

  private static async apiRequest<TRequest, TResponse>(
    url: string,
    method: string,
    data?: TRequest
  ): Promise<TResponse | null> {
    const token = LocalStorageService.getAccessToken();
    const response = await this.apiFetch<TRequest, TResponse>(
      url,
      method,
      token,
      data
    );

    if (!response || !response.isSucceed) {
      if (response?.messages) {
        // Display error messages
        Object.keys(response.messages).forEach((key) =>
          response.messages[key].forEach((value) =>
            ToastService.showError(value)
          )
        );
      }
      return null; // Explicitly return null for failed requests
    }

    // Display success messages
    if (response.messages) {
      Object.keys(response.messages).forEach((key) =>
        response.messages[key].forEach((value) =>
          ToastService.showSuccess(value)
        )
      );
    }

    return response.data ?? null; // Return data or null if data is undefined
  }

  // private static async refreshUserToken(): Promise<boolean> {
  //   const url = this.serverUrl + "users/refreshToken";
  //   var aasdasdf = TokenService.getUserName();
  //   const refreshTokenDto = TokenService.getUserRefreshTokenDto();
  //   return await this.apiFetch<UserRefreshTokenDto, UserRefreshTokenDto>(
  //     url,
  //     "POST",
  //     null,
  //     refreshTokenDto
  //   ).then((response) => {
  //     if (!response || !response.data) {
  //       response?.messages["error"].forEach((x) => ToastService.showError(x));
  //       //TODO: ennable this to switch to loged out mode
  //       // TokenService.logout();
  //       // authLogout();
  //       return false;
  //     }

  //     // authLogin();
  //     TokenService.setAccessToken(response.data.accessToken);
  //     TokenService.setRefreshToken(response.data.refreshToken);
  //     TokenService.setRefreshTokenExpireDate(response.toString());
  //     return true;
  //   });
  // }

  private static async refreshUserToken(): Promise<boolean> {
    const url = this.serverUrl + "users/refreshToken";
    const refreshTokenDto = TokenService.getUserRefreshTokenDto();

    // Use the accessToken from refreshTokenDto for the Authorization header
    const token = refreshTokenDto.accessToken;

    if (!token) {
      console.error("No access token available for refresh.");
      ToastService.showError("No access token available. Please log in again.");
      TokenService.logout();
      return false;
    }

    return await this.apiFetch<UserRefreshTokenDto, UserRefreshTokenDto>(
      url,
      "POST",
      token, // Pass the access token instead of null
      refreshTokenDto
    ).then((response) => {
      if (!response || !response.data) {
        response?.messages["error"]?.forEach((x) => ToastService.showError(x));
        return false;
      }

      TokenService.setAccessToken(response.data.accessToken);
      TokenService.setRefreshToken(response.data.refreshToken);
      // Set refresh token expiration (7 days from now, matching server)
      const expireDate = new Date(new Date().getTime() + 604800 * 1000);
      TokenService.setRefreshTokenExpireDate(expireDate.toString());
      ToastService.showSuccess("Token refreshed successfully!");
      return true;
    });
  }

  private static async handle401<TRequest, Tresponse>(
    url: string,
    method: string,
    token?: string | null,
    data?: TRequest | null
  ): Promise<ApiResponse<Tresponse> | null> {
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

  private static async handle400<TEntity>(
    response?: ApiResponse<TEntity> | null
  ): Promise<ApiResponse<TEntity> | null> {
    response?.messages["error"].forEach((x) => ToastService.showError(x));

    return null;
  }

  private static async apiFetch<TRequest, TResponse>(
    url: string,
    method: string,
    token?: string | null,
    data?: TRequest | null
  ): Promise<ApiResponse<TResponse> | null> {
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

      if (response.status === 400) {
        const result = (await response.json()) as ApiResponse<TResponse>;
        return this.handle400(result);
      }

      if (response.ok) {
        const result = (await response.json()) as Promise<
          ApiResponse<TResponse>
        >;
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
