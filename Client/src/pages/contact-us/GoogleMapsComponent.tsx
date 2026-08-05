interface IField {
  address: string;
  zoom?: number;
}

export default function GoogleMapsComponent({ address, zoom = 15 }: IField) {
  const src = `https://maps.google.com/maps?q=${encodeURIComponent(
    address
  )}&z=${zoom}&output=embed`;

  return (
    <iframe
      title="Business location"
      src={src}
      width="100%"
      height="100%"
      style={{ border: 0, display: "block", minHeight: "300px" }}
      loading="lazy"
      referrerPolicy="no-referrer-when-downgrade"
    />
  );
}
