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

// Type for context value
interface ThemeContextType {
  currentTheme: string;
  currentThemeScale: number;

  setTheme: (newTheme: string) => void;
  setThemeScale: (themeScale: number) => void;
}

const defaultThemeContext: ThemeContextType = {
  currentTheme: "bootstrap4-dark-blue",
  currentThemeScale: 14,
  setTheme: () => {},
  setThemeScale: () => {},
};

// Create the context
export const ThemeContext =
  createContext<ThemeContextType>(defaultThemeContext);

// Custom hook.
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
  const [currentThemeScale, setCurrentThemeScale] = useState<number>(
    LocalStorageService.getThemeScale() ?? defaultThemeContext.currentThemeScale
  );

  // Initialize (Create Link element and assign theme).
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

    // Set default theme scale.
    document.documentElement.style.fontSize = `${currentThemeScale + 5}px`;

    LocalStorageService.setThemeName(currentTheme);
    LocalStorageService.setThemeScale(currentThemeScale);
  }, []);

  const value: ThemeContextType = {
    currentTheme,
    currentThemeScale,
    setTheme: (newTheme: string) => {
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
    },
    setThemeScale: (themeScale: number) => {
      document.documentElement.style.fontSize = `${themeScale + 5}px`;
      setCurrentThemeScale(themeScale);
      LocalStorageService.setThemeScale(themeScale);
    },
  };

  return (
    <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
  );
};
