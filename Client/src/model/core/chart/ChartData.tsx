import { DailyEmailCount } from "./DailyEmailCount";
import { UserGrowth } from "./UserGrowth";
import { SubscriptionChartsDto } from "../../entities/subscription/SubscriptionChartsDto";

export interface ChartData {
  dailyEmails: DailyEmailCount[];
  availableEmails: number;
  userGrowth: UserGrowth[];
  // Absent for anybody without the subscriptions claim.
  subscriptions?: SubscriptionChartsDto;
}
