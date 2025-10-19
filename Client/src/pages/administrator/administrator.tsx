import { Route, Routes } from "react-router-dom";
import TrainGroupAdminPage from "../train-group-admin/TrainGroupAdminPage";
import TrainGroupAdminCalendarPage from "../train-group-admin/TrainGroupAdminCalendarPage";
import RolesPage from "../role/RolesPage";
import UsersPage from "../user/UsersPage";
import TrainGroupAdminFormPage from "../train-group-admin/TrainGroupAdminFormPage";
import { FormMode } from "../../enum/FormMode";
import MailsPage from "../mail/MailsPage";
import MailSendPage from "../mail/MailSendPage";
import ChartsComponent from "./ChartsComponent";
import UserProfilePage from "../user/UserProfilePage";
import NavLeft from "../../components/navbar-left/NavLeft";
import WorkoutPlansPage from "../workout-plan/WorkoutPlansPage";
import WorkoutPlanFormPage from "../workout-plan/WorkoutPlanFormPage";
import UserStatusesPage from "../user-status/UserStatusesPage";

export default function Administrator() {
  return (
    <>
      <div className="grid h-full">
        <div className="col-2 ">
          <NavLeft />
        </div>
        <div className="col-10">
          {/* <ScrollPanel className="custombar2 h-full w-full"> */}
          <Routes>
            <Route
              index
              element={<ChartsComponent />}
            />

            <Route
              path="train-group-calendar"
              element={<TrainGroupAdminCalendarPage />}
            />
            <Route
              path="train-groups"
              element={<TrainGroupAdminPage />}
            />

            <Route
              path="train-groups/:id/view"
              element={<TrainGroupAdminFormPage formMode={FormMode.VIEW} />}
            />
            <Route
              path="train-groups/add"
              element={<TrainGroupAdminFormPage formMode={FormMode.ADD} />}
            />
            <Route
              path="train-groups/:id/edit"
              element={<TrainGroupAdminFormPage formMode={FormMode.EDIT} />}
            />

            <Route
              path="emails"
              element={<MailsPage />}
            />
            <Route
              path="email-send"
              element={<MailSendPage />}
            />

            <Route
              path="workout-plans"
              element={<WorkoutPlansPage />}
            />
            <Route
              path="workout-plans/add"
              element={<WorkoutPlanFormPage formMode={FormMode.ADD} />}
            />
            <Route
              path="workout-plans/:id/edit"
              element={<WorkoutPlanFormPage formMode={FormMode.EDIT} />}
            />
            <Route
              path="workout-plans/:id/view"
              element={<WorkoutPlanFormPage formMode={FormMode.VIEW} />}
            />

            {/* Users */}
            <Route
              path="users"
              element={<UsersPage />}
            />
            <Route
              path="roles"
              element={<RolesPage />}
            />

            <Route
              path="users/:id/profile"
              element={<UserProfilePage />}
            />

            <Route
              path="user-statuses"
              element={<UserStatusesPage />}
            />
          </Routes>
          {/* </ScrollPanel> */}
        </div>
      </div>
    </>
  );
}
