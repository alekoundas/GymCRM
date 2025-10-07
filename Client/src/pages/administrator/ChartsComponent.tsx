import { useEffect, useState } from "react";
import { Card } from "primereact/card";
import { ChartData } from "../../model/core/chart/ChartData";
import { Chart } from "primereact/chart";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";

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
    return <div>Loading charts...</div>;
  }

  const dailyEmailChartData = {
    labels: chartData.dailyEmails.map((d) =>
      new Date(d.date).toLocaleDateString()
    ),
    datasets: [
      {
        label: "Emails Sent",
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
    labels: ["Used", "Available"],
    datasets: [
      {
        data: [500 - chartData.availableEmails, chartData.availableEmails],
        backgroundColor: ["#FF6384", "#36A2EB"],
        hoverBackgroundColor: ["#FF6384", "#36A2EB"],
      },
    ],
  };

  const userGrowthChartData = {
    labels: chartData.userGrowth.map((d) =>
      new Date(d.date).toLocaleDateString()
    ),
    datasets: [
      {
        label: "Cumulative Users",
        data: chartData.userGrowth.map((d) => d.cumulative),
        fill: false,
        borderColor: "rgb(75, 192, 192)",
        tension: 0.1,
      },
    ],
  };

  return (
    <div className="grid">
      <h1>{t("welcome")}</h1>
      <div className="col-12 md:col-4">
        <Card title="Daily Emails Sent (Last 7 Days)">
          <Chart
            type="bar"
            data={dailyEmailChartData}
            options={dailyEmailOptions}
          />
        </Card>
      </div>
      <div className="col-12 md:col-4">
        <Card title="Available Emails (Out of 500)">
          <Chart
            type="doughnut"
            data={availableEmailsData}
          />
        </Card>
      </div>
      <div className="col-12 md:col-4">
        <Card title="User Growth">
          <Chart
            type="line"
            data={userGrowthChartData}
          />
        </Card>
      </div>
    </div>
  );
}
