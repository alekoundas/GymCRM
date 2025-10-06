import { DataTableDto } from "../model/datatable/DataTableDto";
import { UserPasswordChangeDto } from "../model/entities/user/UserPasswordChangeDto";
import { UserLoginRequestDto } from "../model/entities/user/UserLoginRequestDto";
import { UserRefreshTokenDto } from "../model/entities/user/UserRefreshTokenDto";
import { UserRegisterDto } from "../model/entities/user/UserRegisterDto";
import { LookupDto } from "../model/lookup/LookupDto";
import { TimeSlotRequestDto } from "../model/TimeSlotRequestDto";
import { TimeSlotResponseDto } from "../model/TimeSlotResponseDto";
import { LocalStorageService } from "./LocalStorageService";
import { ToastService } from "./ToastService";
import { TokenService } from "./TokenService";
import { UserPasswordResetDto } from "../model/entities/user/UserPasswordResetDto";
import { UserPasswordForgotDto } from "../model/entities/user/UserPasswordForgotDto";
import { AutoCompleteDto } from "../model/core/auto-complete/AutoCompleteDto";
import { MailSendDto } from "../model/entities/mail/MailSendDto";
import { ChartData } from "../model/core/chart/ChartData";
import { TrainGroupParticipantUpdateDto } from "../model/entities/train-group-participant/TrainGroupParticipantUpdateDto";
import { ApiResponseDto } from "../model/core/api-response/ApiResponseDto";

export default class ApiService {
  private static readonly BASE_URL = "/api/";
  private static readonly TOKEN_EXPIRATION_MS = 604800 * 1000; // 7 days
  private static refreshPromise: Promise<boolean> | null = null;

  private static buildUrl(
    controller: string,
    endpoint: string = "",
    id?: number | string
  ): string {
    let url = `${this.BASE_URL}${controller}`;
    if (id) url += `/${id}`;
    if (endpoint) url += `/${endpoint}`;
    return url;
  }

  private static showMessages(
    messages: Record<string, string[]> | undefined,
    isSuccess: boolean
  ) {
    if (!messages) return;
    const allMessages = Object.values(messages).flat();
    allMessages.forEach((msg) =>
      isSuccess ? ToastService.showSuccess(msg) : ToastService.showError(msg)
    );
  }

  private static async apiFetch<TRequest, TResponse>(
    url: string,
    method: string,
    data?: TRequest,
    retries = 1
  ): Promise<ApiResponseDto<TResponse> | null> {
    const token = LocalStorageService.getAccessToken();
    const language = LocalStorageService.getLanguage();
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 10000); // 10s timeout

    try {
      const response = await fetch(url, {
        method,
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
          "Accept-Language": language ?? "",
        },
        credentials: "include",
        body: data ? JSON.stringify(data) : undefined,
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      if (response.status === 401) {
        return this.handle401(url, method, token, data);
      }

      if (response.status === 400) {
        const result = (await response.json()) as ApiResponseDto<TResponse>;
        this.showMessages(result.messages, false);
        return null;
      }

      if (response.ok) {
        return (await response.json()) as ApiResponseDto<TResponse>;
      }

      if (response.status >= 500 && retries > 0) {
        await new Promise((resolve) => setTimeout(resolve, 1000)); // Wait 1s before retry
        return this.apiFetch(url, method, data, retries - 1);
      }

      ToastService.showError(`API call failed with status ${response.status}`);
      return null;
    } catch (error) {
      clearTimeout(timeoutId);
      if (error instanceof DOMException && error.name === "AbortError") {
        ToastService.showError("Request timed out.");
        return null;
      }
      if (error instanceof TypeError && retries > 0) {
        await new Promise((resolve) => setTimeout(resolve, 1000));
        return this.apiFetch(url, method, data, retries - 1);
      }
      ToastService.showError("Network or unexpected error during API call.");
      console.error(error);
      return null;
    }
  }

  private static async apiRequest<TRequest, TResponse>(
    url: string,
    method: string,
    data?: TRequest
  ): Promise<TResponse | null> {
    const response = await this.apiFetch<TRequest, TResponse>(
      url,
      method,
      data
    );
    if (!response || !response.isSucceed) {
      this.showMessages(response?.messages, false);
      return null;
    }
    this.showMessages(response.messages, true);
    return response.data ?? null;
  }

