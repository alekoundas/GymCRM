import { Button } from "primereact/button";
import { useApiService } from "../../services/ApiService";
import { useEffect, useState } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import { Card } from "primereact/card";
import { Message } from "primereact/message";
import { TokenService } from "../../services/TokenService";
import { useTranslator } from "../../services/TranslatorService";

export default function GooglePage() {
  const { t } = useTranslator();
  const apiService = useApiService();
  const params = useParams();
  const [searchParams] = useSearchParams();

  useEffect(() => {
    const state: string = searchParams.get("state") ?? "";
    const code: string = searchParams.get("code") ?? "";
    const scope: string = searchParams.get("scope") ?? "";
    console.log("state:" + state);
    console.log("code:" + code);
    console.log("scope:" + scope);

    const encodedCode = encodeURIComponent(code);
    const encodedState = encodeURIComponent(state);

    if (state?.length > 0 && code?.length > 0) {
      apiService
        .getGoogle<string>(
          `auth/googlecallback?code=${encodedCode}&state=${encodedState}`,
        )
        .then((response) => {});
    }
  }, []);

  const [loading, setLoading] = useState(false);

  const handleAuth = async () => {
    setLoading(true);
    try {
      // Fetch the auth URL from API (now CORS-enabled)
      await apiService.getGoogle<string>("auth/google").then((response) => {
        if (!response) throw new Error("Failed to get auth URL");

        // Store state for CSRF (optional, in sessionStorage)
        //   sessionStorage.setItem("oauth_state", state);

        // Redirect browser to Google
        window.location.href = response;
      });
    } catch (error) {
      console.error("Auth setup failed:", error);
      alert("Error starting auth. Check console.");
    } finally {
      setLoading(false);
    }
  };

  // One-off, to be pressed once after this release and then taken out again. It
  // grants every member exactly what they have already attended, so everybody who
  // was training before subscriptions existed starts from nought instead of deep in
  // the red. Pressing it twice changes nothing: the server skips anybody who
  // already holds credits.
  const [seeding, setSeeding] = useState(false);
  const [seeded, setSeeded] = useState<number | null>(null);

  const handleSeedBalances = async () => {
    setSeeding(true);
    try {
      const response = await apiService.seedInitialSubscriptionBalances();
      if (response !== null) setSeeded(response);
    } finally {
      setSeeding(false);
    }
  };

  return (
    <>
      <Card>
        <div className="w-full">
          <div className="flex justify-content-between align-items-center">
            <div></div>
            <div>
              <Button
                label="Google login"
                onClick={handleAuth}
              ></Button>
            </div>
            <div></div>
          </div>
        </div>
      </Card>
    </>
  );
}
