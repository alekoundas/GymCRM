export interface UserPasswordChangeDto {
  userId: string;
  oldPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}
export class UserPasswordChangeDto {
  userId: string = "";
  oldPassword: string = "";
  newPassword: string = "";
  confirmNewPassword: string = "";
}
