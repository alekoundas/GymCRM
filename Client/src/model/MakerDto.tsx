export interface MakerDto {
  [key: string]: any;

  id: number;
  title: string;
}

export class MakerDto {
  id: number = 0;
  title: string = "";
}
