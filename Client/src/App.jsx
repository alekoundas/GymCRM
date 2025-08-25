import { Route, Routes } from "react-router-dom";
import { ScrollPanel } from "primereact/scrollpanel";
import { ToastService } from "./services/ToastService.tsx";
import { ThemeService } from "./services/ThemeService.tsx";
import { useEffect, useRef } from "react";
import { Toast } from "primereact/toast";

import NavTop from "./components/core/navbar-top/NavTop.tsx";
import Home from "./pages/home/Home.tsx";
import Login from "./pages/user/Login.tsx";
import Roles from "./pages/role/Roles.tsx";
import Users from "./pages/user/Users.tsx";
import Register from "./pages/user/Register.tsx";
import Administrator from "./pages/administrator/administrator.tsx";
import Appointment from "./pages/appointment/Appointment.tsx";
import TrainGroupAdminPage from "./pages/train-group/TrainGroupAdminPage.tsx";

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
                element={<Appointment />}
              />

              {/* Administrator */}
              <Route
                path="/administrator"
                element={<Administrator />}
              >
                <Route
                  path="train-groups"
                  element={<TrainGroupAdminPage />}
                />
                <Route
                  path="train-groups/add"
                  element={<TrainGroupAdminPage />}
                />
                <Route
                  path="train-groups/:id/edit"
                  element={<TrainGroupAdminPage />}
                />
                <Route
                  path="train-groups/:id/view"
                  element={<TrainGroupAdminPage />}
                />

                {/* Users */}
                <Route
                  path="users/login"
                  element={<Login />}
                />
                <Route
                  path="users/register"
                  element={<Register />}
                />
                <Route
                  path="users"
                  element={<Users />}
                />
                <Route
                  path="roles"
                  element={<Roles />}
                />

                {/* Lookups */}
                {/* <Route
                  path="makers"
                  element={<Makers />}
                />
                <Route
                  path="makermodels"
                  element={<MakerModels />}
                /> */}
              </Route>

              {/* Users */}
              <Route
                path="/users/login"
                element={<Login />}
              />
              <Route
                path="/users/register"
                element={<Register />}
              />
            </Routes>
          </ScrollPanel>
        </div>
      </div>
      {/* 
      <div className=" w-full ">
        <div className="flex flex-row pb-3">
          <div className="flex flex-column w-full">
            <NavTop />
          </div>
        </div>

        <div className="flex flex-row  gap-3">
          <div className="flex-column">
            <NavLeft />
          </div>

          <div className="flex-column flex-auto ">
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
                  path="/customers"
                  element={<Customers />}
                />
                <Route
                  path="/customers/add"
                  element={<CustomerForm />}
                />
                <Route
                  path="/customers/:id/edit"
                  element={<CustomerForm />}
                />
                <Route
                  path="/customers/:id/view"
                  element={<CustomerForm />}
                />

                <Route
                  path="/tickets"
                  element={<Tickets />}
                />

                <Route
                  path="/users/login"
                  element={<Login />}
                />
                <Route
                  path="/users"
                  element={<Users />}
                />

                <Route
                  path="/roles"
                  element={<Roles />}
                />
              </Routes>
            </ScrollPanel>
          </div>
        </div>
      </div> */}
    </>
  );
}
