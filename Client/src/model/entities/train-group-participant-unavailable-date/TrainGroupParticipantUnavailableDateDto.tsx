export interface TrainGroupParticipantUnavailableDateDto {
  id: number;
  unavailableDate: string;
  trainGroupParticipantId: number;
  isAdminPage: boolean;
  // The member's own clock, for the 12-hour rule. Left out, the server took the
  // member to be on UTC and put the cut-off hours off in Greece.
  clientTimezoneOffsetMinutes?: number;
}

export class TrainGroupParticipantUnavailableDateDto {
  id: number = -1;
  unavailableDate: string = "";
  trainGroupParticipantId: number = 0;
  isAdminPage: boolean = false;
}
