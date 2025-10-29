import { Route, Routes } from "react-router-dom";
import { ScrollPanel } from "primereact/scrollpanel";
import NavTop from "./components/navbar-top/NavTop.tsx";
import Home from "./pages/home/Home.tsx";
import Administrator from "./pages/administrator/administrator.tsx";
import TrainGroupAdminCalendarPage from "./pages/train-group-admin/TrainGroupAdminCalendarPage.tsx";
import TrainGroupAdminPage from "./pages/train-group-admin/TrainGroupAdminPage.tsx";
import RolesPage from "./pages/role/RolesPage.tsx";
import UsersPage from "./pages/user/UsersPage.tsx";
import LoginPage from "./pages/user/LoginPage.tsx";
import TrainGroupsBookingCalendarPage from "./pages/train-group-booking/TrainGroupsBookingCalendarPage.tsx";
import TrainGroupAdminFormPage from "./pages/train-group-admin/TrainGroupAdminFormPage.tsx";
import UserProfilePage from "./pages/user/UserProfilePage.tsx";
import UserPasswordForgotPage from "./pages/user/UserPasswordForgotPage.tsx";
import UserPasswordResetPage from "./pages/user/UserPasswordResetPage.tsx";
import MailsPage from "./pages/mail/MailsPage.tsx";
import MailSendPage from "./pages/mail/MailSendPage.tsx";
import { FormMode } from "./enum/FormMode.tsx";
import RegisterPage from "./pages/user/RegisterPage.tsx";
import WorkoutPlansPage from "./pages/workout-plan/WorkoutPlansPage.tsx";
import WorkoutPlanFormPage from "./pages/workout-plan/WorkoutPlanFormPage.tsx";
import UserStatusesPage from "./pages/user-status/UserStatusesPage.tsx";
import { useEffect } from "react";
import { useTranslator } from "./services/TranslatorService.tsx";
import { LocalStorageService } from "./services/LocalStorageService.tsx";

export default function App() {
  const { t, setLanguage } = useTranslator();

  useEffect(() => {
    const language = LocalStorageService.getLanguage();
    if (!language) {
      setLanguage("el");
    }
  }, []);
  return (
    <>
      <div className="flex flex-column p-0 m-0 h-full">
        <div className="pt-0 pb-1">
          <NavTop />
        </div>

        <div className="pt-1 pb-0 overflow-hidden h-full">
          <ScrollPanel className="custombar1 h-full">
            <Routes>
              <Route
                path="/"
                element={<Home />}
              />

              <Route
                path="/appointment"
                element={<TrainGroupsBookingCalendarPage />}
              />

              <Route
                path="/workout-plans"
                element={<WorkoutPlansPage />}
              />

              {/* Administrator */}
              <Route
                path="/administrator/*"
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
                  element={<TrainGroupAdminFormPage formMode={FormMode.ADD} />}
                />
                <Route
                  path="train-groups/:id/edit"
                  element={<TrainGroupAdminFormPage formMode={FormMode.EDIT} />}
                />
                <Route
                  path="train-groups/:id/view"
                  element={<TrainGroupAdminFormPage formMode={FormMode.VIEW} />}
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
              </Route>

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
