import {
  ColumnBodyOptions,
  ColumnEditorOptions,
  ColumnEvent,
} from "primereact/column";

export interface DataTableColumns<TEntity> {
  field: string;
  header: string;
  sortable: boolean;
  filter: boolean;
  filterPlaceholder: string;
  style: React.CSSProperties;
  // body?: (rowData: TEntity, options?: ColumnBodyOptions) => JSX.Element;
  body?: ((rowData: TEntity, options: ColumnBodyOptions) => any) | undefined;
  editor?: boolean; // enables row edit

  cellEditor?: (options: ColumnEditorOptions) => React.ReactNode;
  onCellEditInit?: (event: ColumnEvent) => void;
  onCellEditComplete?: (event: ColumnEvent) => void;
}
