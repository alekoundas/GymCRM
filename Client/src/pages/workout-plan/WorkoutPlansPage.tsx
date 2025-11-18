import { useEffect, useRef, useState } from "react";
import { FormMode } from "../../enum/FormMode";
import GenericDialogComponent, {
  DialogControl,
} from "../../components/core/dialog/GenericDialogComponent";
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
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { Card } from "primereact/card";
import { Tag } from "primereact/tag";
import { ColumnFilterElementTemplateOptions } from "primereact/column";
import DataTableFilterDateComponent from "../../components/core/datatable/DataTableFilterDateComponent";

export default function WorkoutPlansPage() {
  const { t } = useTranslator();
  const apiService = useApiService();
  const navigate = useNavigate();
  const location = useLocation();
  const isAdminPage = location.pathname.includes("/administrator");
  const { workoutPlanDto, resetWorkoutPlanDto, setWorkoutPlanDto } =
    useWorkoutPlanStore();

  const [isDeleteDialogVisible, setDeleteDialogVisibility] = useState(false); // Dialog visibility

  const dialogControlDelete: DialogControl = {
    showDialog: () => setDeleteDialogVisibility(true),
    hideDialog: () => setDeleteDialogVisibility(false),
  };

  const triggerRefreshDataTable = useRef<
    ((dto: DataTableDto<WorkoutPlanDto>) => void) | undefined
  >(undefined);

  const [datatableDto, setDatatableDto] = useState<
    DataTableDto<WorkoutPlanDto>
  >({
    ...new DataTableDto(),
    filters: isAdminPage // If Admin see everything
      ? [
          { fieldName: "createdOn", filterType: "between" },
          { fieldName: "title", filterType: "contains" },
        ]
      : [
          { fieldName: "createdOn", filterType: "between" },
          { fieldName: "title", filterType: "contains" },
          {
            fieldName: "UserId",
            value: TokenService.getUserId(),
            filterType: "equals",
          },
        ],
    dataTableSorts: [],
  });

  const availableGridRowButtons: () => ButtonTypeEnum[] = () => {
    if (isAdminPage)
      return [ButtonTypeEnum.ADD, ButtonTypeEnum.EDIT, ButtonTypeEnum.DELETE];

    return [ButtonTypeEnum.EDIT];
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
          <Tag
            className="opacity-100"
            style={{
              backgroundColor: "#" + user.userStatus?.color,
            }}
          >
            {" " +
              user.firstName[0].toUpperCase() +
              user.firstName.slice(1, user.firstName.length) +
              " " +
              user.lastName[0].toUpperCase() +
              user.lastName.slice(1, user.lastName.length)}
          </Tag>
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
      filterTemplate: (options: ColumnFilterElementTemplateOptions) => (
        <DataTableFilterIdComponent
          options={options}
          controller="users"
        />
      ),
      body: (rowData: { user: UserDto | undefined }, options: any) =>
        chipTemplate(rowData.user),
      style: { width: "10%" },
    },
    {
      field: "createdOn",
      header: t("CreatedOn"),
      sortable: true,
      filter: true,
      filterTemplate: (options) => (
        <DataTableFilterDateComponent options={options} />
      ),
      body: (rowData, options) => (
        <>
          {new Date(rowData.createdOn).getDate() +
            "/" +
            (new Date(rowData.createdOn).getMonth() + 1) +
            "/" +
            new Date(rowData.createdOn).getFullYear() +
            " " +
            new Date(rowData.createdOn).getHours() +
            ":" +
            new Date(rowData.createdOn).getMinutes()}
        </>
      ),
      filterPlaceholder: t("Search"),
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
          navigate(rowData.id + "/view");
        }
        break;
      case ButtonTypeEnum.ADD:
        resetWorkoutPlanDto();
        navigate("add");
        break;
      case ButtonTypeEnum.EDIT:
        if (rowData) {
          navigate(rowData.id + "/edit");
        }
        break;
      case ButtonTypeEnum.DELETE:
        if (rowData) {
          setWorkoutPlanDto(rowData);
          dialogControlDelete.showDialog();
        }
        break;

      default:
        break;
    }
  };

  const onDelete = async (): Promise<void> => {
    const response = await apiService
      .delete("WorkoutPlans", workoutPlanDto.id)
      .then(() => {
        dialogControlDelete.hideDialog();
        if (triggerRefreshDataTable.current)
          triggerRefreshDataTable.current(datatableDto);
      });
  };

  return (
    <>
      <Card title={t("Workout Plans")}>
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
      </Card>

      {/*                                       */}
      {/*          Delete Train Group           */}
      {/*                                       */}
      <GenericDialogComponent
        visible={isDeleteDialogVisible}
        control={dialogControlDelete}
        onDelete={onDelete}
        formMode={FormMode.DELETE}
      >
        <div className="flex justify-content-center">
          <p>{t("Are you sure")}?</p>
        </div>
      </GenericDialogComponent>
    </>
  );
}
