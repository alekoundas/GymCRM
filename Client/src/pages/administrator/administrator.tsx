import { Column } from "primereact/column";
import { DataTable } from "primereact/datatable";
import { ScrollPanel } from "primereact/scrollpanel";
import { Route, Routes } from "react-router-dom";
import NavLeft from "../../components/navbar-left/NavLeft";
import CustomerForm from "../customer/CustomerForm";
import Customers from "../customer/Customers";
import MakerModels from "../maker-model/MakerModels";
import Makers from "../maker/Makers";
import Roles from "../role/Roles";
import Login from "../user/Login";
import Register from "../user/Register";
import Users from "../user/Users";

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
                path="customers"
                element={<Customers />}
              />
              <Route
                path="customers/add"
                element={<CustomerForm />}
              />
              <Route
                path="customers/:id/edit"
                element={<CustomerForm />}
              />
              <Route
                path="customers/:id/view"
                element={<CustomerForm />}
              />

              {/* Users */}
              <Route
                path="users"
                element={<Users />}
              />
              <Route
                path="roles"
                element={<Roles />}
              />

              {/* Lookups */}
              <Route
                path="makers"
                element={<Makers />}
              />
              <Route
                path="makermodels"
                element={<MakerModels />}
              />
            </Routes>
          </ScrollPanel>
        </div>
      </div>
    </>
  );
}
