import { ColumnEditorOptions, ColumnEvent } from "primereact/column";
import { DataTableRowEditCompleteEvent } from "primereact/datatable";

export interface DataTableColumns {
  field: string;
  header: string;
  sortable: boolean;
  filter: boolean;
  filterPlaceholder: string;
  style: React.CSSProperties;
  body: any | null;
  rowEditor?: boolean; // enables row edit

  cellEditor?: (options: ColumnEditorOptions) => React.ReactNode;
  onCellEditInit?: (event: ColumnEvent) => void;
  onCellEditComplete?: (event: ColumnEvent) => void;
}
