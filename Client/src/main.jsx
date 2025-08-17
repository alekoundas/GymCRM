import { createRoot } from "react-dom/client";
import App from "./App.jsx";
import "primeicons/primeicons.css";
import "primeflex/primeflex.css";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "./contexts/AuthContext.js";

createRoot(document.getElementById("root")).render(
  <>
    {/* <StrictMode> */}
    <BrowserRouter>
      <AuthProvider>
        <App />
      </AuthProvider>
    </BrowserRouter>
    {/* </StrictMode> */}
  </>
);
