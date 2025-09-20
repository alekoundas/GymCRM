import { Route, Routes } from "react-router-dom";
import { ScrollPanel } from "primereact/scrollpanel";
import { ToastService } from "./services/ToastService.tsx";
import { ThemeService } from "./services/ThemeService.tsx";
import { useEffect, useRef } from "react";
import { Toast } from "primereact/toast";

import NavTop from "./components/core/navbar-top/NavTop.tsx";
import Home from "./pages/home/Home.tsx";
import Administrator from "./pages/administrator/administrator.tsx";
import TrainGroupAdminCalendarPage from "./pages/train-group-admin/TrainGroupAdminCalendarPage.tsx";
import TrainGroupAdminPage from "./pages/train-group-admin/TrainGroupAdminPage.tsx";
import RolesPage from "./pages/role/RolesPage.tsx";
import UsersPage from "./pages/user/UsersPage.tsx";
import LoginPage from "./pages/user/LoginPage.tsx";
import RegisterPage from "./pages/user/RegisterPage.tsx";
import TrainGroupsBookingCalendarPage from "./pages/train-group-booking/TrainGroupsBookingCalendarPage.tsx";
import TrainGroupAdminFormPage from "./pages/train-group-admin/TrainGroupAdminFormPage.tsx";
import UserProfilePage from "./pages/user/UserProfilePage.tsx";
import UserPasswordForgotPage from "./pages/user/UserPasswordForgotPage.tsx";
import UserPasswordResetPage from "./pages/user/UserPasswordResetPage.tsx";
import MailsPage from "./pages/mail/MailsPage.tsx";

export default function App() {
  // Set Toast messages here
  const toast = useRef(null);
  const theme = useRef(null);

  useEffect(() => {
    ToastService.setToastRef(toast);
    ThemeService.setRef(theme);
    ThemeService.setDefaultTheme();
    ThemeService.setDefaultThemeScale();
  }, []);

  return (
    <>
      {/* Theme switching here. */}
      <link
        ref={theme}
        rel="stylesheet"
        type="text/css"
      />

      {/* Display messages */}
      <Toast ref={toast} />

      <div className="grid">
        <div className="col-12 ">
          <NavTop />
        </div>
        {/* <div className="col-2 ">
          <NavLeft />
        </div> */}
        <div className="col-12">
          <ScrollPanel
            style={{ height: "100%", width: "100%" }}
            className="custombar2"
          >
            <Routes>
              <Route
                path="/"
                element={<Home />}
              />

              <Route
                path="/appointment"
                element={<TrainGroupsBookingCalendarPage />}
              />

              {/* Administrator */}
              <Route
                path="/administrator"
                element={<Administrator />}
              >
                <Route
                  path="train-group-calendar"
                  element={<TrainGroupAdminCalendarPage />}
                />
                <Route
                  path="train-groups"
                  element={<TrainGroupAdminPage />}
                />
                <Route
                  path="train-groups/add"
                  element={<TrainGroupAdminFormPage />}
                />
                <Route
                  path="train-groups/:id/edit"
                  element={<TrainGroupAdminFormPage />}
                />
                <Route
                  path="train-groups/:id/view"
                  element={<TrainGroupAdminFormPage />}
                />

                <Route
                  path="emails"
                  element={<MailsPage />}
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
              </Route>

              {/* Users */}
              <Route
                path="/users/login"
                element={<LoginPage />}
              />
              <Route
                path="/users/register"
                element={<RegisterPage />}
              />
              <Route
                path="/users/profile"
                element={<UserProfilePage />}
              />
              <Route
                path="/users/forgot-password"
                element={<UserPasswordForgotPage />}
              />
              <Route
                path="/users/reset-password"
                element={<UserPasswordResetPage />}
              />
            </Routes>
          </ScrollPanel>
        </div>
      </div>
    </>
  );
}
