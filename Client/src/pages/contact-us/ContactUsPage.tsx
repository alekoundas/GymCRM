import { Card } from "primereact/card";
import { useTranslator } from "../../services/TranslatorService";
import GoogleMapsComponent from "./GoogleMapsComponent";

export default function ContactUsPage() {
  const { t } = useTranslator();

  const contactInfo = {
    mobile: "+30 6957 02 5139",
    landline: "+30 213 04 88 192",
    address: "Mpizaniou 3, Ilioupoli 163 43",
    email: "rosacorelab@gmail.com",
  };

  const items = [
    {
      icon: "pi pi-mobile",
      label: t("Mobile Phone"),
      value: contactInfo.mobile,
      href: `tel:${contactInfo.mobile.replace(/\s+/g, "")}`,
    },
    {
      icon: "pi pi-phone",
      label: t("Landline"),
      value: contactInfo.landline,
      href: `tel:${contactInfo.landline.replace(/\s+/g, "")}`,
    },
    {
      icon: "pi pi-map-marker",
      label: t("Address"),
      value: t("Mpizaniou 3, Ilioupoli 163 43"),
    },
    {
      icon: "pi pi-envelope",
      label: "Email",
      value: contactInfo.email,
      href: `mailto:${contactInfo.email}`,
    },
  ];

  const header = (
    <div className="flex justify-content-center align-items-center pt-4">
      <h2 className="m-0 text-900">{t("Contact Us")}</h2>
    </div>
  );
  const footer = (
    <div className="flex justify-content-center align-items-center">
      <p className="m-0 text-600 text-center">
        {t("Wed love to hear from you!")}
      </p>
    </div>
  );

  return (
    <Card header={header} footer={footer} className="w-full">
      <div className="grid align-items-stretch">
        <div className="col-12 lg:col-5">
          <div className="flex flex-column gap-3 p-2 md:p-4 h-full justify-content-center">
            {items.map((item) => (
              <div
                key={item.label}
                className="flex align-items-center gap-3 p-3 surface-50 border-round"
              >
                <div
                  className="flex align-items-center justify-content-center border-circle bg-primary text-white flex-shrink-0"
                  style={{ width: "2.75rem", height: "2.75rem" }}
                >
                  <i className={`${item.icon} text-lg`} />
                </div>
                <div className="flex flex-column flex-1 min-w-0">
                  <span className="font-semibold text-900 text-sm">
                    {item.label}
                  </span>
                  {item.href ? (
                    <a
                      href={item.href}
                      className="mt-1 text-600 no-underline hover:text-primary line-height-3"
                      style={{ wordBreak: "break-word" }}
                    >
                      {item.value}
                    </a>
                  ) : (
                    <span
                      className="mt-1 text-600 line-height-3"
                      style={{ wordBreak: "break-word" }}
                    >
                      {item.value}
                    </span>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
        <div className="col-12 lg:col-7">
          <div
            className="border-round overflow-hidden shadow-1"
            style={{ minHeight: "300px" }}
          >
            <GoogleMapsComponent address={contactInfo.address} />
          </div>
        </div>
      </div>
    </Card>
  );
}
