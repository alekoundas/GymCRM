import { DailyEmailCount } from "./DailyEmailCount";
import { UserGrowth } from "./UserGrowth";

export interface ChartData {
  dailyEmails: DailyEmailCount[];
  availableEmails: number;
  userGrowth: UserGrowth[];
}
