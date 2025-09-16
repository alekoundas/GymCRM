import React, { FormEvent, SyntheticEvent, useEffect, useState } from "react";
import { InputText } from "primereact/inputtext";
import { FormMode } from "../../enum/FormMode";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { usePhoneNumberStore } from "../../stores/PhoneNumberStore";
import { InputSwitch } from "primereact/inputswitch";

interface IField extends DialogChildProps {}

export default function PhoneNumberFormComponent({ formMode }: IField) {
  const { phoneNumberDto, updatePhoneNumberDto } = usePhoneNumberStore();

  return (
    <div className="flex flex-column md:flex-row justify-content-center ">
      <div className="field pr-8">
        <label
          htmlFor="number"
          className="block text-900 font-medium mb-2"
        >
          Phone Number
        </label>
        <InputText
          id="number"
          name="Number"
          type="text"
          placeholder="Number"
          value={phoneNumberDto.number}
          onChange={(x) => updatePhoneNumberDto({ number: x.target.value })}
          disabled={formMode === FormMode.VIEW}
        />
      </div>

      {formMode !== FormMode.ADD && (
        <div className="field ">
          <label
            htmlFor="isPrimary"
            className="block text-900 font-medium mb-2"
          >
            Primary
          </label>
          <InputSwitch
            id="isPrimary"
            name="Primary"
            checked={phoneNumberDto.isPrimary}
            onChange={(x) => updatePhoneNumberDto({ isPrimary: x.value })}
            disabled={formMode === FormMode.VIEW}
          />
        </div>
      )}
    </div>
  );
}
