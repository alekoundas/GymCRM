import { TrainGroupParticipantDto } from "./TrainGroupParticipantDto";

export interface TrainGroupParticipantUpdateDto {
  userId: string | undefined;
  trainGroupId: number;
  selectedDate: string;
  trainGroupParticipantDtos: TrainGroupParticipantDto[];
  clientTimezoneOffsetMinutes?: number;
}

export class TrainGroupParticipantUpdateDto {
  userId: string | undefined;
  trainGroupId: number = -1;
  selectedDate: string = "";
  trainGroupParticipantDtos: TrainGroupParticipantDto[] = [];
  clientTimezoneOffsetMinutes?: number = new Date().getTimezoneOffset();
}
