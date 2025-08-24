import { Dropdown, DropdownChangeEvent } from "primereact/dropdown";
import React, { useEffect, useState } from "react";
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
    try {
      const result = await ApiService.getDataLookup(controller, dto);
      console.log(`LookupComponent: API response for ${controller}:`, result); // Debug
      setLoading(false);
      return result;
    } catch (error) {
      console.error(`Failed to fetch data for ${controller}:`, error);
      setLoading(false);
      return null;
    }
  };

  const setSelectedOptionById = async (id: string) => {
    if (!id) return;
    const dto = new LookupDto();
    dto.filter.id = id;
    const result = await refreshData(dto);
    if (result?.data && result.data[0]?.id) {
      setSelectedOption(result.data[0].id);
      console.log("LookupComponent: Preselected id:", result.data[0].id); // Debug
    } else {
      console.warn(`LookupComponent: No option found for id ${id}`);
    }
  };

  // Load initial data and react to idValue changes
  useEffect(() => {
    if (idValue) {
      setSelectedOptionById(idValue);
    }
    // Load all options for the dropdown
    refreshData(new LookupDto()).then((result) => {
      if (result?.data) {
        setLookupDto({ ...result });
        // Check if idValue matches an option
        if (idValue && result.data.some((opt: any) => opt.id === idValue)) {
          setSelectedOption(idValue);
          console.log("LookupComponent: Matched idValue in options:", idValue); // Debug
        }
      }
    });
  }, [idValue, controller]);

  const onLazyLoad = async (event: VirtualScrollerLazyEvent) => {
    const result = await refreshData(lookupDto);
    if (result) {
      setLookupDto({ ...result });
    } else {
      setLookupDto(new LookupDto());
    }
  };

  const onSaveCustom = () => {
    if (onCustomSave && selectedOption) {
      onCustomSave(selectedOption).then((id) => {
        if (!id) return;
        setIsVisible(false);
        setSelectedOptionById(id.toString());
        refreshData(lookupDto).then((result) => {
          if (result) setLookupDto({ ...result });
          else setLookupDto(new LookupDto());
        });
      });
    }
  };

  const handleChange = (event: DropdownChangeEvent): void => {
    if (event.originalEvent) {
      if (onCustomChange) onCustomChange(false);
      setIsVisible(false);
      setSelectedOption(event.value);
    } else {
      if (onCustomChange) onCustomChange(true);
      if (allowCustom) setIsVisible(true);
      lookupDto.filter.value = event.value;
      setLookupDto({ ...lookupDto });
      refreshData(lookupDto).then((result) => {
        if (result) setLookupDto({ ...result });
        else setLookupDto(new LookupDto());
      });
      setSelectedOption(event.value);
    }
    if (onChange) onChange(event.value);
  };

  return (
    <>
      <div className="grid">
        <div className="col-9 pr-0">
          <div className="flex justify-center">
            <Dropdown
              optionLabel="value" // What to display in the dropdown
              optionValue="id" // The value binding (id)
              value={selectedOption}
              onChange={handleChange}
              editable={isEditable}
              options={lookupDto.data}
              placeholder="Select a value"
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
