import { AutoCompleteFilterDto } from "./AutoCompleteFilterDto";

export interface AutoCompleteDto<TEntity> {
  skip?: number;
  take?: number;
  totalRecords?: number;
  searchValue?: string;
  suggestions?: TEntity[];
}

export class AutoCompleteDto<TEntity> implements AutoCompleteDto<TEntity> {
  skip?: number = 0;
  take?: number = 0;
  totalRecords?: number = 0;
  searchValue?: string = "";
  suggestions?: TEntity[] = [];
}
