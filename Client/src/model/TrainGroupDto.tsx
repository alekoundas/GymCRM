import { TrainGroupDateDto } from "./TrainGroupDateDto";

export interface TrainGroupDto {
  id: number;
  title: string;
  description: string;
  duration: Date;
  startOn: Date;
  maxParticipants: number;
  trainerId: string;
  trainGroupDates: TrainGroupDateDto[];
  TrainGroupParticipants: TrainGroupDateDto[];
}

export class TrainGroupDto {
  id: number;
  title: string;
  description: string;
  duration: Date;
  startOn: Date;
  maxParticipants: number;
  trainerId: string;
  trainGroupDates: TrainGroupDateDto[];
  TrainGroupParticipants: TrainGroupDateDto[];
}
