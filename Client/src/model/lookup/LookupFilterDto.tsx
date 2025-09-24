// Interface
export interface LookupFilterDto {
  id?: string;
  parentId?: string;
  value?: string;
}

// Class
export class LookupFilterDto implements LookupFilterDto {
  id?: string = "";
  parentId?: string = "";
  value?: string = "";
}
