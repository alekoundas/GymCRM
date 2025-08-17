import { Dropdown, DropdownChangeEvent } from "primereact/dropdown";
import React, { useState } from "react";
import ApiService from "../../services/ApiService";
import { VirtualScrollerLazyEvent } from "primereact/virtualscroller";
import { LookupDto } from "../../model/lookup/LookupDto";
import { Button } from "primereact/button";

interface IField {
  controller: string;
  idValue: string;
  isEditable: boolean;
  isEnabled: boolean;
  allowCustom: boolean;
  onCustomSave?: (value: string) => Promise<number | null>;
  onCustomChange?: (isCustom: boolean) => void;
  onChange?: (id: string) => void;
}

export default function LookupComponent({
  controller,
  idValue,
  isEditable,
  isEnabled,
  allowCustom,
  onCustomChange,
  onCustomSave,
  onChange,
}: IField) {
  const [isVisible, setIsVisible] = useState(false);
  const [loading, setLoading] = useState(false);
  const [lookupDto, setLookupDto] = useState<LookupDto>(new LookupDto());
  const [selectedOption, setSelectedOption] = useState<string | null>(null);

  const refreshData = async (dto: LookupDto) => {
    setLoading(true);

    return await ApiService.getDataLookup(controller, dto).then((result) => {
      lookupDto.data = result?.data;
      setLoading(false);
      return result;
    });
  };

  const setSelectedOptionById = (id: string) => {
    const dto = new LookupDto();
    dto.filter.id = id;
    refreshData(dto).then((x) => {
      if (x?.data && x?.data[0]?.value) {
        setSelectedOption(x.data[0].value);
      }
    });
  };

  // Load intitial data.
  React.useEffect(() => {
    if (idValue) {
      setSelectedOptionById(idValue);
    }
  }, []);

  const onLazyLoad = async (_event: VirtualScrollerLazyEvent) => {
    refreshData(lookupDto).then((resultLookupDto) => {
      if (resultLookupDto) setLookupDto({ ...resultLookupDto });
      else setLookupDto(new LookupDto());
    });
  };

  const onSaveCustom = () => {
    if (onCustomSave && selectedOption)
      onCustomSave(selectedOption).then((id) => {
        if (!id) return;

        setIsVisible(false);
        setSelectedOptionById(id.toString());
        refreshData(lookupDto).then((resultLookupDto) => {
          if (resultLookupDto) setLookupDto({ ...resultLookupDto });
          else setLookupDto(new LookupDto());
        });
      });
  };

  const handleChange = (event: DropdownChangeEvent): void => {
    // Stupid PrimeRact Dropdown doesnt retrieve object but just a string.
    // This "if" statement means that user selected an option.
    if (event.originalEvent) {
      if (onCustomChange) onCustomChange(false);

      setIsVisible(false);
      setSelectedOption(event.value);
    }

    // This else statement means that user set a custom value.
    else {
      if (onCustomChange) onCustomChange(true);
      if (allowCustom) setIsVisible(true);

      lookupDto.filter.value = event.value;
      setLookupDto({ ...lookupDto });
      refreshData(lookupDto).then((resultLookupDto) => {
        if (resultLookupDto) setLookupDto({ ...resultLookupDto });
        else setLookupDto(new LookupDto());
      });
      setSelectedOption(event.value);
    }

    if (onChange) onChange(event.value);
  };

  return (
    <>
      <div className="grid ">
        <div className="col-9 pr-0">
          <div className="flex justify-center">
            <Dropdown
              optionLabel="value" // What to display in the dropdown
              optionValue="id" // The value binding (id)
              value={selectedOption}
              onChange={handleChange}
              editable={isEditable}
              options={lookupDto.data}
              placeholder="Select a Role"
              className="w-full md:w-14rem"
              disabled={!isEnabled}
              virtualScrollerOptions={{
                lazy: true,
                onLazyLoad: onLazyLoad,
                itemSize: 38,
                showLoader: true,
                loading: loading,

                delay: 250,
              }}
            />
          </div>
        </div>
        <div className="col-3 pl-0">
          <Button
            type="button"
            visible={isVisible}
            icon="pi pi-save"
            onClick={onSaveCustom}
          />
        </div>
      </div>
    </>
  );
}
