import { Component, ErrorInfo, ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";
import { Card } from "primereact/card";
import { useTranslator } from "../../../services/TranslatorService";

interface IField {
  children: ReactNode;
  // A new value clears the error, so moving to another page leaves the broken one
  // behind instead of keeping the error screen up.
  resetKey: string;
}

interface IState {
  error: Error | null;
  componentStack: string;
}

//
//          Without this, an error thrown while drawing a page took the whole app
//          down with it. React removes everything when nothing catches an error,
//          and with the dark colour scheme that left an empty black window - with
//          nothing to tell anybody what had happened.
//
export default class ErrorBoundaryComponent extends Component<IField, IState> {
  state: IState = { error: null, componentStack: "" };

  static getDerivedStateFromError(error: Error): Partial<IState> {
    return { error };
  }

  componentDidCatch(_error: Error, errorInfo: ErrorInfo) {
    // React still writes the error to the console itself.
    this.setState({ componentStack: errorInfo.componentStack ?? "" });
  }

  componentDidUpdate(previousProps: IField) {
    if (this.state.error && previousProps.resetKey !== this.props.resetKey)
      this.setState({ error: null, componentStack: "" });
  }

  render() {
    if (!this.state.error) return this.props.children;

    return (
      <ErrorScreen
        error={this.state.error}
        componentStack={this.state.componentStack}
      />
    );
  }
}

// A component of its own, because the translator and the router are hooks.
function ErrorScreen({
  error,
  componentStack,
}: {
  error: Error;
  componentStack: string;
}) {
  const { t } = useTranslator();
  const navigate = useNavigate();

  // The message and the first few components it passed through are what a
  // screenshot needs to show. Plain tags like "at div" say nothing, so they go.
  const details = [
    window.location.pathname + window.location.search,
    `${error.name}: ${error.message}`,
    ...componentStack
      .split("\n")
      .map((x) => x.trim())
      .filter((x) => x.length > 0 && !/^at [a-z][a-z0-9]*( |$)/.test(x))
      .slice(0, 8),
  ].join("\n");

  return (
    <div className="flex justify-content-center p-3">
      <Card
        className="w-full"
        style={{ maxWidth: "40rem" }}
      >
        <div className="flex align-items-center gap-2 mb-3">
          <i className="pi pi-exclamation-triangle text-orange-400 text-2xl" />
          <h2 className="m-0">{t("Something went wrong")}</h2>
        </div>

        <p className="mt-0 line-height-3">
          {t(
            "The page stopped because of an error. If it happens again, send us a screenshot of this screen",
          )}
          .
        </p>

        <div className="text-xs uppercase text-color-secondary mb-1">
          {t("Details")}
        </div>
        <pre
          className="m-0 p-3 border-round surface-ground text-sm overflow-auto"
          style={{ whiteSpace: "pre-wrap", wordBreak: "break-word", maxHeight: "16rem" }}
        >
          {details}
        </pre>

        <div className="flex flex-wrap justify-content-end gap-2 mt-3">
          <Button
            label={t("Go back")}
            icon="pi pi-angle-left"
            className="p-button-text"
            onClick={() => navigate(-1)}
          />
          <Button
            label={t("Reload page")}
            icon="pi pi-refresh"
            onClick={() => window.location.reload()}
          />
        </div>
      </Card>
    </div>
  );
}
