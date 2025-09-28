import { createRoot } from "react-dom/client";
import App from "./App.tsx";
import "primeicons/primeicons.css";
import "primeflex/primeflex.css";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "./contexts/AuthContext.js";
import { PrimeReactProvider } from "primereact/api";

createRoot(document.getElementById("root")).render(
  <>
    {/* <StrictMode> */}
    <div className="h-screen">
      <BrowserRouter>
        <AuthProvider>
          <PrimeReactProvider value={{ ripple: true, locale: "en" }}>
            <App />
          </PrimeReactProvider>
        </AuthProvider>
      </BrowserRouter>
    </div>
    {/* </StrictMode> */}
  </>
);
