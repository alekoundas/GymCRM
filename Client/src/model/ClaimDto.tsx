export interface ClaimDto {
  [key: string]: any;
  controller: string;
  view: boolean;
  add: boolean;
  edit: boolean;
  delete: boolean;
}

export class ClaimDto {
  controller: string = "";
  view: boolean = false;
  add: boolean = false;
  edit: boolean = false;
  delete: boolean = false;
}
