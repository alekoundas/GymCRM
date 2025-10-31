import { useApiService } from "../../services/ApiService";
import { Card } from "primereact/card";
import { useTranslator } from "../../services/TranslatorService";
import { GoogleMap, useLoadScript, Marker } from "@react-google-maps/api";

interface IField {
  center?: { lat: number; lng: number };
  zoom?: number;
  markers?: Array<{ lat: number; lng: number; title?: string }>;
}

export default function GoogleMapsComponent({
  center = { lat: 37.7749, lng: -122.4194 },
  zoom = 13,
  markers = [],
}: IField) {
  // const { isLoaded, loadError } = useLoadScript({
  //   googleMapsApiKey: import.meta.env.VITE_GOOGLE_MAPS_API_KEY as string,
  //   libraries: ['places'], // Optional: for search/autocomplete
  // });

  // if (loadError) return <div>Error loading maps: {loadError.message}</div>;
  // if (!isLoaded) return <div>Loading map...</div>;

  return (
    <>
      <GoogleMap
        mapContainerStyle={{
          width: "100%",
          height: "400px", // Or full viewport for mobile
        }}
        center={center}
        zoom={zoom}
        options={{
          // Production: Disable unnecessary features for perf
          fullscreenControl: false,
          streetViewControl: false,
          mapTypeControl: false,
        }}
      >
        {markers.map((marker, index) => (
          <Marker
            key={index}
            position={{ lat: marker.lat, lng: marker.lng }}
            title={marker.title}
          />
        ))}
      </GoogleMap>
    </>
  );
}
