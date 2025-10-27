export interface TrainGroupUnavailableDateDto {
  id: number;
  unavailableDate: string;
  trainGroupId: number;
}

export class TrainGroupUnavailableDateDto {
  id: number = -1;
  unavailableDate: string = "";
  trainGroupId: number = 0;
}
