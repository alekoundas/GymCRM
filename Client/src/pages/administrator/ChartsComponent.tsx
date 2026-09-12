import { useEffect, useState } from "react";
import { Card } from "primereact/card";
import { ChartData } from "../../model/core/chart/ChartData";
import { Chart } from "primereact/chart";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { SubscriptionBucketDto } from "../../model/entities/subscription/SubscriptionChartsDto";
import SubscriptionBalanceTag from "../subscription/SubscriptionBalanceTag";

// A floor, not a fixed height. The chart is a flex child that takes whatever the card
// has spare, so a row of cards lines up and no panel is left half empty; this only
// stops it collapsing when the card is short.
const MIN_CHART_HEIGHT = "16rem";

// PrimeReact renders the chart inside a div of its own that has no height, so chart.js
// measures that rather than the box we put it in and falls back to its default canvas
// height. Filling the wrapper is what lets the card's height reach the canvas.
const cardPassThrough = {
  body: { className: "h-full flex flex-column" },
  content: { className: "flex-1 flex flex-column" },
};

// A single accent carries the neutral charts; red through green is kept for the one
// chart where the colour means something - how close a member is to running out.
const ACCENT = "#60A5FA";
const ACCENT_SOFT = "rgba(96, 165, 250, 0.25)";
const GOOD = "#34D399";
const WARN = "#FBBF24";
const BAD = "#F87171";

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

  // Chart.js paints its own text and grid lines and knows nothing about the theme, so
  // the colours are read off the stylesheet. An unknown variable comes back empty and
  // chart.js falls back to its default, which is the behaviour we had before.
  const style = getComputedStyle(document.documentElement);
  const textColor = style.getPropertyValue("--text-color");
  const mutedColor = style.getPropertyValue("--text-color-secondary");
  const gridColor = style.getPropertyValue("--surface-border");

  // maintainAspectRatio is the whole point of this object. Left on, chart.js sizes the
  // canvas from its own ratio and parks it against the left edge of the card; off, the
  // canvas fills the box it is given, which is what centres everything.
  const axisOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: { displayColors: false },
    },
    scales: {
      x: {
        ticks: { color: mutedColor },
        grid: { display: false },
        border: { color: gridColor },
      },
      y: {
        beginAtZero: true,
        ticks: { color: mutedColor, precision: 0 },
        grid: { color: gridColor, drawBorder: false },
        border: { display: false },
      },
    },
  };

  const doughnutOptions = {
    responsive: true,
    maintainAspectRatio: false,
    cutout: "68%",
    plugins: {
      legend: { position: "bottom" as const, labels: { color: textColor } },
    },
  };

  const bar = (label: string, values: number[], colour: string | string[]) => ({
    label,
    data: values,
    backgroundColor: colour,
    borderRadius: 6,
    // Four buckets across a wide card would otherwise each be a slab.
    maxBarThickness: 56,
  });

  const dailyEmailChartData = {
    labels: chartData.dailyEmails.map((d) => new Date(d.date).toLocaleDateString()),
    datasets: [bar(t("Emails Sent"), chartData.dailyEmails.map((d) => d.count), ACCENT)],
  };

  const availableEmailsData = {
    labels: [t("Used"), t("Available")],
    datasets: [
      {
        data: [500 - chartData.availableEmails, chartData.availableEmails],
        backgroundColor: [WARN, ACCENT],
        hoverBackgroundColor: [WARN, ACCENT],
        borderWidth: 0,
      },
    ],
  };

  const userGrowthChartData = {
    labels: chartData.userGrowth.map((d) => new Date(d.date).toLocaleDateString()),
    datasets: [
      {
        label: t("Cumulative Users"),
        data: chartData.userGrowth.map((d) => d.cumulative),
        fill: true,
        backgroundColor: ACCENT_SOFT,
        borderColor: ACCENT,
        pointBackgroundColor: ACCENT,
        pointRadius: 2,
        tension: 0.35,
      },
    ],
  };

  const subscriptions = chartData.subscriptions;

  // Short month and a two digit year: twelve of them have to sit side by side.
  const monthLabel = (year: number, month: number): string =>
    new Date(year, month - 1, 1).toLocaleDateString(undefined, {
      month: "short",
      year: "2-digit",
    });

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
    labels: subscriptions?.monthlyApproved.map((x) => monthLabel(x.year, x.month)) ?? [],
    datasets: [
      bar(t("Subscriptions"), subscriptions?.monthlyApproved.map((x) => x.amount) ?? [], ACCENT),
    ],
  };

  const bucketsData = {
    labels: subscriptions?.buckets.map(bucketLabel) ?? [],
    datasets: [
      // Running out on the left, comfortable on the right.
      bar(t("Members"), subscriptions?.buckets.map((x) => x.count) ?? [], [
        BAD,
        WARN,
        ACCENT,
        GOOD,
      ]),
    ],
  };

  return (
    <div className="grid">
      <div className="col-12 md:col-4">
        <Card
          className="h-full"
          pt={cardPassThrough}
          title={t("Daily Emails Sent (Last 7 Days)")}
        >
          <div
            className="flex-1"
            style={{ minHeight: MIN_CHART_HEIGHT }}
          >
            <Chart
              type="bar"
              data={dailyEmailChartData}
              options={axisOptions}
              style={{ height: "100%" }}
            />
          </div>
        </Card>
      </div>

      <div className="col-12 md:col-4">
        <Card
          className="h-full"
          pt={cardPassThrough}
          title={t("Available Emails (Out of 500)")}
        >
          <div
            className="flex-1"
            style={{ minHeight: MIN_CHART_HEIGHT }}
          >
            <Chart
              type="doughnut"
              data={availableEmailsData}
              options={doughnutOptions}
              style={{ height: "100%" }}
            />
          </div>
        </Card>
      </div>

      <div className="col-12 md:col-4">
        <Card
          className="h-full"
          pt={cardPassThrough}
          title={t("User Growth")}
        >
          <div
            className="flex-1"
            style={{ minHeight: MIN_CHART_HEIGHT }}
          >
            <Chart
              type="line"
              data={userGrowthChartData}
              options={axisOptions}
              style={{ height: "100%" }}
            />
          </div>
        </Card>
      </div>

      {subscriptions && (
        <>
          <div className="col-12 md:col-6">
            <Card
              className="h-full"
              pt={cardPassThrough}
              title={t("Subscriptions added per month")}
            >
              <div
                className="flex-1"
                style={{ minHeight: MIN_CHART_HEIGHT }}
              >
                <Chart
                  type="bar"
                  data={monthlySubscriptionsData}
                  options={axisOptions}
                  style={{ height: "100%" }}
                />
              </div>
            </Card>
          </div>

          <div className="col-12 md:col-6">
            <Card
              className="h-full"
              pt={cardPassThrough}
              title={t("Members by remaining subscriptions")}
            >
              <div
                className="flex-1"
                style={{ minHeight: MIN_CHART_HEIGHT }}
              >
                <Chart
                  type="bar"
                  data={bucketsData}
                  options={axisOptions}
                  style={{ height: "100%" }}
                />
              </div>

              {/* Stays its natural size - only the chart above it stretches. */}
              <div className="flex-none mt-4 pt-3 border-top-1 surface-border">
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
                        className="flex align-items-center justify-content-between gap-3 py-2 border-bottom-1 surface-border"
                      >
                        <span className="white-space-nowrap overflow-hidden text-overflow-ellipsis">
                          {debtor.fullName}
                        </span>
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
