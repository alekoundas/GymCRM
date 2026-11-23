import { ColumnFilterElementTemplateOptions } from "primereact/column";
import {
  MultiSelect,
  MultiSelectChangeEvent,
  MultiSelectFilterEvent,
} from "primereact/multiselect";
import { useEffect, useRef, useState } from "react";
import { LookupDto } from "../../../model/lookup/LookupDto";
import { LookupOptionDto } from "../../../model/lookup/LookupOptionDto";
import { Avatar } from "primereact/avatar";
import {
  VirtualScrollerLazyEvent,
  VirtualScrollerLoadingTemplateOptions,
} from "primereact/virtualscroller";
import { classNames } from "primereact/utils";
import { Skeleton } from "primereact/skeleton";
import { useApiService } from "../../../services/ApiService";
import { formatUserName } from "../../../services/UserNameService";
import UserChipComponent from "../user/UserChipComponent";

interface IField {
  options: ColumnFilterElementTemplateOptions;
  controller: string;
}

export default function DataTableFilterIdComponent({
  options,
  controller,
}: IField) {
  const [searchValue, setSearchValue] = useState("");
  // The names shown as selected - the options' "value" field, which is their label.
  const [selectedEntities, setSelectedEntities] = useState<string[]>();
  const [isDataLoaded, setIsDataLoaded] = useState(false); // used to escape lazyload firing again after dto update.
  const [lookupDto, setLookupDto] = useState<LookupDto>(new LookupDto());
  const apiService = useApiService();

  // Options fetched by id to show a selection the grid restored, kept apart from the
  // paged list so they do not throw its paging out.
  const [restoredOptions, setRestoredOptions] = useState<LookupOptionDto[]>([]);

  // The ids this dropdown last handed the grid, so its own change is not mistaken
  // for one coming back from outside.
  const lastAppliedIds = useRef<string[]>([]);

  const fetchData = async (dto: LookupDto) => {
    dto.take = 1000;
    const result = apiService.getDataLookup(controller, { ...dto, data: [] });
    return result;
  };

  // Members read "First Last (username)", the same as their grid column. The username
  // also keeps two people with the same name apart, which matters here: a selection
  // is held by its label. Other lookups show their value as before.
  const isUserLookup = controller.toLowerCase() === "users";
  const labelOf = (option: LookupOptionDto): string =>
    isUserLookup
      ? formatUserName({
          firstName: option.firstName,
          lastName: option.lastName,
          userName: option.value,
        })
      : (option.value ?? "");

  // Everything the dropdown can show: the paged list, plus anything restored that
  // the list has not reached yet.
  //
  // The server has already searched, word by word and without accents. Primereact
  // sieves the options a second time with a plain "contains" on `filterBy`, which
  // threw those answers away - "γιαν παπ" is not a substring of "Γιάννης
  // Παπαδόπουλος". Each option carries the search itself in that field, so the
  // second pass keeps what the server found.
  const allOptions: (LookupOptionDto & { label: string; searchKey: string })[] = [
    ...restoredOptions.filter((x) => !(lookupDto.data ?? []).some((y) => y.id === x.id)),
    ...(lookupDto.data ?? []),
  ].map((x) => ({ ...x, label: labelOf(x), searchKey: searchValue }));

  // The grid holds the ids; this dropdown shows names. When the grid's value comes
  // from somewhere else - the url, on coming back to the page - the names for those
  // ids are looked up so the selection shows rather than sitting there as "Any"
  // while the rows are still filtered by it.
  useEffect(() => {
    // Cleared.
    if (options.value === null) {
      lastAppliedIds.current = [];
      setSelectedEntities([]);
      return;
    }

    const ids: string[] = (Array.isArray(options.value) ? options.value : [options.value])
      .filter((x: unknown) => typeof x === "string" && x.length > 0);

    const isOwnChange =
      ids.length === lastAppliedIds.current.length &&
      ids.every((x) => lastAppliedIds.current.includes(x));
    if (isOwnChange) return;

    lastAppliedIds.current = ids;
    if (ids.length === 0) {
      setSelectedEntities([]);
      return;
    }

    const restore = async () => {
      const known = allOptions.filter((x) => x.id && ids.includes(x.id));
      const missing = ids.filter((id) => !known.some((x) => x.id === id));

      const fetched = await Promise.all(
        missing.map((id) => {
          const dto = new LookupDto();
          dto.filter.id = id;
          return fetchData(dto);
        })
      );
      const found = fetched
        .flatMap((x) => x?.data ?? [])
        .filter((x) => x.id && ids.includes(x.id));

      setRestoredOptions((previous) => [
        ...previous.filter((x) => !found.some((y) => y.id === x.id)),
        ...found,
      ]);

      const matched: LookupOptionDto[] = [...known, ...found];
      setSelectedEntities(
        ids
          .map((id) => matched.find((x) => x.id === id))
          .filter((x): x is LookupOptionDto => !!x)
          .map(labelOf)
          .filter((x) => x.length > 0)
      );
    };

    restore();
  }, [options.value]);

  const onLazyLoad = async (event: VirtualScrollerLazyEvent) => {
    if (isDataLoaded) {
      setIsDataLoaded(false); // reset value
      return;
    }

    const currentLength = lookupDto.data?.length || 0;
    const requestedFirst = +event.first;
    const nextSkip = Math.max(requestedFirst, currentLength);

    if (lookupDto.data?.length ?? 0 > 0)
      if (nextSkip >= (lookupDto.totalRecords || 0)) {
        return;
      }

    const dto = { ...lookupDto };
    dto.skip = nextSkip;
    dto.filter.value = searchValue;

    const result = await fetchData(dto);
    if (result)
      setLookupDto({
        ...result,
        data: [...(lookupDto.data || []), ...(result.data || [])],
      });

    setIsDataLoaded(true); // escape next load.
  };

  const search = async (event: MultiSelectFilterEvent) => {
    const term = event.filter.trim();
    setSearchValue(term);

    const newDto = new LookupDto();
    newDto.skip = 0;
    // What was typed. It used to be left out, so the search box found nothing new.
    newDto.filter.value = term;

    const result = await fetchData(newDto);
    if (result) setLookupDto(result);
    setIsDataLoaded(true); // escape next load.
  };

  const getDisplayImageSrc = (
    profileImage: string | undefined,
  ): string | undefined => {
    if (!profileImage) return undefined;
    if (profileImage.startsWith("data:")) return profileImage;
    return `data:image/png;base64,${profileImage}`;
  };

  const itemTemplate: (option: LookupOptionDto) => React.ReactNode = (
    option,
  ) =>
    isUserLookup ? (
      <UserChipComponent
        user={{
          firstName: option.firstName,
          lastName: option.lastName,
          userName: option.value,
          profileImage: option.profileImage,
        }}
      />
    ) : (
      <div className="flex align-items-center gap-2">
        {option.profileImage && (
          <img
            alt={getDisplayImageSrc(option.profileImage ?? "")}
            src={getDisplayImageSrc(option.profileImage ?? "")}
            width="32"
          />
        )}
        <span>{option.value}</span>
      </div>
    );

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
  return (
    <MultiSelect
      value={selectedEntities}
      options={allOptions}
      itemTemplate={itemTemplate}
      onChange={(e: MultiSelectChangeEvent) => {
        setSelectedEntities(e.value);
        const ids = allOptions
          .filter((x) => (e.value as string[]).some((y) => y === x.label))
          .map((x) => x.id ?? "");
        lastAppliedIds.current = ids;
        options.filterApplyCallback(ids);
      }}
      virtualScrollerOptions={{
        lazy: true,
        onLazyLoad: onLazyLoad,
        loadingTemplate: loadingTemplate,
        itemSize: 50,
        showLoader: false, // TODO: Enable this somehow....
        // loading: loading,
        // delay: 200, // Reduced to minimize double triggers
        scrollHeight: "300px",
      }}
      filter
      filterDelay={400} // TODO: Why dis dont work
      onFilter={search}
      filterBy="searchKey"
      optionLabel="label"
      optionValue="label"
      placeholder="Any"
      className="p-column-filter"
      maxSelectedLabels={1}
      style={{ minWidth: "14rem" }}
    />
  );
}
