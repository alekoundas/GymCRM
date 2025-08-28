import { ScrollPanel } from "primereact/scrollpanel";
import { Route, Routes } from "react-router-dom";
import NavLeft from "../../components/core/navbar-left/NavLeft";
import TrainGroupAdminPage from "../train-group/TrainGroupAdminPage";
import TrainGroupAdminCalendarPage from "../train-group/TrainGroupAdminCalendarPage";
import RolesPage from "../role/RolesPage";
import UsersPage from "../user/UsersPage";

export default function Administrator() {
  return (
    <>
      <div className="grid">
        <div className="col-2 ">
          <NavLeft />
        </div>
        <div className="col-10">
          <ScrollPanel
            style={{ height: "100%", width: "100%" }}
            className="custombar2"
          >
            <Routes>
              <Route
                path="train-group-calendar"
                element={<TrainGroupAdminCalendarPage />}
              />
              <Route
                path="train-groups"
                element={<TrainGroupAdminPage />}
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
            </Routes>
          </ScrollPanel>
        </div>
      </div>
    </>
  );
}
