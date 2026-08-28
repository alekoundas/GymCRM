import { JSX, useState } from "react";
import {
  VirtualScrollerLazyEvent,
  VirtualScrollerLoadingTemplateOptions,
} from "primereact/virtualscroller";
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
  const [isDataLoaded, setIsDataLoaded] = useState(false); // used to escape lazyload firing again after dto update.
  const [searchValue, setSearchValue] = useState("");
  const [autoCompleteDto, setAutoCompleteDto] = useState<
    AutoCompleteDto<TEntity>
  >(new AutoCompleteDto());
  const [selectedEntityDtos, setSelectedEntityDtos] = useState<TEntity[]>([]);
  const apiService = useApiService();

  const fetchData = async (dto: AutoCompleteDto<TEntity>) => {
    dto.take = 10;

    const result = await apiService.getDataAutoComplete<TEntity>(
      controller,
      dto
    );

    return result;
  };

  const onLazyLoad = async (event: VirtualScrollerLazyEvent) => {
    if (isDataLoaded) {
      setIsDataLoaded(false); // reset value
      return;
    }

    const currentLength = autoCompleteDto.suggestions?.length || 0;
    const requestedFirst = +event.first;
    const nextSkip = Math.max(requestedFirst, currentLength);

    if (nextSkip >= (autoCompleteDto.totalRecords || 0)) {
      return;
    }

    const dto = { ...autoCompleteDto };
    dto.skip = nextSkip;
    dto.searchValue = searchValue;

    const result = await fetchData(dto);
    if (result)
      setAutoCompleteDto({
        ...result,
        suggestions: [
          ...(autoCompleteDto.suggestions || []),
          ...(result.suggestions || []),
        ],
      });

    setIsDataLoaded(true); // escape next load.
  };

  const handleChange = (event: AutoCompleteChangeEvent): void => {
    const value = event.value as TEntity[];
    if (onChange) onChange(value);
    setSelectedEntityDtos(value);
    setSearchValue("");
  };

  // Two calls on purpose: the page size has to be a real number, and the count
  // only comes back with a page, so the first one asks how many there are.
  const selectAll = async () => {
    setSelectingAll(true);

    const countDto = new AutoCompleteDto<TEntity>();
    countDto.take = 1;
    const countResult = await apiService.getDataAutoComplete<TEntity>(
      controller,
      countDto,
    );

    const totalRecords = countResult?.totalRecords ?? 0;
    if (totalRecords > 0) {
      const allDto = new AutoCompleteDto<TEntity>();
      allDto.take = totalRecords;

      const allResult = await apiService.getDataAutoComplete<TEntity>(
        controller,
        allDto,
      );

      const allEntities = allResult?.suggestions ?? [];
      setSelectedEntityDtos(allEntities);
      if (onChange) onChange(allEntities);
    }

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

    const newDto = new AutoCompleteDto<TEntity>();
    newDto.skip = 0;
    newDto.searchValue = searchQuery;

    const result = await fetchData(newDto);
    if (result) setAutoCompleteDto(result);
    setIsDataLoaded(true); // escape next load.
  };

  return (
    <>
      <div className="p-fluid">
        <AutoComplete
          id="recipients"
          multiple
          dropdown
          virtualScrollerOptions={{
            lazy: true,
            onLazyLoad: onLazyLoad,
            loadingTemplate: loadingTemplate,
            itemSize: 50,
            showLoader: false, // TODO: Enable this somehow....
            // loading: loading,
            delay: 200, // Reduced to minimize double triggers
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
