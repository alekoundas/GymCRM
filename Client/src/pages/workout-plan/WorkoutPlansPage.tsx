import { useRef, useState } from "react";
import { FormMode } from "../../enum/FormMode";
import { DialogControl } from "../../components/core/dialog/GenericDialogComponent";
import DataTableComponent from "../../components/core/datatable/DataTableComponent";
import { DataTableFilterDisplayEnum } from "../../enum/DataTableFilterDisplayEnum";
import { DataTableDto } from "../../model/datatable/DataTableDto";
import { DataTableColumns } from "../../model/datatable/DataTableColumns";
import { ButtonTypeEnum } from "../../enum/ButtonTypeEnum";
import { TokenService } from "../../services/TokenService";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";
import { useWorkoutPlanStore } from "../../stores/WorkoutPlanStore";
import { WorkoutPlanDto } from "../../model/entities/workout-plan/WorkoutPlanDto";
import { UserDto } from "../../model/entities/user/UserDto";
import { Avatar } from "primereact/avatar";
import DataTableFilterIdComponent from "../../components/core/datatable/DataTableFilterIdComponent";
import { useParams } from "react-router-dom";

export default function WorkoutPlansPage() {
  const { t } = useTranslator();
  const params = useParams();
  const apiService = useApiService();
  const { workoutPlanDto, resetWorkoutPlanDto, setWorkoutPlanDto } =
    useWorkoutPlanStore();

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<WorkoutPlanDto>) => void) | undefined
  >(undefined);

  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<WorkoutPlanDto>
  >({
    ...new DataTableDto(),
    filters: params["administrator"] // If Admin see everything
      ? []
      : [
          {
            fieldName: "UserId",
            value: TokenService.getUserId(),
            filterType: "equals",
          },
        ],
    dataTableSorts: [],
  });

  const availableGridRowButtons: () => ButtonTypeEnum[] = () => {
    return [
      ButtonTypeEnum.VIEW,
      ButtonTypeEnum.ADD,
      ButtonTypeEnum.EDIT,
      ButtonTypeEnum.DELETE,
    ];
  };

  // Custom chip template for selected users
  const chipTemplate = (user: UserDto | undefined) => {
    if (user) {
      const initials = `${user.firstName.charAt(0)}${user.lastName.charAt(
        0
      )}`.toUpperCase();
      const imageSrc = "data:image/png;base64," + user.profileImage;
      return (
        <div className="flex m-0 p-0 align-items-center">
          <Avatar
            image={user.profileImage ? imageSrc : ""}
            label={user.profileImage ? undefined : initials}
            shape="circle"
            size="normal"
            className=" mr-2 "
          />
          {" " +
            user.firstName[0].toUpperCase() +
            user.firstName.slice(1, user.firstName.length) +
            " " +
            user.lastName[0].toUpperCase() +
            user.lastName.slice(1, user.lastName.length)}
        </div>
      );
    }
  };

  const dataTableColumns: DataTableColumns<WorkoutPlanDto>[] = [
    {
      field: "title",
      header: t("Title"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      style: { width: "40%" },
    },
    {
      field: "userId",
      header: t("Participant"),
      sortable: true,
      filter: true,
      filterPlaceholder: t("Search"),
      filterTemplate: (options) => (
        <DataTableFilterIdComponent
          options={options}
          controller="users"
        />
      ),
      body: (rowData, options) => chipTemplate(rowData.user),
      style: { width: "10%" },
    },
  ];

  const onDataTableClick = (
    buttonType: ButtonTypeEnum,
    rowData?: WorkoutPlanDto
  ) => {
    switch (buttonType) {
      case ButtonTypeEnum.VIEW:
        if (rowData) {
          setWorkoutPlanDto(rowData);
        }
        break;
      case ButtonTypeEnum.ADD:
        resetWorkoutPlanDto();
        break;
      case ButtonTypeEnum.EDIT:
        if (rowData) {
          setWorkoutPlanDto(rowData);
        }
        break;
      case ButtonTypeEnum.DELETE:
        if (rowData) {
          setWorkoutPlanDto(rowData);
        }
        break;

      default:
        break;
    }
  };

  return (
    <>
      <DataTableComponent
        dataTableDto={datatableDto}
        setDataTableDto={setDatatableDto}
        formMode={FormMode.EDIT}
        onButtonClick={onDataTableClick}
        controller="WorkoutPlans"
        filterDisplay={DataTableFilterDisplayEnum.ROW}
        dataTableColumns={dataTableColumns}
        triggerRefreshData={triggerRefreshDataTable}
        availableGridRowButtons={availableGridRowButtons()}
      />
    </>
  );
}
