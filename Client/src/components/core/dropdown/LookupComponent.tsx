import { Dropdown, DropdownChangeEvent } from "primereact/dropdown";
import { JSX, useEffect, useState } from "react";
import { VirtualScrollerLazyEvent } from "primereact/virtualscroller";
import { LookupDto } from "../../../model/lookup/LookupDto";
import { LookupOptionDto } from "../../../model/lookup/LookupOptionDto";
import { useApiService } from "../../../services/ApiService";
import { useTranslator } from "../../../services/TranslatorService";
import { Tag } from "primereact/tag";
import { UserDto } from "../../../model/entities/user/UserDto";
import { Avatar } from "primereact/avatar";

interface IField {
  controller: string;
  selectedEntityId: string;
  isEnabled: boolean;
  onChange?: (entity: LookupOptionDto | undefined) => void;
}

export default function LookupComponent({
  controller,
  selectedEntityId,
  isEnabled,
  onChange,
}: IField) {
  const { t } = useTranslator();
  const apiService = useApiService();

  const [isDataLoaded, setIsDataLoaded] = useState(false); // used to escape lazyload firing again after dto update.
  const [loading, setLoading] = useState(false);
  const [lookupDto, setLookupDto] = useState<LookupDto>(new LookupDto());
  const [selectedId, setSelectedId] = useState<string | undefined>();

  const fetchData = async (dto: LookupDto) => {
    setLoading(true);
    dto.take = 10;
    const result = await apiService.getDataLookup(controller, dto);
    setLoading(false);
    return result;
  };

  const setSelectedOptionById = async (id: string) => {
    lookupDto.filter.id = id;
    const result = await fetchData(lookupDto);
    lookupDto.filter.id = undefined;

    if (result) setLookupDto(result);
  };

  // Load initial data and react to idValue changes
  useEffect(() => {
    if (selectedEntityId) {
      setSelectedOptionById(selectedEntityId);
      setSelectedId(selectedEntityId);
    }
  }, [selectedEntityId]);

  const onLazyLoad = async (event: VirtualScrollerLazyEvent) => {
    if (isDataLoaded) {
      setIsDataLoaded(false); // reset value
      return;
    }

    const currentLength = lookupDto.data?.length || 0;
    const requestedFirst = +event.first;
    const nextSkip = Math.max(requestedFirst, currentLength);

    if (currentLength > 0)
      if (nextSkip >= (lookupDto.totalRecords || 0)) {
        return;
      }

    lookupDto.skip = nextSkip;

    const result = await fetchData(lookupDto);
    if (result)
      setLookupDto({
        ...result,
        data: [...(lookupDto.data || []), ...(result.data || [])],
      });

    setIsDataLoaded(true); // escape next load.
  };

  const handleChange = (event: DropdownChangeEvent): void => {
    const value = event.value;
    if (!value) {
      lookupDto.filter.id = undefined;
      lookupDto.data = [];
      setSelectedId(undefined);
    } else {
      setSelectedId(value);
    }
    const entity: LookupOptionDto | undefined = lookupDto.data?.find(
      (x) => x.id === value
    );
    if (onChange) onChange(entity);
  };

  const template = (data: LookupOptionDto, props?: any): JSX.Element => {
    if (data)
      if (
        data.firstName &&
        data.lastName &&
        data.firstName?.length > 0 &&
        data.lastName?.length > 0
      ) {
        const userDto = new UserDto();
        userDto.firstName = data.firstName;
        userDto.lastName = data.lastName;
        userDto.profileImage = data.profileImage;
        userDto.userColor = data.userColor;
        const template = userTemplate(userDto);
        if (template) {
          return template;
        } else {
          return <span>{data.value}</span>;
        }
      } else {
        return <span>{data.value}</span>;
      }
    return <span>{props.placeholder}</span>;
  };

  const userTemplate = (user: UserDto | undefined): JSX.Element | undefined => {
    if (user) {
      const initials = `${user.firstName.charAt(0)}${user.lastName.charAt(
        0
      )}`.toUpperCase();
      const imageSrc = "data:image/png;base64," + user.profileImage;
      return (
        <div className="flex m-0 p-0 align-items-center">
          <Avatar
            image={user.profileImage ? imageSrc : ""}
            label={user.profileImage ? undefined : initials}
            shape="circle"
            size="normal"
            className=" mr-2 "
          />
          <Tag
            className="opacity-100"
            style={{
              backgroundColor: "#" + user.userColor,
            }}
          >
            {" " +
              user.firstName[0].toUpperCase() +
              user.firstName.slice(1, user.firstName.length) +
              " " +
              user.lastName[0].toUpperCase() +
              user.lastName.slice(1, user.lastName.length)}
          </Tag>
        </div>
      );
    }
  };
  return (
    <>
      <Dropdown
        optionLabel="value" // What to display in the dropdown
        optionValue="id" // The value binding (id)
        value={selectedId}
        onChange={handleChange}
        options={lookupDto.data}
        itemTemplate={template}
        valueTemplate={template}
        placeholder={t("Select a value")}
        className="w-full md:w-14rem"
        disabled={!isEnabled}
        showClear
        virtualScrollerOptions={{
          lazy: true,
          onLazyLoad: onLazyLoad,
          itemSize: 40,
          showLoader: false,
          loading: loading,
          delay: 100,
        }}
      />
    </>
  );
}
