import { Route, Routes } from "react-router-dom";
import { ScrollPanel } from "primereact/scrollpanel";
import { ToastService } from "./services/ToastService.tsx";
import { ThemeService } from "./services/ThemeService.tsx";
import { useEffect, useRef } from "react";
import { Toast } from "primereact/toast";

import NavTop from "./components/navbar-top/NavTop.tsx";
import NavLeft from "./components/navbar-left/NavLeft.tsx";
import Home from "./pages/home/Home.tsx";
import Customers from "./pages/customer/Customers.tsx";
import CustomerForm from "./pages/customer/CustomerForm.tsx";
import Tickets from "./pages/ticket/Tickets.tsx";
import Login from "./pages/user/Login.tsx";
import ApiService from "./services/ApiService.tsx";
import Roles from "./pages/role/Roles.tsx";
import Users from "./pages/user/Users.tsx";
import Makers from "./pages/maker/Makers.tsx";
import MakerModels from "./pages/maker-model/MakerModels.tsx";

export default function App() {
  // Set Toast messages here
  const toast = useRef(null);
  const theme = useRef(null);

  useEffect(() => {
    ToastService.setToastRef(toast);
    ThemeService.setRef(theme);
    ThemeService.setDefaultTheme();
    ThemeService.setDefaultThemeScale();

    ApiService.test();
  }, []);

  return (
    <>
      {/* Theme switching here. */}
      <link
        ref={theme}
        rel="stylesheet"
        type="text/css"
      />
      <Toast ref={toast} />

      <div className="grid   ">
        <div className="col-12 ">
          <NavTop />
        </div>
        <div className="col-2 ">
          <NavLeft />
        </div>
        <div className="col-10 ">
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

              <Route
                path="/makers"
                element={<Makers />}
              />

              <Route
                path="/makermodels"
                element={<MakerModels />}
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
