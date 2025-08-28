export interface UserRefreshTokenDto {
  accessToken: string;
  refreshToken: string;
}

export class UserRefreshTokenDto {
  accessToken: string = "";
  refreshToken: string = "";
}
