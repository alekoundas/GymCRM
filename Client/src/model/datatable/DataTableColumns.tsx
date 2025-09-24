import {
  ColumnBodyOptions,
  ColumnEditorOptions,
  ColumnEvent,
  ColumnFilterElementTemplateOptions,
} from "primereact/column";
import { JSX } from "react";

export interface DataTableColumns<TEntity> {
  field: string;
  header: string;
  sortable: boolean;
  filter: boolean;
  filterPlaceholder: string;
  filterTemplate?: (
    options: ColumnFilterElementTemplateOptions
  ) => JSX.Element | undefined;

  style: React.CSSProperties;
  body?: ((rowData: TEntity, options: ColumnBodyOptions) => any) | undefined;
  // body?: (rowData: TEntity, options?: ColumnBodyOptions) => JSX.Element;

  editor?: boolean; // enables row edit
  cellEditor?: (options: ColumnEditorOptions) => React.ReactNode;
  onCellEditInit?: (event: ColumnEvent) => void;
  onCellEditComplete?: (event: ColumnEvent) => void;
}
