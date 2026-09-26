// What the admin fills in to record an attendance by hand. Either a train group is
// chosen, and the server copies the session off it, or the session is typed out here
// for one whose group no longer exists.
export interface TrainGroupAttendanceAddDto {
  attendanceDate: string;
  userId: string;

  trainGroupId?: number;

  trainGroupTitle: string;
  trainGroupDescription: string;
  trainGroupStartOn?: string;
  trainGroupDuration?: string;
  trainerFullName: string;
}

export class TrainGroupAttendanceAddDto implements TrainGroupAttendanceAddDto {
  // Today, as midnight UTC - the form's calendar reads it back with the UTC getters.
  attendanceDate: string = new Date(
    Date.UTC(new Date().getFullYear(), new Date().getMonth(), new Date().getDate())
  ).toISOString();
  userId: string = "";
  trainGroupId?: number = undefined;
  trainGroupTitle: string = "";
  trainGroupDescription: string = "";
  trainGroupStartOn?: string = undefined;
  trainGroupDuration?: string = undefined;
  trainerFullName: string = "";
}
