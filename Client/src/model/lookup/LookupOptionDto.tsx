export interface LookupOptionDto {
  id?: string;
  value?: string;
  firstName?: string;
  lastName?: string;
  profileImage?: string | null; // Base64-encoded image (e.g., "data:image/jpeg;base64,...") or null
  userColor?: string | null;
  color?: string | null; // A train group category's "#rrggbb"
}

// Class
export class LookupOptionDto {
  id?: string = "";
  value?: string = "";
  firstName?: string = "";
  lastName?: string = "";
  profileImage?: string | null; // Base64-encoded image (e.g., "data:image/jpeg;base64,...") or null
  userColor?: string | null;
  color?: string | null; // A train group category's "#rrggbb"
}
