import { DayOfWeekEnum } from "../../../enum/DayOfWeekEnum";
import { TrainGroupDateTypeEnum } from "../../../enum/TrainGroupDateTypeEnum";
import { TrainGroupParticipantDto } from "../train-group-participant/TrainGroupParticipantDto";
import { TrainGroupDto } from "../train-group/TrainGroupDto";

export interface TrainGroupDateDto {
  id: number;
  trainGroupDateType?: TrainGroupDateTypeEnum;
  fixedDay?: Date;
  recurrenceDayOfWeek?: DayOfWeekEnum;
  recurrenceDayOfMonth?: number;
  trainGroupId?: number;
  trainGroup?: TrainGroupDto;
  trainGroupParticipants: TrainGroupParticipantDto[];
  // trainGroupDateCancellationSubscribers: TrainGroupCancellationSubscriberDto[];
}

export class TrainGroupDateDto {
  id: number = 0;
  trainGroupDateType?: TrainGroupDateTypeEnum;
  fixedDay?: Date;
  recurrenceDayOfWeek?: DayOfWeekEnum;
  recurrenceDayOfMonth?: number;
  trainGroupId?: number;
  trainGroup?: TrainGroupDto;
  trainGroupParticipants: TrainGroupParticipantDto[] = [];
  // trainGroupDateCancellationSubscribers: TrainGroupCancellationSubscriberDto[] =
  // [];
}
