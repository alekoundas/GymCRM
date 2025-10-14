import { TrainGroupDateDto } from "../train-group-date/TrainGroupDateDto";
import { TrainGroupParticipantDto } from "../train-group-participant/TrainGroupParticipantDto";
import { UserDto } from "../user/UserDto";

export interface TrainGroupDto {
  id: number;
  title: string;
  description: string;
  duration: string;
  startOn: string;
  maxParticipants: number;
  trainerId: string;
  trainer?: UserDto | undefined;
  trainGroupDates: TrainGroupDateDto[];
  trainGroupParticipants: TrainGroupParticipantDto[];
}

export class TrainGroupDto {
  id: number = 0;
  title: string = "";
  description: string = "";
  duration: string = new Date(Date.UTC(2000, 0, 1, 0, 0, 0)).toISOString();
  startOn: string = new Date(Date.UTC(2000, 0, 1, 0, 0, 0)).toISOString();
  maxParticipants: number = 0;
  trainerId: string = "";
  trainer?: UserDto | undefined;
  trainGroupDates: TrainGroupDateDto[] = [];
  trainGroupParticipants: TrainGroupParticipantDto[] = [];
}
