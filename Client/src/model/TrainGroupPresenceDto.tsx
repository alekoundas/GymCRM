export interface TrainGroupPresenceDto {
  id: number;
  presenceDate: string;
  trainGroupId: number;
  userId: number;
}

export class TrainGroupPresenceDto {
  id: number = 0;
  presenceDate: string = "";
  trainGroupId: number = 0;
  userId: number = 0;
}
