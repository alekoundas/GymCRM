import { Column } from "primereact/column";
import { DataTable } from "primereact/datatable";

export default function Tickets() {
  //const [products, setProducts] = useState([]);
  //const [multiSortMeta, setmultiSortMeta] = useState([]);

  // useEffect(() => {
  //   const refrshData = async () => {
  //     var response = await ApiService.getDataGrid<CustomerDto[]>("customers", {
  //       data: [],
  //       first: 0,
  //       rows: 100,
  //       page: 1,
  //       multiSortMeta: [
  //         { field: "firstName", order: 1 },
  //         { field: "lastName", order: 1 },
  //       ],
  //       filters: {
  //         firstName: { value: "", matchMode: "contains" },
  //         lastName: { value: "", matchMode: "contains" },
  //       },
  //     });
  //   };

  //   refrshData();
  // }, []);

  return (
    <>
      <div className="card">
        <DataTable
          lazy
          sortMode="multiple"
          tableStyle={{ minWidth: "50rem" }}
        >
          <Column
            field="code"
            header="Code"
            sortable
            style={{ width: "25%" }}
          ></Column>
          <Column
            field="name"
            header="Name"
            sortable
            style={{ width: "25%" }}
          ></Column>
          <Column
            field="category"
            header="Category"
            sortable
            style={{ width: "25%" }}
          ></Column>
        </DataTable>
      </div>
    </>
  );
}
