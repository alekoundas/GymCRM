import { Button } from "primereact/button";
import { InputText } from "primereact/inputtext";
import { useState } from "react";
import { useAuth } from "../../contexts/AuthContext";
import { useNavigate } from "react-router-dom";
import { UserRegisterDto } from "../../model/entities/user/UserRegisterDto";
import { useApiService } from "../../services/ApiService";
import { useTranslator } from "../../services/TranslatorService";

export default function RegisterPage() {
  const { t } = useTranslator();
  const apiService = useApiService();
  const { login } = useAuth();
  const navigate = useNavigate();

  const [userRegisterDto, setUserRegisterDto] = useState(new UserRegisterDto());
  const handleChange = (event: React.ChangeEvent<any>) => {
    const name = event.target.name;
    const value = event.target.value;

    userRegisterDto[name] = value;
    setUserRegisterDto({ ...userRegisterDto });
  };

  const onSave = () => {
    // assing uuser id for required user id
    userRegisterDto.phoneNumbers.forEach(
      (x) => (x.userId = "00000000-0000-0000-0000-000000000000")
    );

    apiService.register(userRegisterDto, login).then((isSuccessful) => {
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
            <div className="text-900 text-3xl font-medium mb-3">Join us</div>
          </div>

          <div>
            <label
              htmlFor="userName"
              className="block text-900 font-medium mb-2"
            >
              {t("User Name")}
            </label>
            <InputText
              id="userName"
              name="userName"
              type="text"
              placeholder={t("User Name")}
              className="w-full mb-3"
              value={userRegisterDto.userName}
              onChange={handleChange}
            />

            <div className="grid ">
              <div className=" col-12  lg:col-6 xl:col-6">
                <label
                  htmlFor="firstName"
                  className="block text-900 font-medium mb-2"
                >
                  {t("First Name")}
                </label>
                <InputText
                  id="firstName"
                  name="firstName"
                  type="text"
                  placeholder={t("First Name")}
                  className="w-full mb-3"
                  value={userRegisterDto.firstName}
                  onChange={handleChange}
                />
              </div>
              <div className="col-12  lg:col-6 xl:col-6">
                <label
                  htmlFor="lastName"
                  className="block text-900 font-medium mb-2"
                >
                  {t("Last Name")}
                </label>
                <InputText
                  id="lastName"
                  name="lastName"
                  type="text"
                  placeholder={t("Last Name")}
                  className="w-full mb-3"
                  value={userRegisterDto.lastName}
                  onChange={handleChange}
                />
              </div>
            </div>

            <label
              htmlFor="address"
              className="block text-900 font-medium mb-2"
            >
              {t("Address")}
            </label>
            <InputText
              id="address"
              name="address"
              type="text"
              placeholder={t("Address")}
              className="w-full mb-3"
              value={userRegisterDto.address}
              onChange={handleChange}
            />

            <label
              htmlFor="phoneNumber"
              className="block text-900 font-medium mb-2"
            >
              {t("Phone Number")}
            </label>
            <InputText
              id="phoneNumber"
              onChange={(e) =>
                (userRegisterDto.phoneNumbers = [
                  { id: 0, number: e.target.value, isPrimary: true },
                ])
              }
              type="text"
              placeholder={t("Phone Number")}
              className="w-full mb-3"
            />

            <label
              htmlFor="email"
              className="block text-900 font-medium mb-2"
            >
              Email
            </label>
            <InputText
              id="email"
              name="email"
              type="text"
              placeholder="Email address"
              className="w-full mb-3"
              value={userRegisterDto.email}
              onChange={handleChange}
            />

            <label
              htmlFor="password"
              className="block text-900 font-medium mb-2"
            >
              {t("Password")}
            </label>
            <InputText
              id="password"
              name="password"
              type="password"
              placeholder={t("Password")}
              className="w-full mb-3"
              value={userRegisterDto.password}
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
            </div>

            <Button
              label={t("Register")}
              className="w-full"
              onClick={onSave}
            />
          </div>
        </div>
      </div>
    </>
  );
}
