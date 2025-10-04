import React, {
  createContext,
  useContext,
  useEffect,
  useState,
  ReactNode,
} from "react";
import { PrimeReactContext } from "primereact/api";
import { LocalStorageService } from "../services/LocalStorageService";
import { PrimeTheme } from "../model/PrimeTheme";

// Define available themes (optional; you can remove if using ThemeService for this)
export interface ThemeOption {
  label: string;
  value: string;
}

// Type for context value
interface ThemeContextType {
  currentTheme: string;
  switchTheme: (newTheme: string) => void;
}

// Default context value (for TypeScript safety)
const defaultThemeContext: ThemeContextType = {
  currentTheme: "bootstrap4-dark-blue", // Default theme
  switchTheme: () => {},
};

// Create the context
export const ThemeContext =
  createContext<ThemeContextType>(defaultThemeContext);

// Custom hook for easy consumption
export const useTheme = () => {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error("useTheme must be used within a ThemeProvider");
  }
  return context;
};

// Provider component
interface ThemeProviderProps {
  children: ReactNode;
}
export const ThemeProvider: React.FC<ThemeProviderProps> = ({ children }) => {
  const { changeTheme } = useContext(PrimeReactContext);
  const [currentTheme, setCurrentTheme] = useState<string>(
    LocalStorageService.getThemeName() || defaultThemeContext.currentTheme
  );

  // Initialize link and set initial theme (runs once on mount)
  useEffect(() => {
    let link = document.getElementById("theme-link") as HTMLLinkElement | null;
    if (!link) {
      link = document.createElement("link");
      link.id = "theme-link";
      link.rel = "stylesheet";
      document.head.appendChild(link);
    }

    // Set href to currentTheme (saved or default)
    link.href = `/themes/${currentTheme}/theme.css`;
  }, []); // Empty deps: Run once, currentTheme is set before this

  // Apply saved theme on mount (if mismatch after init)
  useEffect(() => {
    const savedTheme = LocalStorageService.getThemeName();
    if (changeTheme && savedTheme && savedTheme !== currentTheme) {
      changeTheme(currentTheme, savedTheme, "theme-link");
      setCurrentTheme(savedTheme);
    }
  }, [changeTheme, currentTheme]);

  // Switch theme function (unchanged)
  const switchTheme = (newTheme: string) => {
    if (changeTheme) {
      changeTheme(currentTheme, newTheme, "theme-link", () => {
        const palette = new PrimeTheme(newTheme).getColorPalette();

        document.dispatchEvent(
          new CustomEvent("primeThemeChange", {
            detail: { palette },
          })
        );
      });
      setCurrentTheme(newTheme);
      LocalStorageService.setThemeName(newTheme);
    }
  };

  const value: ThemeContextType = {
    currentTheme,
    switchTheme,
  };

  return (
    <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
  );
};
