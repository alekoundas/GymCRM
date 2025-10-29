import { useCallback } from "react";
import { useToast } from "../contexts/ToastContext";
import { DataTableDto } from "../model/datatable/DataTableDto";
import { UserPasswordChangeDto } from "../model/entities/user/UserPasswordChangeDto";
import { UserLoginRequestDto } from "../model/entities/user/UserLoginRequestDto";
import { UserRefreshTokenDto } from "../model/entities/user/UserRefreshTokenDto";
import { UserRegisterDto } from "../model/entities/user/UserRegisterDto";
import { LookupDto } from "../model/lookup/LookupDto";
import { TimeSlotRequestDto } from "../model/TimeSlotRequestDto";
import { TimeSlotResponseDto } from "../model/TimeSlotResponseDto";
import { LocalStorageService } from "./LocalStorageService";
import { TokenService } from "./TokenService";
import { UserPasswordResetDto } from "../model/entities/user/UserPasswordResetDto";
import { UserPasswordForgotDto } from "../model/entities/user/UserPasswordForgotDto";
import { AutoCompleteDto } from "../model/core/auto-complete/AutoCompleteDto";
import { MailSendDto } from "../model/entities/mail/MailSendDto";
import { ChartData } from "../model/core/chart/ChartData";
import { TrainGroupParticipantUpdateDto } from "../model/entities/train-group-participant/TrainGroupParticipantUpdateDto";
import { ApiResponseDto } from "../model/core/api-response/ApiResponseDto";
import { UserLoginResponseDto } from "../model/entities/user/UserLoginResponseDto";
import { useTranslator } from "./TranslatorService";

