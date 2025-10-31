import { useApiService } from "../../services/ApiService";
import { Card } from "primereact/card";
import { useTranslator } from "../../services/TranslatorService";
import GoogleMapsComponent from "./GoogleMapsComponent";

export default function ContactUsPage() {
  const { t } = useTranslator();
  const apiService = useApiService();

  const contactInfo = {
    mobile: "+30 6957 02 5139",
    landline: "+30 213 04 88 192",
    address: "Mpizaniou 3, Ilioupoli 163 43",
    email: "rosacorelab@gmail.com",
  };

  const header = (
    <div className="flex justify-content-center align-items-center">
      <p className="m-0 text-500">
        <h2 className="m-0">{t("Contact Us")}</h2>
      </p>
    </div>
  );
  const footer = (
    <div className="flex justify-content-center align-items-center">
      <p className="m-0 text-500">{t("Wed love to hear from you!")}</p>
    </div>
  );

  return (
    <>
      <Card
        header={header}
        footer={footer}
        className="w-full"
      >
        <div className="grid ">
          <div className=" col-12  lg:col-6 xl:col-6">
            <div className="p-4">
              <div className="field mb-3">
                <label className="font-semibold text-900">
                  {t("Mobile Phone")}
                </label>
                <p className="mt-1 text-600">{contactInfo.mobile}</p>
              </div>

              <div className="field mb-3">
                <label className="font-semibold text-900">
                  {t("Landline")}
                </label>
                <p className="mt-1 text-600">{contactInfo.landline}</p>
              </div>

              <div className="field mb-3">
                <label className="font-semibold text-900">{t("Address")}</label>
                <p className="mt-1 text-600">{contactInfo.address}</p>
              </div>

              <div className="field">
                <label className="font-semibold text-900">Email</label>
                <p className="mt-1 text-600">{contactInfo.email}</p>
              </div>
            </div>
          </div>
          <div className=" col-12  lg:col-6 xl:col-6">
            {/* <GoogleMapsComponent /> */}
          </div>
        </div>
      </Card>
    </>
  );
}
