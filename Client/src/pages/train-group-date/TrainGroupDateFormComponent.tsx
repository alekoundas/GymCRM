import { FormMode } from "../../enum/FormMode";
import { useTrainGroupStore } from "../../stores/TrainGroupStore";
import { InputNumber, InputNumberChangeEvent } from "primereact/inputnumber";
import { Dropdown } from "primereact/dropdown";
import { TrainGroupDateTypeEnum } from "../../enum/TrainGroupDateTypeEnum";
import { Calendar } from "primereact/calendar";
import { DateService } from "../../services/DateService";
import { DayOfWeekEnum } from "../../enum/DayOfWeekEnum";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";

interface IField extends DialogChildProps {}

export default function TrainGroupDateFormComponent({ formMode }: IField) {
  const { trainGroupDateDto, setTrainGroupDateDto } = useTrainGroupStore();

  return (
    <>
      <div className="flex flex-column md:flex-row justify-content-center p-3">
        <div className="field p-3">
          <label
            htmlFor="trainGroupDateType"
            className="block text-900 font-medium mb-2"
          >
            Train Group Date Type
          </label>
          <Dropdown
            value={trainGroupDateDto.trainGroupDateType}
            onChange={(e: any) => {
              setTrainGroupDateDto({
                ...trainGroupDateDto,
                trainGroupDateType: e.value,
                fixedDay:
                  e.value === TrainGroupDateTypeEnum.FIXED_DAY
                    ? trainGroupDateDto.fixedDay
                    : undefined,
                recurrenceDayOfWeek:
                  e.value === TrainGroupDateTypeEnum.DAY_OF_WEEK
                    ? trainGroupDateDto.recurrenceDayOfWeek
                    : undefined,
                recurrenceDayOfMonth:
                  e.value === TrainGroupDateTypeEnum.DAY_OF_MONTH
                    ? trainGroupDateDto.recurrenceDayOfMonth
                    : undefined,
              });
            }}
            options={Object.keys(TrainGroupDateTypeEnum)}
            optionLabel="type"
            placeholder="Select Type"
            className="w-full"
            checkmark={true}
            highlightOnSelect={true}
            disabled={formMode === FormMode.VIEW}
          />
        </div>

        {trainGroupDateDto.trainGroupDateType ===
          TrainGroupDateTypeEnum.FIXED_DAY && (
          <div className="field p-3">
            <label
              htmlFor="fixedDay"
              className="block text-900 font-medium mb-2"
            >
              Fixed Day
            </label>
            <Calendar
              value={trainGroupDateDto.fixedDay}
              onChange={(e: any) => {
                setTrainGroupDateDto({
                  ...trainGroupDateDto,
                  fixedDay: new Date(
                    Date.UTC(
                      e.target.value.getFullYear(),
                      e.target.value.getMonth(),
                      e.target.value.getDate(),
                      0,
                      0,
                      0
                    )
                  ),
                });
              }}
              showIcon={true}
              minDate={new Date(new Date().setHours(0, 0, 0, 0))} // Prevent selecting past dates
              className="w-full"
              dateFormat="dd/mm/yy"
              disabled={formMode === FormMode.VIEW}
            />
          </div>
        )}

        {trainGroupDateDto.trainGroupDateType ===
          TrainGroupDateTypeEnum.DAY_OF_MONTH && (
          <div className="field p-3">
            <label
              htmlFor="recurrenceDayOfMonth"
              className="block text-900 font-medium mb-2"
            >
              Recurrence Day Of Month
            </label>

            <InputNumber
              value={trainGroupDateDto.recurrenceDayOfMonth}
              onChange={(e: InputNumberChangeEvent) => {
                if (e.value)
                  setTrainGroupDateDto({
                    ...trainGroupDateDto,
                    recurrenceDayOfMonth: e.value,
                  });
              }}
              min={0}
              max={31}
              disabled={formMode === FormMode.VIEW}
            />
          </div>
        )}
        {trainGroupDateDto.trainGroupDateType ===
          TrainGroupDateTypeEnum.DAY_OF_WEEK && (
          <div className="field p-3">
            <label
              htmlFor="recurrenceDayOfWeek"
              className="block text-900 font-medium mb-2"
            >
              Recurrence Day Of Week
            </label>
            <Dropdown
              value={trainGroupDateDto.recurrenceDayOfWeek}
              options={Object.keys(DayOfWeekEnum)}
              onChange={(e: any) => {
                if (e.target.value) {
                  setTrainGroupDateDto({
                    ...trainGroupDateDto,
                    recurrenceDayOfWeek: e.target.value,
                  });
                }
              }}
              placeholder="Select Type"
              className="w-full"
              checkmark={true}
              highlightOnSelect={true}
              disabled={formMode === FormMode.VIEW}
            />
          </div>
        )}
      </div>
    </>
  );
}
