import { ColumnEditorOptions, ColumnEvent } from "primereact/column";

export interface DataTableColumns {
  field: string;
  header: string;
  sortable: boolean;
  filter: boolean;
  filterPlaceholder: string;
  style: React.CSSProperties;
  body: any | null;
  editor?: (options: ColumnEditorOptions) => React.ReactNode;
  onCellEditInit?: (event: ColumnEvent) => void;
  onCellEditComplete?: (event: ColumnEvent) => void;
}
