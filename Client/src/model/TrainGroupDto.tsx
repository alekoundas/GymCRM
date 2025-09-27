import { UserDto } from "./entities/user/UserDto";
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
  trainer?: UserDto | undefined;
  trainGroupDates: TrainGroupDateDto[];
  trainGroupParticipants: TrainGroupParticipantDto[];
}

export class TrainGroupDto {
  id: number = 0;
  title: string = "";
  description: string = "";
  duration: Date = new Date();
  startOn: Date = new Date();
  maxParticipants: number = 0;
  trainerId: string = "";
  trainer?: UserDto | undefined;
  trainGroupDates: TrainGroupDateDto[] = [];
  trainGroupParticipants: TrainGroupParticipantDto[] = [];
}
