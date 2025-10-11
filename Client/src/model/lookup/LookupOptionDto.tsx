export interface LookupOptionDto {
  id?: string;
  value?: string;
  firstName?: string;
  lastName?: string;
  profileImage?: string | null; // Base64-encoded image (e.g., "data:image/jpeg;base64,...") or null
}

// Class
export class LookupOptionDto {
  id?: string = "";
  value?: string = "";
  firstName?: string = "";
  lastName?: string = "";
  profileImage?: string | null; // Base64-encoded image (e.g., "data:image/jpeg;base64,...") or null
}