const BASE_URL = "/api/";
const TOKEN_EXPIRATION_MS = 604800 * 1000; // 7 days
const refreshPromiseRef = { current: null as Promise<boolean> | null };
export const useApiService = () => {
  const { t } = useTranslator();
  const { showSuccess, showInfo, showWarn, showError } = useToast();

  const buildUrl = useCallback(
    (
      controller: string,
      endpoint: string = "",
      id?: number | string
    ): string => {
      let url = `${BASE_URL}${controller}`;
      if (id) url += `/${id}`;
      if (endpoint) url += `/${endpoint}`;
      return url;
    },
    []
  );

  const showMessages = useCallback(
    (messages: Record<string, string[]> | undefined, isSuccess: boolean) => {
      if (!messages) return;
      const allMessages = Object.values(messages).flat();
      allMessages.forEach((msg) =>
        isSuccess ? showSuccess(msg) : showError(msg)
      );
    },
    [showSuccess, showError]
  );

  const refreshUserToken = useCallback(async (): Promise<boolean> => {
    if (refreshPromiseRef.current) {
      console.log("Reusing existing token refresh promise.");
      return refreshPromiseRef.current;
    }

    refreshPromiseRef.current = (async () => {
      try {
        const url = buildUrl("Auth", "refreshToken");
        const refreshTokenDto = TokenService.getUserRefreshTokenDto();
        const token = refreshTokenDto.accessToken;
        const refreshToken = refreshTokenDto.refreshToken;

        if (!token || !refreshToken) {
          showError(t("Invalid or missing token. Please log in again."));
          TokenService.logout();
          return false;
        }

        const response = await apiFetch<
          UserRefreshTokenDto,
          UserRefreshTokenDto
        >(url, "POST", refreshTokenDto);
        if (!response || !response.data) {
          showMessages(response?.messages, false);
          return false;
        }

        const expireDate = new Date(Date.now() + TOKEN_EXPIRATION_MS);
        TokenService.setAccessToken(response.data.accessToken);
        TokenService.setRefreshToken(response.data.refreshToken);
        TokenService.setRefreshTokenExpireDate(expireDate.toISOString());
        console.log(
          "Token refresh successful, new expiration:",
          expireDate.toISOString()
        );
        // showSuccess("Token refreshed successfully!");
        return true;
      } catch (error) {
        console.error("Unexpected error during token refresh:", error);
        showError(t("Failed to refresh token. Please log in again."));
        return false;
      } finally {
        refreshPromiseRef.current = null;
      }
    })();

    return refreshPromiseRef.current;
  }, [buildUrl, showError, showSuccess, showMessages]);

  const handle401 = useCallback(
    async <TRequest, TResponse>(
      url: string,
      method: string,
      token: string | null | undefined,
      data?: TRequest
    ): Promise<ApiResponseDto<TResponse> | null> => {
      if (TokenService.isRefreshTokenExpired()) {
        showWarn(t("Token expired. Login required."));
        TokenService.logout();
        return null;
      }

      // showInfo("Token expired. Attempting to renew.");
      const isSuccess = await refreshUserToken();
      if (isSuccess) {
        // showInfo("Token renewed.");
        return apiFetch(url, method, data);
      }
      console.warn(t("Token refresh failed."));
      return null;
    },
    [showWarn, showInfo, refreshUserToken]
  );

  const apiFetch = useCallback(
    async <TRequest, TResponse>(
      url: string,
      method: string,
      data?: TRequest,
      retries = 1
    ): Promise<ApiResponseDto<TResponse> | null> => {
      const token = LocalStorageService.getAccessToken();
      const language = LocalStorageService.getLanguage();
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 10000);

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
          return handle401(url, method, token, data);
        }

        if (response.status === 400) {
          const result = (await response.json()) as ApiResponseDto<TResponse>;
          showMessages(result.messages, false);
          return null;
        }

        if (response.ok) {
          return (await response.json()) as ApiResponseDto<TResponse>;
        }

        if (response.status >= 500 && retries > 0) {
          await new Promise((resolve) => setTimeout(resolve, 1000));
          return apiFetch(url, method, data, retries - 1);
        }

        showError(t("API call failed with status ") + response.status);
        return null;
      } catch (error) {
        clearTimeout(timeoutId);
        if (error instanceof DOMException && error.name === "AbortError") {
          showError(t("Request timed out."));
          return null;
        }
        if (error instanceof TypeError && retries > 0) {
          await new Promise((resolve) => setTimeout(resolve, 1000));
          return apiFetch(url, method, data, retries - 1);
        }
        showError(t("Network or unexpected error during API call."));
        console.error(error);
        return null;
      }
    },
    [handle401, showError, showMessages]
  );

  const apiRequest = useCallback(
    async <TRequest, TResponse>(
      url: string,
      method: string,
      data?: TRequest
    ): Promise<TResponse | null> => {
      const response = await apiFetch<TRequest, TResponse>(url, method, data);
      if (!response || !response.isSucceed) {
        showMessages(response?.messages, false);
        return null;
      }
      showMessages(response.messages, true);
      return response.data ?? null;
    },
    [apiFetch, showMessages]
  );

  const get = useCallback(
    async <TEntity,>(
      controller: string,
      id: number | string
    ): Promise<TEntity | null> => {
      const url = buildUrl(controller, "", id);
      return apiRequest<TEntity, TEntity>(url, "GET");
    },
    [buildUrl, apiRequest]
  );

  const getGoogle = useCallback(
    async <TEntity,>(controller: string): Promise<TEntity | null> => {
      const url = buildUrl(controller, "");
      return apiRequest<TEntity, TEntity>(url, "GET");
    },
    [buildUrl, apiRequest]
  );

  const getDataLookup = useCallback(
    async (controller: string, data: LookupDto): Promise<LookupDto | null> => {
      const url = buildUrl(controller, "Lookup");
      return apiRequest<LookupDto, LookupDto>(url, "POST", data);
    },
    [buildUrl, apiRequest]
  );

  const getDataAutoComplete = useCallback(
    async <TEntity,>(
      controller: string,
      data: AutoCompleteDto<TEntity>
    ): Promise<AutoCompleteDto<TEntity> | null> => {
      const url = buildUrl(controller, "AutoComplete");
      return apiRequest<AutoCompleteDto<TEntity>, AutoCompleteDto<TEntity>>(
        url,
        "POST",
        data
      );
    },
    [buildUrl, apiRequest]
  );

  const getDataGrid = useCallback(
    async <TEntity,>(
      controller: string,
      data: DataTableDto<TEntity>
    ): Promise<DataTableDto<TEntity> | null> => {
      const url = buildUrl(controller, "GetDataTable");
      return apiRequest<DataTableDto<TEntity>, DataTableDto<TEntity>>(
        url,
        "POST",
        data
      );
    },
    [buildUrl, apiRequest]
  );

  const create = useCallback(
    async <TEntity,>(
      controller: string,
      data: TEntity
    ): Promise<TEntity[] | null> => {
      const url = buildUrl(controller);
      return apiRequest<TEntity[], TEntity[]>(url, "POST", [data]);
    },
    [buildUrl, apiRequest]
  );

  const createRange = useCallback(
    async <TEntity,>(
      controller: string,
      data: TEntity[]
    ): Promise<TEntity[] | null> => {
      const url = buildUrl(controller);
      return apiRequest<TEntity[], TEntity[]>(url, "POST", data);
    },
    [buildUrl, apiRequest]
  );

  const update = useCallback(
    async <TEntity,>(
      controller: string,
      data: TEntity,
      id: number | string
    ): Promise<TEntity | null> => {
      const url = buildUrl(controller, "", id);
      return apiRequest<TEntity, TEntity>(url, "PUT", data);
    },
    [buildUrl, apiRequest]
  );

  const deleteMethod = useCallback(
    async <TEntity,>(
      controller: string,
      id: number | string
    ): Promise<TEntity | null> => {
      const url = buildUrl(controller, "", id);
      return apiRequest<TEntity, TEntity>(url, "DELETE");
    },
    [buildUrl, apiRequest]
  );

  const timeslots = useCallback(
    async (
      endpoint: string,
      data: TimeSlotRequestDto
    ): Promise<TimeSlotResponseDto[] | null> => {
      const url = buildUrl(endpoint);
      return apiRequest<TimeSlotRequestDto, TimeSlotResponseDto[]>(
        url,
        "POST",
        data
      );
    },
    [buildUrl, apiRequest]
  );

  const updateParticipants = useCallback(
    async (
      data: TrainGroupParticipantUpdateDto
    ): Promise<TrainGroupParticipantUpdateDto | null> => {
      const url = buildUrl("TrainGroupParticipants", "UpdateParticipants");
      return apiRequest<
        TrainGroupParticipantUpdateDto,
        TrainGroupParticipantUpdateDto
      >(url, "POST", data);
    },
    [buildUrl, apiRequest]
  );

  const passwordForgot = useCallback(
    async (data: UserPasswordForgotDto): Promise<boolean> => {
      const url = buildUrl("Auth", "PasswordForgot");
      const result = await apiRequest<UserPasswordForgotDto, boolean>(
        url,
        "POST",
        data
      );
      return result ?? false;
    },
    [buildUrl, apiRequest]
  );

  const passwordReset = useCallback(
    async (data: UserPasswordResetDto): Promise<boolean> => {
      const url = buildUrl("Auth", "PasswordReset");
      const result = await apiRequest<UserPasswordResetDto, boolean>(
        url,
        "POST",
        data
      );
      return result ?? false;
    },
    [buildUrl, apiRequest]
  );

  const passwordChange = useCallback(
    async (data: UserPasswordChangeDto): Promise<boolean> => {
      const url = buildUrl("Auth", "PasswordChange");
      const result = await apiRequest<UserPasswordChangeDto, boolean>(
        url,
        "POST",
        data
      );
      return result ?? false;
    },
    [buildUrl, apiRequest]
  );

  const login = useCallback(
    async (
      data: UserLoginRequestDto,
      authLogin: () => void
    ): Promise<boolean> => {
      const url = buildUrl("Auth", "login");
      const result = await apiRequest<
        UserLoginRequestDto,
        UserLoginResponseDto
      >(url, "POST", data);
      if (!result) {
        showError(t("Login failed!"));
        return false;
      }

      const expireDate = new Date(Date.now() + TOKEN_EXPIRATION_MS);
      TokenService.setAccessToken(result.accessToken);
      TokenService.setRefreshToken(result.refreshToken);
      TokenService.setProfileImage(result.profileImage ?? "");
      TokenService.setFirstName(result.firstName ?? "");
      TokenService.setLastName(result.lastName ?? "");
      TokenService.setRefreshTokenExpireDate(expireDate.toISOString());
      console.debug(
        "Login successful, tokens set, expiration:",
        expireDate.toISOString()
      );
      authLogin();
      showSuccess(t("Login successful!"));
      return true;
    },
    [buildUrl, apiRequest, showError, showSuccess]
  );

  const register = useCallback(
    async (data: UserRegisterDto, authLogin: () => void): Promise<boolean> => {
      const url = buildUrl("Auth", "register");
      const result = await apiRequest<UserRegisterDto, UserRegisterDto>(
        url,
        "POST",
        data
      );
      if (!result) return false;

      const loginDto = new UserLoginRequestDto();
      loginDto.userNameOrEmail = data.email;
      loginDto.password = data.password;
      showSuccess(t("Registration successful!"));
      return login(loginDto, authLogin);
    },
    [buildUrl, apiRequest, showSuccess, login]
  );

  const logout = useCallback(
    async (authLogout: () => void): Promise<void> => {
      const url = buildUrl("Auth", "logout");
      const result = await apiRequest<
        ApiResponseDto<boolean>,
        ApiResponseDto<boolean>
      >(url, "POST");
      if (result?.isSucceed) {
        showSuccess(t("Logout successful!"));
      }
      TokenService.logout();
      authLogout();
    },
    [buildUrl, apiRequest, showSuccess]
  );

  const emailSend = useCallback(
    async (data: MailSendDto): Promise<boolean> => {
      const url = buildUrl("mails", "Send");
      const result = await apiRequest<MailSendDto, boolean>(url, "POST", data);
      return result ?? false;
    },
    [buildUrl, apiRequest]
  );

  const getChartData = useCallback(async (): Promise<ChartData | null> => {
    const url = buildUrl("charts");
    return apiRequest<null, ChartData>(url, "GET");
  }, [buildUrl, apiRequest]);

  return {
    get,
    getGoogle,
    getDataLookup,
    getDataAutoComplete,
    getDataGrid,
    create,
    createRange,
    update,
    delete: deleteMethod,
    timeslots,
    updateParticipants,
    passwordForgot,
    passwordReset,
    passwordChange,
    login,
    register,
    logout,
    emailSend,
    getChartData,
  };
};
