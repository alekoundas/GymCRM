import { createContext, useContext, useState, ReactNode } from "react";
import { TokenService } from "../services/TokenService";

// Create the AuthContext
interface AuthContextProps {
  isUserAuthenticated: boolean;
  login: () => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextProps | undefined>(undefined);

// Custom hook to use the AuthContext
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};

// AuthProvider component
export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [isUserAuthenticated, setIsUserAuthenticated] = useState(
    !TokenService.isRefreshTokenExpired()
  );

  const login = () => {
    setIsUserAuthenticated(true); // Set authentication to true
  };

  const logout = () => {
    setIsUserAuthenticated(false); // Set authentication to false
  };

  return (
    <AuthContext.Provider value={{ isUserAuthenticated, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};
