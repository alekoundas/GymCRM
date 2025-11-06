import { Button } from "primereact/button";
import { useApiService } from "../../services/ApiService";
import { useEffect, useState } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import { Card } from "primereact/card";

export default function GooglePage() {
  const apiService = useApiService();
  const params = useParams();
  const [searchParams] = useSearchParams();

  const [tokenDateExpireDate, setTokenDateExpireDate] = useState<string>();

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
          `auth/googlecallback?code=${encodedCode}&state=${encodedState}`
        )
        .then((response) => {});
    }

    // Get expiration date
    apiService.getGoogleTokenExpireDate().then((result) => {
      if (result) {
        setTokenDateExpireDate(result);
      }
    });
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

  return (
    <>
      <Card>
        <div className="w-full">
          <div className="flex justify-content-between align-items-center">
            <div></div>
            <div>
              <h2 className="m-0">
                Expires on:{" "}
                {new Date(tokenDateExpireDate).getDate() +
                  "/" +
                  (new Date(tokenDateExpireDate).getMonth() + 1) +
                  "/" +
                  new Date(tokenDateExpireDate).getFullYear() +
                  " " +
                  new Date(tokenDateExpireDate).getHours() +
                  ":" +
                  new Date(tokenDateExpireDate).getMinutes()}
              </h2>
            </div>
            <div></div>
          </div>
        </div>

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
