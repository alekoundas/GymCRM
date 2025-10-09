export interface UserLoginResponseDto {
  accessToken: string;
  refreshToken: string;
  firstName: string;
  lastName: string;
  profileImage?: string | null; // Base64-encoded image (e.g., "data:image/jpeg;base64,...") or null
}

export class UserLoginResponseDto {
  accessToken: string = "";
  refreshToken: string = "";
  firstName: string = "";
  lastName: string = "";
  profileImage?: string | null; // Base64-encoded image (e.g., "data:image/jpeg;base64,...") or null
}
