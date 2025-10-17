import { Knob } from "primereact/knob";
import { Sidebar } from "primereact/sidebar";
import { useTheme } from "../../../contexts/ThemeContext";
import { Button } from "primereact/button";
import ThemeService from "../../../services/ThemeService";
import { Image } from "primereact/image";
import { useState } from "react";
import { useTranslator } from "../../../services/TranslatorService";
import { LocalStorageService } from "../../../services/LocalStorageService";

interface IField {
  isVisible: boolean;
  hideSidebar: () => void;
}
export default function NavSidebar({ isVisible, hideSidebar }: IField) {
  const { currentThemeScale, setTheme, setThemeScale } = useTheme();
  const { t, setLanguage } = useTranslator();
  const [selectedLang, setSelectedLang] = useState<string>(
    LocalStorageService.getLanguage() ?? "en"
  );
  const languages = [
    // Get those languages on top
    {
      code: "el",
      name: "Greek",
      flagUrl: "/flags/4x3/gr.svg",
    },
    {
      code: "en",
      name: "English",
      flagUrl: "/flags/4x3/us.svg",
    },

    // Rest are order by country code
    {
      code: "ar",
      name: "Arabic",
      flagUrl: "/flags/4x3/sa.svg",
    },
    {
      code: "cs",
      name: "Czech",
      flagUrl: "/flags/4x3/cz.svg",
    },
    {
      code: "da",
      name: "Danish",
      flagUrl: "/flags/4x3/dk.svg",
    },
    {
      code: "de",
      name: "German",
      flagUrl: "/flags/4x3/de.svg",
    },
    // {
    //   code: "el",
    //   name: "Greek",
    //   flagUrl: "/flags/4x3/gr.svg",
    // },
    // {
    //   code: "en",
    //   name: "English",
    //   flagUrl: "/flags/4x3/us.svg",
    // },
    {
      code: "es",
      name: "Spanish",
      flagUrl: "/flags/4x3/es.svg",
    },
    {
      code: "fi",
      name: "Finnish",
      flagUrl: "/flags/4x3/fi.svg",
    },
    {
      code: "fr",
      name: "French",
      flagUrl: "/flags/4x3/fr.svg",
    },
    {
      code: "it",
      name: "Italian",
      flagUrl: "/flags/4x3/it.svg",
    },
    {
      code: "ja",
      name: "Japanese",
      flagUrl: "/flags/4x3/jp.svg",
    },
    {
      code: "ko",
      name: "Korean",
      flagUrl: "/flags/4x3/kr.svg",
    },
    {
      code: "nl",
      name: "Dutch",
      flagUrl: "/flags/4x3/nl.svg",
    },
    {
      code: "no",
      name: "Norwegian",
      flagUrl: "/flags/4x3/no.svg",
    },
    {
      code: "pl",
      name: "Polish",
      flagUrl: "/flags/4x3/pl.svg",
    },
    {
      code: "pt",
      name: "Portuguese",
      flagUrl: "/flags/4x3/pt.svg",
    },
    {
      code: "ru",
      name: "Russian",
      flagUrl: "/flags/4x3/ru.svg",
    },
    {
      code: "sq",
      name: "Albanian",
      flagUrl: "/flags/4x3/al.svg",
    },
    {
      code: "sv",
      name: "Swedish",
      flagUrl: "/flags/4x3/se.svg",
    },
    {
      code: "tr",
      name: "Turkish",
      flagUrl: "/flags/4x3/tr.svg",
    },
    {
      code: "zh",
      name: "Chinese",
      flagUrl: "/flags/4x3/cn.svg",
    },
  ];

  return (
    <>
      <Sidebar
        visible={isVisible}
        position="right"
        onHide={() => hideSidebar()}
      >
        <h2>{t("Theme scale")}:</h2>
        <div className="card flex flex-column align-items-center gap-2">
          <Knob
            value={currentThemeScale}
            onChange={(e) => setThemeScale(e.value)}
            max={20}
            size={100}
          />
          <div className="flex gap-2">
            <Button
              icon="pi pi-plus"
              onClick={() => setThemeScale(currentThemeScale + 1)}
              disabled={currentThemeScale === 20}
            />
            <Button
              icon="pi pi-minus"
              onClick={() => setThemeScale(currentThemeScale - 1)}
              disabled={currentThemeScale === 0}
            />
          </div>
        </div>
        <h2>{t("Dark themes")}:</h2>
        <div className="flex flex-wrap ">
          {ThemeService.getDarkThemes().map((row, index) => (
            <div
              key={index}
              className="flex bg-primary m-1 border-round"
            >
              <Button
                onClick={() => setTheme(row.themeName)}
                className="cursor-pointer p-link"
              >
                <Image
                  src={row.themeImage}
                  width="50"
                  alt={row.themeName}
                  onError={(e) => {
                    const img = e.target as HTMLImageElement;
                    if (img.src.endsWith(".svg")) {
                      // Try PNG if SVG fails
                      img.src = row.themeImage.replace(".svg", ".png");
                    }
                  }}
                />
              </Button>
            </div>
          ))}
        </div>
        <h2>{t("Light themes")}:</h2>
        <div className="flex flex-wrap ">
          {ThemeService.getLightThemes().map((row, index) => (
            <div
              key={index}
              className="flex bg-primary m-1 border-round"
            >
              <Button
                onClick={() => setTheme(row.themeName)}
                className="cursor-pointer p-link"
              >
                <Image
                  src={row.themeImage}
                  width="50"
                  alt={row.themeName}
                  onError={(e) => {
                    const img = e.target as HTMLImageElement;
                    if (img.src.endsWith(".svg")) {
                      // Try PNG if SVG fails
                      img.src = row.themeImage.replace(".svg", ".png");
                    }
                  }}
                />
              </Button>
            </div>
          ))}
        </div>

        <h2>{t("Languages")}:</h2>
        <div className="flex flex-wrap gap-2">
          {languages.map((lang) => (
            <Button
              key={lang.code}
              className="p-1 m-0"
              severity={selectedLang === lang.code ? "success" : "secondary"}
              onClick={() => {
                const selectedLanguageCode =
                  selectedLang === lang.code ? "en" : lang.code;

                setLanguage(selectedLanguageCode);
                setSelectedLang(selectedLanguageCode);
              }}
            >
              <Image
                src={lang.flagUrl}
                width="24"
                height="18"
                alt={`${lang.name} flag`}
              />
              <span>{lang.name}</span>
            </Button>
          ))}
        </div>
      </Sidebar>
    </>
  );
}
