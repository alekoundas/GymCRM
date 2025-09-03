import { TrainGroupDateDto } from "./TrainGroupDateDto";
import { TrainGroupParticipantDto } from "./TrainGroupParticipantDto";

export interface TrainGroupDto {
  id: number;
  title: string;
  description: string;
  duration: Date;
  startOn: Date;
  maxParticipants: number;
  trainerId: string;
  trainGroupDates: TrainGroupDateDto[];
  trainGroupParticipants: TrainGroupParticipantDto[];
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
  trainGroupParticipants: TrainGroupParticipantDto[];
}
