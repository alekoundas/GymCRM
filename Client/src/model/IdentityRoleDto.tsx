import { ClaimDto } from "./ClaimDto";

export interface IdentityRoleDto {
  [key: string]: any;
  id: string;
  name: string;
  normalizedName: string;
  concurrencyStamp: string;
  claims: ClaimDto[];
}

export class IdentityRoleDto {
  id: string = "";
  name: string = "";
  normalizedName: string = "";
  concurrencyStamp: string = "";
  claims: ClaimDto[] = [];
}
