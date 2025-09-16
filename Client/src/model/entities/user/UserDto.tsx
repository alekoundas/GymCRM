export interface UserDto {
  [key: string]: any;

  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  roleId: string;
  profileImage?: string | null; // Base64-encoded image (e.g., "data:image/jpeg;base64,...") or null
}

export class UserDto {
  id: string = "";
  userName: string = "";
  email: string = "";
  firstName: string = "";
  lastName: string = "";
  roleId: string = "";
  profileImage?: string | null; // Base64-encoded image (e.g., "data:image/jpeg;base64,...") or null
}