  private static async handle401<TRequest, TResponse>(
    url: string,
    method: string,
    token: string | null | undefined,
    data?: TRequest
  ): Promise<ApiResponseDto<TResponse> | null> {
    if (TokenService.isRefreshTokenExpired()) {
      ToastService.showWarn("Token expired. Login required.");
      TokenService.logout();
      return null;
    }

    // TODO find out what is wrong with this in published
    // if (TokenService.isTokenExpired()) {
    ToastService.showInfo("Token expired. Attempting to renew.");
    const isSuccess = await this.refreshUserToken();
    if (isSuccess) {
      ToastService.showInfo("Token renewed. Retrying request.");
      return this.apiFetch(url, method, data);
    }
    console.warn("Token refresh failed.");
    return null;
  }

  private static async refreshUserToken(): Promise<boolean> {
    if (this.refreshPromise) {
      console.debug("Reusing existing token refresh promise.");
      return this.refreshPromise;
    }

    this.refreshPromise = (async () => {
      try {
        const url = this.buildUrl("Auth", "refreshToken");
        const refreshTokenDto = TokenService.getUserRefreshTokenDto();
        const token = refreshTokenDto.accessToken;
        const refreshToken = refreshTokenDto.refreshToken;

        if (!token || !refreshToken) {
          ToastService.showError(
            "Invalid or missing token. Please log in again."
          );
          TokenService.logout();
          return false;
        }

        console.debug("Sending refresh token request:", {
          url,
          refreshTokenDto,
        });
        const response = await this.apiFetch<
          UserRefreshTokenDto,
          UserRefreshTokenDto
        >(url, "POST", refreshTokenDto);
        if (!response || !response.data) {
          this.showMessages(response?.messages, false);
          return false;
        }

        const expireDate = new Date(Date.now() + this.TOKEN_EXPIRATION_MS);
        TokenService.setAccessToken(response.data.accessToken);
        TokenService.setRefreshToken(response.data.refreshToken);
        TokenService.setRefreshTokenExpireDate(expireDate.toISOString()); // Use ISO format for UTC
        // TokenService.setRefreshTokenExpireDate(expireDate.toString());
        console.debug(
          "Token refresh successful, new expiration:",
          expireDate.toISOString()
        );
        ToastService.showSuccess("Token refreshed successfully!");
        return true;
      } catch (error) {
        console.error("Unexpected error during token refresh:", error);
        ToastService.showError("Failed to refresh token. Please log in again.");
        return false;
      } finally {
        this.refreshPromise = null;
      }
    })();

    return this.refreshPromise;
  }

  public static async get<TEntity>(
    controller: string,
    id: number | string
  ): Promise<TEntity | null> {
    const url = this.buildUrl(controller, "", id);
    return this.apiRequest<TEntity, TEntity>(url, "GET");
  }

  public static async getDataLookup(
    controller: string,
    data: LookupDto
  ): Promise<LookupDto | null> {
    const url = this.buildUrl(controller, "Lookup");
    return this.apiRequest<LookupDto, LookupDto>(url, "POST", data);
  }
  public static async getDataAutoComplete<TEntity>(
    controller: string,
    data: AutoCompleteDto<TEntity>
  ): Promise<AutoCompleteDto<TEntity> | null> {
    const url = this.buildUrl(controller, "AutoComplete");
    return this.apiRequest<AutoCompleteDto<TEntity>, AutoCompleteDto<TEntity>>(
      url,
      "POST",
      data
    );
  }

  public static async getDataGrid<TEntity>(
    controller: string,
    data: DataTableDto<TEntity>
  ): Promise<DataTableDto<TEntity> | null> {
    const url = this.buildUrl(controller, "GetDataTable");
    return this.apiRequest<DataTableDto<TEntity>, DataTableDto<TEntity>>(
      url,
      "POST",
      data
    );
  }

  public static async create<TEntity>(
    controller: string,
    data: TEntity
  ): Promise<TEntity[] | null> {
    const url = this.buildUrl(controller);
    return this.apiRequest<TEntity[], TEntity[]>(url, "POST", [data]);
  }

  public static async createRange<TEntity>(
    controller: string,
    data: TEntity[]
  ): Promise<TEntity[] | null> {
    const url = this.buildUrl(controller);
    return this.apiRequest<TEntity[], TEntity[]>(url, "POST", data);
  }

