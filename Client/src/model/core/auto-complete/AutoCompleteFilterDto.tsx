export interface AutoCompleteFilterDto {
  excludedIds?: string[];
  searchValue?: string;
}

export class AutoCompleteFilterDto implements AutoCompleteFilterDto {
  excludedIds?: string[] = [];
  searchValue?: string = "";
}
