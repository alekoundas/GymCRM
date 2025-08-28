export interface UserLoginResponseDto {
  accessToken: string;
  refreshToken: string;
}

export class UserLoginResponseDto {
  accessToken: string = "";
  refreshToken: string = "";
}
