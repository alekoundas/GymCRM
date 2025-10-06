import { Button } from "primereact/button";
import { InputText } from "primereact/inputtext";
import { useState } from "react";
import { useAuth } from "../../contexts/AuthContext";
import { useNavigate } from "react-router-dom";
import { UserLoginRequestDto } from "../../model/entities/user/UserLoginRequestDto";
import { useApiService } from "../../services/ApiService";

export default function LoginPage() {
  const apiService = useApiService();
  const { login } = useAuth();
  const navigate = useNavigate();

  const [userLoginDto, setUserLoginDto] = useState(new UserLoginRequestDto());
  const handleChange = (event: React.ChangeEvent<any>) => {
    const name = event.target.name;
    const value = event.target.value;

    userLoginDto[name] = value;
    setUserLoginDto({ ...userLoginDto });
  };

  const onLogin = () => {
    apiService.login(userLoginDto, login).then((isSuccessful) => {
      if (isSuccessful) {
        navigate("/");
      }
    });
  };
  return (
    <>
      <div className="flex align-items-center justify-content-center">
        <div className="surface-card p-4 shadow-2 border-round w-full lg:w-6">
          <div className="text-center mb-5">
            <img
              src="https://primefaces.org/cdn/primereact/images/logo.png"
              alt="hyper"
              height={50}
              className="mb-3"
            />
            <div className="text-900 text-3xl font-medium mb-3">
              Welcome Back
            </div>
            <span className="text-600 font-medium line-height-3">
              Don't have an account?
            </span>
            <a
              className="font-medium no-underline ml-2 text-blue-500 cursor-pointer"
              onClick={() => navigate("/users/register")}
            >
              Create today!
            </a>
          </div>

          <div>
            <label
              htmlFor="userNameOrEmail"
              className="block text-900 font-medium mb-2"
            >
              Username / Email
            </label>
            <InputText
              id="userNameOrEmail"
              name="userNameOrEmail"
              type="text"
              placeholder="Username / Email address"
              className="w-full mb-3"
              value={userLoginDto.userNameOrEmail}
              onChange={handleChange}
            />

            <label
              htmlFor="password"
              className="block text-900 font-medium mb-2"
            >
              Password
            </label>
            <InputText
              id="password"
              name="password"
              type="password"
              placeholder="Password"
              className="w-full mb-3"
              value={userLoginDto.password}
              onChange={handleChange}
            />

            <div className="flex align-items-center justify-content-between mb-6">
              <div className="flex align-items-center">
                {/* <Checkbox
                  id="rememberme"
                  onChange={(e) => setChecked(e.checked)}
                  checked={checked}
                  className="mr-2"
                /> */}
                {/* <label htmlFor="rememberme">Remember me</label> */}
              </div>
              <a
                className="font-medium no-underline ml-2 text-blue-500 text-right cursor-pointer"
                onClick={() => navigate("/users/forgot-password")}
              >
                Forgot your password?
              </a>
            </div>

            <Button
              label="Sign In"
              className="w-full"
              onClick={onLogin}
            />
          </div>
        </div>
      </div>
    </>
  );
}
