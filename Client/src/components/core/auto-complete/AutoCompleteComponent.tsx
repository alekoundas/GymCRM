import { JSX, useState } from "react";
import { VirtualScrollerLoadingTemplateOptions } from "primereact/virtualscroller";
import { useApiService } from "../../../services/ApiService";
import {
  AutoComplete,
  AutoCompleteChangeEvent,
  AutoCompleteCompleteEvent,
} from "primereact/autocomplete";
import { AutoCompleteDto } from "../../../model/core/auto-complete/AutoCompleteDto";
import { classNames } from "primereact/utils";
import { Skeleton } from "primereact/skeleton";
import { Button } from "primereact/button";
import { useTranslator } from "../../../services/TranslatorService";

interface IField<TEntity> {
  controller: string;
  existingIds?: string[];
  isEnabled: boolean;
  // Adds a button that pulls the whole list in one go. Off unless asked for - it
  // only makes sense where selecting everything is a reasonable thing to do.
  isSelectAllVisible?: boolean;
  itemTemplate: (data: TEntity) => JSX.Element;
  selectedItemTemplate: (data: TEntity) => JSX.Element;
  onChange?: (entities: TEntity[]) => void;
}

export default function AutoCompleteComponent<TEntity>({
  controller,
  existingIds,
  isEnabled,
  isSelectAllVisible,
  itemTemplate,
  selectedItemTemplate,
  onChange,
}: IField<TEntity>) {
  const { t } = useTranslator();
  const [isSelectingAll, setSelectingAll] = useState(false);
  const [loading, setLoading] = useState(false);
  const [searchValue, setSearchValue] = useState("");
  const [autoCompleteDto, setAutoCompleteDto] = useState<
    AutoCompleteDto<TEntity>
  >(new AutoCompleteDto());
  const [selectedEntityDtos, setSelectedEntityDtos] = useState<TEntity[]>([]);
  const apiService = useApiService();

  const fetchData = async (dto: AutoCompleteDto<TEntity>) => {
    setLoading(true);
    dto.take = 1000;

    const result = await apiService.getDataAutoComplete<TEntity>(
      controller,
      dto
    );

    setLoading(false);
    return result;
  };

  const loadPage = async (skip: number, value: string) => {
    const dto = new AutoCompleteDto<TEntity>();
    dto.skip = skip;
    dto.searchValue = value;
    return await fetchData(dto);
  };

  // A page at a time until there are no more, rather than a request per scroll tick.
  // The list opens with the first thousand already in it and the rest arrive behind
  // it, so scrolling never waits on the network.
  const load = async (value: string) => {
    const first = await loadPage(0, value);
    if (!first) return;

    let all = first.suggestions ?? [];
    setAutoCompleteDto({ ...first, suggestions: all });

    while (all.length < (first.totalRecords ?? 0)) {
      const next = await loadPage(all.length, value);
      if (!next?.suggestions?.length) break;

      all = [...all, ...next.suggestions];
      setAutoCompleteDto({ ...next, suggestions: all });
    }
  };

  const handleChange = (event: AutoCompleteChangeEvent): void => {
    const value = event.value as TEntity[];
    if (onChange) onChange(value);
    setSelectedEntityDtos(value);
    setSearchValue("");
  };

  const selectAll = async () => {
    setSelectingAll(true);

    // Pages through the lot, the same way the list itself fills.
    let all: TEntity[] = [];
    let total = 0;

    do {
      const page = await loadPage(all.length, "");
      if (!page?.suggestions?.length) break;

      all = [...all, ...page.suggestions];
      total = page.totalRecords ?? 0;
    } while (all.length < total);

    setSelectedEntityDtos(all);
    if (onChange) onChange(all);

    setSelectingAll(false);
  };

  const clearSelection = () => {
    setSelectedEntityDtos([]);
    if (onChange) onChange([]);
  };

  const loadingTemplate = (options: VirtualScrollerLoadingTemplateOptions) => {
    const className = classNames("flex align-items-center p-2", {
      odd: options.odd,
    });

    return (
      <div
        className={className}
        style={{ height: "50px" }}
      >
        <Skeleton
          width={options.even ? "60%" : "50%"}
          height="1.3rem"
        />
      </div>
    );
  };

  const search = async (event: AutoCompleteCompleteEvent) => {
    // Only add search filter if query is non-empty
    let searchQuery = searchValue;
    if (event.query?.trim() && event.originalEvent.type !== "click") {
      searchQuery = event.query.trim();
      setSearchValue(searchQuery);
    }

    await load(searchQuery);
  };

  return (
    <>
      <div className="p-fluid">
        <AutoComplete
          id="recipients"
          multiple
          dropdown
          // Not lazy any more. The rows are all in memory by the time they are
          // scrolled to, so the scroller only has to draw them - which is what
          // stopped the list arriving ten at a time.
          virtualScrollerOptions={{
            loadingTemplate: loadingTemplate,
            itemSize: 50,
            showLoader: false,
            loading: loading,
            scrollHeight: "300px",
          }}
          value={selectedEntityDtos}
          suggestions={autoCompleteDto.suggestions ?? ([] as any)}
          itemTemplate={itemTemplate}
          selectedItemTemplate={selectedItemTemplate}
          completeMethod={search}
          onChange={handleChange}
          disabled={!isEnabled}
          inputStyle={{ width: "100%" }}
          style={{ width: "100%" }}
          className="w-full"
        />
      </div>

      {isSelectAllVisible && (
        <div className="flex align-items-center gap-2 mt-2">
          <Button
            type="button"
            label={t("Select all")}
            icon="pi pi-users"
            className="p-button-sm p-button-outlined"
            disabled={!isEnabled || isSelectingAll}
            loading={isSelectingAll}
            onClick={selectAll}
          />
          {selectedEntityDtos.length > 0 && (
            <>
              <Button
                type="button"
                label={t("Clear")}
                icon="pi pi-times"
                className="p-button-sm p-button-text p-button-secondary"
                disabled={!isEnabled || isSelectingAll}
                onClick={clearSelection}
              />
              <span className="text-sm text-color-secondary">
                {selectedEntityDtos.length} {t("Selected")}
              </span>
            </>
          )}
        </div>
      )}
    </>
  );
}
