export interface MakerModelDto {
  [key: string]: any;

  id: number;
  makerId: number;
  title: string;
}

export class MakerModelDto {
  id: number = 0;
  makerId: number = 0;
  title: string = "";
}
