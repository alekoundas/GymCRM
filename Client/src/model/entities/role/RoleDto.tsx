import { ClaimDto } from "../../ClaimDto";

export interface RoleDto {
  [key: string]: any;
  id: string;
  name: string;
  normalizedName: string;
  concurrencyStamp: string;
  claims: ClaimDto[];
}

export class RoleDto {
  id: string = "";
  name: string = "";
  normalizedName: string = "";
  concurrencyStamp: string = "";
  claims: ClaimDto[] = [];
}
