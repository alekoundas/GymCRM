import { Tag } from "primereact/tag";
import { categoryTintStyle } from "../../../services/CategoryColorService";

interface ITag {
  name: string;
  color: string;
  className?: string;
}

// A session's category, as a label tinted in its colour.
export default function CategoryTagComponent({ name, color, className }: ITag) {
  return (
    <Tag
      value={name}
      rounded
      className={`font-medium ${className ?? ""}`}
      style={categoryTintStyle(color)}
    />
  );
}

interface IDot {
  color: string;
  size?: string;
}

// The colour alone, beside a category's name in filters and tabs. The ring keeps it
// visible on a filled, selected button too.
export function CategoryDotComponent({ color, size = "0.625rem" }: IDot) {
  return (
    <span
      className="border-circle flex-none inline-block"
      style={{
        width: size,
        height: size,
        background: color,
        boxShadow: "0 0 0 1px rgba(255, 255, 255, 0.7)",
      }}
    />
  );
}
