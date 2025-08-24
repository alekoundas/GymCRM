import { TrainGroupDateDto } from "./TrainGroupDateDto";
import { TrainGroupParticipantDto } from "./TrainGroupParticipantDto";

export interface TrainGroupDto {
  id: number;
  name: string;
  description: string;
  duration: Date;
  startOn: Date;
  maxParticipants: number;
  trainerId: string;
  repeatingParticipants: TrainGroupParticipantDto[];
  trainGroupDates: TrainGroupDateDto[];
}

export class TrainGroupDto {
  id: number;
  name: string;
  description: string;
  duration: Date;
  startOn: Date;
  maxParticipants: number;
  trainerId: string;
  repeatingParticipants: TrainGroupParticipantDto[];
  trainGroupDates: TrainGroupDateDto[];
}
