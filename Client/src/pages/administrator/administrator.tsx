import { ScrollPanel } from "primereact/scrollpanel";
import { Route, Routes } from "react-router-dom";
import NavLeft from "../../components/core/navbar-left/NavLeft";
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
          </Routes>
          {/* </ScrollPanel> */}
        </div>
      </div>
    </>
  );
}