  public static async update<TEntity>(
    controller: string,
    data: TEntity,
    id: number | string
  ): Promise<TEntity | null> {
    const url = this.buildUrl(controller, "", id);
    return this.apiRequest<TEntity, TEntity>(url, "PUT", data);
  }

  public static async delete<TEntity>(
    controller: string,
    id: number | string
  ): Promise<TEntity | null> {
    const url = this.buildUrl(controller, "", id);
    return this.apiRequest<TEntity, TEntity>(url, "DELETE");
  }

  public static async timeslots(
    endpoint: string,
    data: TimeSlotRequestDto
  ): Promise<TimeSlotResponseDto[] | null> {
    const url = this.buildUrl(endpoint);
    return this.apiRequest<TimeSlotRequestDto, TimeSlotResponseDto[]>(
      url,
      "POST",
      data
    );
  }

  public static async updateParticipants(
    data: TrainGroupParticipantUpdateDto
  ): Promise<TrainGroupParticipantUpdateDto | null> {
    const url = this.buildUrl("TrainGroupParticipants/UpdateParticipants");
    return this.apiRequest<
      TrainGroupParticipantUpdateDto,
      TrainGroupParticipantUpdateDto
    >(url, "POST", data);
  }

  public static async passwordForgot(
    data: UserPasswordForgotDto
  ): Promise<boolean> {
    const url = this.buildUrl("Auth", "PasswordForgot");
    const result = await this.apiRequest<UserPasswordForgotDto, boolean>(
      url,
      "POST",
      data
    );

    return result ?? false;
  }

  public static async passwordReset(
    data: UserPasswordResetDto
  ): Promise<boolean> {
    const url = this.buildUrl("Auth", "PasswordReset");
    const result = await this.apiRequest<UserPasswordResetDto, boolean>(
      url,
      "POST",
      data
    );

    return result ?? false;
  }

  public static async passwordChange(
    data: UserPasswordChangeDto
  ): Promise<boolean> {
    const url = this.buildUrl("Auth", "PasswordChange");
    const result = await this.apiRequest<UserPasswordChangeDto, boolean>(
      url,
      "POST",
      data
    );

    return result ?? false;
  }

  public static async login(
    data: UserLoginRequestDto,
    authLogin: () => void
  ): Promise<boolean> {
    const url = this.buildUrl("Auth", "login");
    const result = await this.apiRequest<
      UserLoginRequestDto,
      UserLoginRequestDto
    >(url, "POST", data);
    if (!result) {
      ToastService.showError("Login failed!");
      return false;
    }

    const expireDate = new Date(Date.now() + this.TOKEN_EXPIRATION_MS);
    TokenService.setAccessToken(result.accessToken);
    TokenService.setRefreshToken(result.refreshToken);
    // TokenService.setRefreshTokenExpireDate(expireDate.toString());
    TokenService.setRefreshTokenExpireDate(expireDate.toISOString()); // Use ISO format for UTC
    console.debug(
      "Login successful, tokens set, expiration:",
      expireDate.toISOString()
    );
    authLogin();
    ToastService.showSuccess("Login successful!");
    return true;
  }

  public static async register(
    data: UserRegisterDto,
    authLogin: () => void
  ): Promise<boolean> {
    const url = this.buildUrl("Auth", "register");
    const result = await this.apiRequest(url, "POST", data);
    if (!result) return false;

    const loginDto = new UserLoginRequestDto();
    loginDto.userNameOrEmail = data.email;
    loginDto.password = data.password;
    ToastService.showSuccess("Registration successful!");
    return this.login(loginDto, authLogin);
  }

  public static async logout(authLogout: () => void): Promise<void> {
    const url = this.buildUrl("Auth", "logout");
    const result = await this.apiRequest<
      ApiResponseDto<boolean>,
      ApiResponseDto<boolean>
    >(url, "POST");
    if (result) {
      ToastService.showSuccess("Logout successful!");
    }
    // else {
    //   ToastService.showError("Logout failed!");
    // }
    TokenService.logout();
    authLogout();
  }

  public static async emailSend(data: MailSendDto): Promise<boolean> {
    const url = this.buildUrl("mails", "Send");
    const result = await this.apiRequest<MailSendDto, boolean>(
      url,
      "POST",
      data
    );

    return result ?? false;
  }

  public static async getChartData(): Promise<ChartData | null> {
    const url = this.buildUrl("charts");
    return this.apiRequest<null, ChartData>(url, "GET");
  }
}
