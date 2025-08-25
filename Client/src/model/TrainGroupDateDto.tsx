import { TrainGroupDateTypeEnum } from "../enum/TrainGroupDateTypeEnum";
import { TrainGroupCancellationSubscriberDto } from "./TrainGroupCancellationSubscriberDto";
import { TrainGroupDto } from "./TrainGroupDto";
import { TrainGroupParticipantDto } from "./TrainGroupParticipantDto";

export interface TrainGroupDateDto {
  id: number;
  trainGroupDateType?: TrainGroupDateTypeEnum;
  fixedDay?: Date;
  recurrenceDayOfWeek?: number;
  recurrenceDayOfMonth?: number;
  trainGroupId?: number;
  trainGroup?: TrainGroupDto;
  trainGroupParticipants: TrainGroupParticipantDto[];
  trainGroupCancellationSubscribers: TrainGroupCancellationSubscriberDto[];
}

export class TrainGroupDateDto {
  id: number;
  trainGroupDateType?: TrainGroupDateTypeEnum;
  fixedDay?: Date;
  recurrenceDayOfWeek?: number;
  recurrenceDayOfMonth?: number;
  trainGroupId?: number;
  trainGroup?: TrainGroupDto;
  trainGroupParticipants: TrainGroupParticipantDto[] = [];
  trainGroupCancellationSubscribers: TrainGroupCancellationSubscriberDto[] = [];
}
