import ThemeService from "../../services/ThemeService";
import { useTranslator } from "../../services/TranslatorService";
import DotGridComponent from "./DotGridComponent";

export default function Home() {
  const { t } = useTranslator();

  const palette = ThemeService.getCurrentThemeColors();
  console.log(palette.primaryColor);
  return (
    <>
      <div
        className="w-full h-full"
        style={{ position: "relative" }}
      >
        <div className="z-0 absolute top-0 left-0 flex align-items-center justify-content-center w-full h-full">
          <DotGridComponent
            dotSize={4}
            gap={14}
            // baseColor={palette.primaryColor}
            baseColor="#60a5fa"
            activeColor="#ff00ff"
            proximity={410}
            shockRadius={200}
            shockStrength={10}
            resistance={1300}
            returnDuration={0.7}
          />
        </div>
        <div className="z-10 absolute top-0 left-0 flex align-items-center justify-content-center w-full h-full">
          <div className="max-h-full overflow-y-auto w-8 text-xs lg:text-lg xl:text-lg font-bold px-4 scrollbar-thin scrollbar-thumb-gray-400 scrollbar-track-gray-200">
            <p className="text-center">
              {t(
                "At ROSA CoreLab, we believe that true strength starts from within us."
              )}
            </p>

            <p className="text-center">
              {t(
                "Our space is a multi-purpose venue dedicated to self-improvement, wellness, and personal development."
              )}
            </p>

            <p className="text-center">
              {t(
                "Here, every person can discover their own rhythm, cultivate their body as well as their mind, find balance, and proceed with confidence to the next step."
              )}
            </p>

            <p className="text-center">
              {t(
                "We offer opportunities for targeted physical strengthening, also through the treasures of martial arts techniques, which develop endurance, discipline, and self-confidence. At the same time, through life coaching and nutritional counseling, we help people find clarity in their goals and adopt a lifestyle that suits them."
              )}
            </p>

            <p className="text-center">
              {t(
                "Our team collaborates with professional nutritionists and mental health experts, because we believe that real change comes when we care for our entire self—body, mind, and soul."
              )}
            </p>

            <p className="text-center">
              {t(
                "At ROSA CoreLab, you dont need any equipment to start. All you need is the desire to improve. We are here to support you on your journey toward a stronger, brighter version of yourself. Because for us, self-improvement is not a goal; it is a way of life."
              )}
            </p>
          </div>
        </div>
      </div>
    </>
  );
}
