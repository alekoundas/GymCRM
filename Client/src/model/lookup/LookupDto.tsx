import { LookupFilterDto } from "./LookupFilterDto";
import { LookupOptionDto } from "./LookupOptionDto";

// Interface
export interface LookupDto {
  skip?: number;
  take?: number;
  filter: LookupFilterDto;
  data?: LookupOptionDto[];
}

// Class
export class LookupDto implements LookupDto {
  skip?: number = 0;
  take?: number = 0;
  filter: LookupFilterDto = new LookupFilterDto();
  data?: LookupOptionDto[] = [];

  constructor() {
    this.data = [];
  }
}
