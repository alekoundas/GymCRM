import { RepeatingTrainGroupTypeEnum } from "../enum/RepeatingTrainGroupTypeEnum";
import { TrainGroupCancellationSubscriberDto } from "./TrainGroupCancellationSubscriberDto";
import { TrainGroupDto } from "./TrainGroupDto";
import { TrainGroupParticipantDto } from "./TrainGroupParticipantDto";

export interface TrainGroupDateDto {
  id: number;
  repeatingTrainGroupType?: RepeatingTrainGroupTypeEnum;
  fixedDay?: Date;
  recurrenceDayOfWeek?: number;
  recurrenceDayOfMonth?: number;
  trainGroupId: number;
  trainGroup?: TrainGroupDto;
  trainGroupParticipants: TrainGroupParticipantDto[];
  trainGroupCancellationSubscribers: TrainGroupCancellationSubscriberDto[];
}

export class TrainGroupDateDto {
  id: number;
  repeatingTrainGroupType?: RepeatingTrainGroupTypeEnum;
  fixedDay?: Date;
  recurrenceDayOfWeek?: number;
  recurrenceDayOfMonth?: number;
  trainGroupId: number = 0;
  trainGroup?: TrainGroupDto;
  trainGroupParticipants: TrainGroupParticipantDto[] = [];
  trainGroupCancellationSubscribers: TrainGroupCancellationSubscriberDto[] = [];
}
