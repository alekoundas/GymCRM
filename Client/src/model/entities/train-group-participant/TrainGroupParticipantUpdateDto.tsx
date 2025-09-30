import { TrainGroupParticipantDto } from "./TrainGroupParticipantDto";

export interface TrainGroupParticipantUpdateDto {
  userId: string | undefined;
  trainGroupId: number;
  selectedDate: string;
  trainGroupParticipantDtos: TrainGroupParticipantDto[];
}

export class TrainGroupParticipantUpdateDto {
  userId: string | undefined;
  trainGroupId: number = -1;
  selectedDate: string = "";
  trainGroupParticipantDtos: TrainGroupParticipantDto[] = [];
}
