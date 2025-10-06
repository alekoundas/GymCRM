import { createContext, useContext, useRef, ReactNode } from "react";
import { Toast } from "primereact/toast";

type ToastSeverity =
  | "success"
  | "info"
  | "warn"
  | "error"
  | "secondary"
  | "contrast";

interface ToastMessage {
  severity: ToastSeverity;
  summary: string;
  detail: string;
  life?: number;
}

interface ToastContextType {
  showSuccess: (message: string) => void;
  showInfo: (message: string) => void;
  showWarn: (message: string) => void;
  showError: (message: string) => void;
  showSecondary: (message: string) => void;
  showContrast: (message: string) => void;
}

const ToastContext = createContext<ToastContextType | undefined>(undefined);

export const useToast = () => {
  const context = useContext(ToastContext);
  if (context === undefined) {
    throw new Error("useToast must be used within a ToastProvider");
  }
  return context;
};

export function ToastProvider({ children }: { children: ReactNode }) {
  const toastRef = useRef<Toast>(null);

  const showToast = (message: ToastMessage) => {
    toastRef.current?.show(message);
  };

  const showSuccess = (message: string) => {
    showToast({
      severity: "success",
      summary: "Success",
      detail: message,
    });
  };

  const showInfo = (message: string) => {
    showToast({
      severity: "info",
      summary: "Info",
      detail: message,
      life: 2000,
    });
  };

  const showWarn = (message: string) => {
    showToast({
      severity: "warn",
      summary: "Warning",
      detail: message,
      life: 5000,
    });
  };

  const showError = (message: string) => {
    showToast({
      severity: "error",
      summary: "Error",
      detail: message,
      life: 5000,
    });
  };

  const showSecondary = (message: string) => {
    showToast({
      severity: "secondary",
      summary: "Secondary",
      detail: message,
      life: 3000,
    });
  };

  const showContrast = (message: string) => {
    showToast({
      severity: "contrast",
      summary: "Contrast",
      detail: message,
      life: 3000,
    });
  };

  const value: ToastContextType = {
    showSuccess,
    showInfo,
    showWarn,
    showError,
    showSecondary,
    showContrast,
  };

  return (
    <ToastContext.Provider value={value}>
      <Toast ref={toastRef} />
      {children}
    </ToastContext.Provider>
  );
}
