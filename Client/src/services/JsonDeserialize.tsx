export const JsonDeserialize = (json: string): any => {
  const dateRegex = /^\d{4}-\d{2}-\d{2}$/; // Matches "YYYY-MM-DD"
  const jsonDeserializeHelper = (key: string, value: any): any => {
    if (typeof value === "string" && dateRegex.test(value)) {
      const [year, month, day] = value.split("-").map(Number);
      return new Date(year, month - 1, day); // Convert to Date
      // Alternatively, return value; to keep as string
    }
    return value;
  };

  return JSON.parse(json, jsonDeserializeHelper);
};
