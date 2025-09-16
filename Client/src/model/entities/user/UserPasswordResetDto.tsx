export interface UserPasswordResetDto {
  email: string;
  token: string;
  newPassword: string;
  confirmNewPassword: string;
}
export class UserPasswordResetDto {
  email: string = "";
  token: string = "";
  newPassword: string = "";
  confirmNewPassword: string = "";
}
