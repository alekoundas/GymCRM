import { ColumnFilterElementTemplateOptions } from "primereact/column";
import {
  MultiSelect,
  MultiSelectChangeEvent,
  MultiSelectFilterEvent,
} from "primereact/multiselect";
import { useEffect, useState } from "react";
import ApiService from "../../../services/ApiService";
import { LookupDto } from "../../../model/lookup/LookupDto";
import { LookupOptionDto } from "../../../model/lookup/LookupOptionDto";
import { Avatar } from "primereact/avatar";
import {
  VirtualScrollerLazyEvent,
  VirtualScrollerLoadingTemplateOptions,
} from "primereact/virtualscroller";
import { classNames } from "primereact/utils";
import { Skeleton } from "primereact/skeleton";

interface IField {
  options: ColumnFilterElementTemplateOptions;
  controller: string;
}

export default function DataTableFilterIdComponent({
  options,
  controller,
}: IField) {
  // const [lookupOptions, setLookupOptions] = useState<LookupOptionDto[]>();
  const [searchValue, setSearchValue] = useState("");
  const [selectedEntities, setSelectedEntities] = useState<LookupOptionDto[]>();
  const [isDataLoaded, setIsDataLoaded] = useState(false); // used to escape lazyload firing again after dto update.
  const [lookupDto, setLookupDto] = useState<LookupDto>(new LookupDto());

  // // Load lookup data.
  // useEffect(() => {
  //   const lookupDto: LookupDto = { take: 1000, filter: {} };
  //   ApiService.getDataLookup(controller, lookupDto).then((response) => {
  //     if (response?.data) {
  //       setLookupOptions(response.data);
  //     }
  //   });
  // }, []);

  const fetchData = async (dto: LookupDto) => {
    dto.take = 10;

    // const lookupDto: LookupDto = { take: 10, filter: {} };
    const result = ApiService.getDataLookup(controller, dto);
    // .then(
    //   (response) => {
    //     if (response?.data) {
    //       setLookupOptions(response.data);
    //     }
    //   }
    // );

    return result;
  };

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
    setSearchValue(event.filter.trim());

    const newDto = new LookupDto();
    newDto.skip = 0;

    const result = await fetchData(newDto);
    if (result) setLookupDto(result);
    setIsDataLoaded(true); // escape next load.
  };

  const getDisplayImageSrc = (
    profileImage: string | undefined
  ): string | undefined => {
    if (!profileImage) return undefined;
    if (profileImage.startsWith("data:")) return profileImage;
    return `data:image/png;base64,${profileImage}`;
  };

  const itemTemplate: (option: LookupOptionDto) => React.ReactNode = (
    option
  ) => (
    <div className="flex align-items-center gap-2">
      {option.profileImage && (
        <img
          alt={getDisplayImageSrc(option.profileImage ?? "")}
          src={getDisplayImageSrc(option.profileImage ?? "")}
          width="32"
        />
        // <Avatar
        //   image={getDisplayImageSrc(option.profileImage ?? "")}
        //   label={getDisplayImageSrc(option.profileImage ?? "")}
        //   shape="circle"
        //   size="normal"
        //   className=" mr-2 "
        // />
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
      options={lookupDto.data}
      itemTemplate={itemTemplate}
      onChange={(e: MultiSelectChangeEvent) => {
        setSelectedEntities(e.value);
        const ids = lookupDto.data
          ?.filter((x) => e.value.some((y: any) => y === x.value))
          .map((x) => x.id);
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
      optionLabel="value"
      placeholder="Any"
      className="p-column-filter"
      maxSelectedLabels={1}
      style={{ minWidth: "14rem" }}
    />
  );
}
