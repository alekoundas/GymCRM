import { Toast } from "primereact/toast";
import { RefObject } from "react";

export namespace ToastService {
  let toastRef: RefObject<Toast>;

  export function setToastRef(toastRefValue: RefObject<Toast>) {
    toastRef = toastRefValue;
  }

  export function showSuccess(message: string) {
    toastRef.current?.show({
      severity: "success",
      summary: "Success",
      detail: message,
    });
  }

  export function showInfo(message: string) {
    toastRef.current?.show({
      severity: "info",
      summary: "Info",
      detail: message,
      life: 3000,
    });
  }

  export function showWarn(message: string) {
    toastRef.current?.show({
      severity: "warn",
      summary: "Warning",
      detail: message,
      life: 3000,
    });
  }

  export function showError(message: string) {
    toastRef.current?.show({
      severity: "error",
      summary: "Error",
      detail: message,
      life: 3000,
    });
  }

  export function showSecondary(message: string) {
    toastRef.current?.show({
      severity: "secondary",
      summary: "Secondary",
      detail: message,
      life: 3000,
    });
  }

  export function showContrast(message: string) {
    toastRef.current?.show({
      severity: "contrast",
      summary: "Contrast",
      detail: message,
      life: 3000,
    });
  }
}
