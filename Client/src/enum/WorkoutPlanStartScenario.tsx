// Mirrors Core.Enums.WorkoutPlanStartScenarioEnum. The API serialises enums as
// strings (JsonStringEnumConverter), so these names must match exactly.
export enum WorkoutPlanStartScenario {
  NoRule = "NoRule",
  Running = "Running",
  FirstEver = "FirstEver",
  UnderMax = "UnderMax",
  AtMax = "AtMax",
  AwayTooLong = "AwayTooLong",
  Orphaned = "Orphaned",
}
