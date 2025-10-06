import { createRoot } from "react-dom/client";
import App from "./App.tsx";
import "primeicons/primeicons.css";
import "primeflex/primeflex.css";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "./contexts/AuthContext.js";
import { PrimeReactProvider } from "primereact/api";
import "./index.css";
import "primeicons/primeicons.css";
import { ThemeProvider } from "./contexts/ThemeContext.tsx";
import { ToastProvider } from "./contexts/ToastContext.tsx";
import { I18nextProvider } from "react-i18next";
import i18n from "./config/i18n.tsx";

createRoot(document.getElementById("root")!).render(
  <>
    {/* <StrictMode> */}
    <div className="h-screen">
      <BrowserRouter>
        <AuthProvider>
          <I18nextProvider i18n={i18n}>
            <PrimeReactProvider
              value={{ ripple: true, locale: "en", unstyled: false }}
            >
              <ToastProvider>
                <ThemeProvider>
                  <App />
                </ThemeProvider>
              </ToastProvider>
            </PrimeReactProvider>
          </I18nextProvider>
        </AuthProvider>
      </BrowserRouter>
    </div>
    {/* </StrictMode> */}
  </>
);
