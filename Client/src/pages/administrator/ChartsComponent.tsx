import { useEffect, useState } from "react";
import { Card } from "primereact/card";
import { ChartData } from "../../model/core/chart/ChartData";
import { Chart } from "primereact/chart";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { SubscriptionBucketDto } from "../../model/entities/subscription/SubscriptionChartsDto";
import SubscriptionBalanceTag from "../subscription/SubscriptionBalanceTag";

export default function ChartsComponent() {
  const [chartData, setChartData] = useState<ChartData | null>(null);
  const apiService = useApiService();
  const { t } = useTranslator();

  useEffect(() => {
    apiService.getChartData().then((data) => {
      if (data) {
        setChartData(data);
      }
    });
  }, []);

  if (!chartData) {
    return <div>{t("Loading charts")}...</div>;
  }

  const dailyEmailChartData = {
    labels: chartData.dailyEmails.map((d) =>
      new Date(d.date).toLocaleDateString()
    ),
    datasets: [
      {
        label: t("Emails Sent"),
        data: chartData.dailyEmails.map((d) => d.count),
        backgroundColor: "rgba(75, 192, 192, 0.2)",
        borderColor: "rgb(75, 192, 192)",
        borderWidth: 1,
      },
    ],
  };

  const dailyEmailOptions = {
    scales: {
      y: {
        beginAtZero: true,
      },
    },
  };

  const availableEmailsData = {
    labels: [t("Used"), t("Available")],
    datasets: [
      {
        data: [500 - chartData.availableEmails, chartData.availableEmails],
        backgroundColor: ["#FF6384", "#36A2EB"],
        hoverBackgroundColor: ["#FF6384", "#36A2EB"],
      },
    ],
  };

  const subscriptions = chartData.subscriptions;

  // Short month and a two digit year: twelve of them have to sit side by side.
  const monthLabel = (value: string): string => {
    const date = new Date(value);
    return date.toLocaleDateString(undefined, { month: "short", year: "2-digit" });
  };

  const bucketLabel = (bucket: SubscriptionBucketDto): string => {
    switch (bucket.key) {
      case "ONE_OR_LESS":
        return `1 ${t("or fewer")}`;
      case "TWO_TO_FIVE":
        return "2 - 5";
      case "SIX_TO_TEN":
        return "6 - 10";
      default:
        return `11+`;
    }
  };

  const monthlySubscriptionsData = {
    labels: subscriptions?.monthlyApproved.map((x) => monthLabel(x.month)) ?? [],
    datasets: [
      {
        label: t("Subscriptions"),
        data: subscriptions?.monthlyApproved.map((x) => x.amount) ?? [],
        backgroundColor: "rgba(54, 162, 235, 0.2)",
        borderColor: "rgb(54, 162, 235)",
        borderWidth: 1,
      },
    ],
  };

  const bucketsData = {
    labels: subscriptions?.buckets.map(bucketLabel) ?? [],
    datasets: [
      {
        label: t("Members"),
        data: subscriptions?.buckets.map((x) => x.count) ?? [],
        // Running out on the left, comfortable on the right.
        backgroundColor: ["#EF4444", "#F59E0B", "#3B82F6", "#22C55E"],
      },
    ],
  };

  const wholeNumberOptions = {
    plugins: { legend: { display: false } },
    scales: {
      y: {
        beginAtZero: true,
        ticks: { precision: 0 },
      },
    },
  };

  const userGrowthChartData = {
    labels: chartData.userGrowth.map((d) =>
      new Date(d.date).toLocaleDateString()
    ),
    datasets: [
      {
        label: t("Cumulative Users"),
        data: chartData.userGrowth.map((d) => d.cumulative),
        fill: false,
        borderColor: "rgb(75, 192, 192)",
        tension: 0.1,
      },
    ],
  };

  return (
    <div className="grid">
      <div className="col-12 md:col-4">
        <Card title={t("Daily Emails Sent (Last 7 Days)")}>
          <Chart
            type="bar"
            data={dailyEmailChartData}
            options={dailyEmailOptions}
          />
        </Card>
      </div>
      <div className="col-12 md:col-4">
        <Card title={t("Available Emails (Out of 500)")}>
          <Chart
            type="doughnut"
            data={availableEmailsData}
          />
        </Card>
      </div>
      <div className="col-12 md:col-4">
        <Card title={t("User Growth")}>
          <Chart
            type="line"
            data={userGrowthChartData}
          />
        </Card>
      </div>

      {subscriptions && (
        <>
          <div className="col-12 md:col-6">
            <Card title={t("Subscriptions added per month")}>
              <Chart
                type="bar"
                data={monthlySubscriptionsData}
                options={wholeNumberOptions}
              />
            </Card>
          </div>

          <div className="col-12 md:col-6">
            <Card title={t("Members by remaining subscriptions")}>
              <Chart
                type="bar"
                data={bucketsData}
                options={wholeNumberOptions}
              />

              <div className="mt-4">
                <h4 className="mt-0 mb-2">{t("Furthest behind")}</h4>

                {subscriptions.topDebtors.length === 0 ? (
                  <p className="m-0 text-color-secondary">
                    {t("Nobody is in the negative")}.
                  </p>
                ) : (
                  <ul className="list-none p-0 m-0">
                    {subscriptions.topDebtors.map((debtor) => (
                      <li
                        key={debtor.userId}
                        className="flex align-items-center justify-content-between py-2 border-bottom-1 surface-border"
                      >
                        <span>{debtor.fullName}</span>
                        <SubscriptionBalanceTag balance={debtor.balance} />
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </Card>
          </div>
        </>
      )}
    </div>
  );
}
