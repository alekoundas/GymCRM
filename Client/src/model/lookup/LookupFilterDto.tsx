// Interface
export interface LookupFilterDto {
  id?: string;
  value?: string;
  parentId?: string;
  searchValue?: string;
}

// Class
export class LookupFilterDto implements LookupFilterDto {
  id?: string = "";
  parentId?: string = "";
  value?: string = "";
  searchValue?: string = "";
}
