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
      height="400"
      style={{ border: 0 }}
      loading="lazy"
      referrerPolicy="no-referrer-when-downgrade"
    />
  );
}
