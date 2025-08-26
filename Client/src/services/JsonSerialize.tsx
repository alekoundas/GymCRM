export const JsonSerialize = (data: any): string => {
  const formatDate = (date: Date): string => {
    // Convert local date to UTC, preserving the time (midnight from Calendar)
    const year = date.getUTCFullYear();
    const month = String(date.getUTCMonth() + 1).padStart(2, "0");
    const day = String(date.getUTCDate()).padStart(2, "0");
    const hours = String(date.getUTCHours()).padStart(2, "0");
    const minutes = String(date.getUTCMinutes()).padStart(2, "0");
    const seconds = String(date.getUTCSeconds()).padStart(2, "0");
    return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}.000Z`;
  };

  const serialize = (value: any): any => {
    if (value instanceof Date) {
      return formatDate(value);
    }
    if (Array.isArray(value)) {
      return value.map(serialize);
    }
    if (value && typeof value === "object") {
      const result: any = {};
      for (const key in value) {
        result[key] = serialize(value[key]);
      }
      return result;
    }
    return value;
  };

  return JSON.stringify(serialize(data));
};
