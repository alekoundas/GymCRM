import ReactQuill from "react-quill-new";
import { InputTextarea } from "primereact/inputtextarea";
import { Button } from "primereact/button";
import { useState } from "react";

// Define allowed tag names as a TypeScript type
type Tag =
  | "a"
  | "img"
  | "div"
  | "p"
  | "span"
  | "table"
  | "td"
  | "th"
  | "ul"
  | "ol"
  | "li";
interface IField {
  value?: string;
  isEnabled: boolean;
  label?: string;
  onChange?: (inputValue: string) => void;
}

export default function RichTextAreaComponent({
  value,
  isEnabled,
  label,
  onChange,
}: IField) {
  const [textAreaHtmlValue, setTextAreaHtmlValue] = useState(value); // used to escape lazyload firing again after dto update.
  const [isHtmlMode, setIsHtmlMode] = useState(false);

  const handleChange = (inputValue: string): void => {
    setTextAreaHtmlValue(inputValue);
    if (onChange) onChange(inputValue);
  };

  // Quill toolbar configuration
  const modules = {
    toolbar: [
      [{ header: [1, 2, 3, 4, 5, false] }],
      ["bold", "italic", "underline"],
      [{ color: [] }, { background: [] }],
      ["blockquote", "code-block"],
      [{ list: "ordered" }, { list: "bullet" }],
      [{ align: [] }],
      [{ size: ["small", false, "large", "huge"] }],
      ["link", "image"],
      ["clean"],
    ],
  };

  // Fixed formats array (removed "bullet")
  const formats = [
    "header",
    "bold",
    "color",
    "background",
    "italic",
    "underline",
    "blockquote",
    "code-block",
    "list", // Covers both bullet and ordered lists
    "align",
    "size",
    "link",
    "image",
  ];

  const cleanHtml = (html: string): string => {
    const parser = new DOMParser();
    const doc = parser.parseFromString(html, "text/html");

    // Remove unsafe tags
    const unsafeTags = [
      "script",
      "style",
      "iframe",
      "object",
      "embed",
      "form",
      "input",
    ];
    unsafeTags.forEach((tag) => {
      const elements = doc.getElementsByTagName(tag);
      Array.from(elements).forEach((el) => el.remove());
    });

    // Allowed tags and attributes for email HTML
    const allowedTags = [
      "p",
      "div",
      "br",
      "strong",
      "em",
      "u",
      "a",
      "ul",
      "ol",
      "li",
      "img",
      "table",
      "tr",
      "td",
      "th",
      "thead",
      "tbody",
      "h1",
      "h2",
      "h3",
      "h4",
      "h5",
      "h6",
      "blockquote",
      "code",
      "pre",
      "span",
    ];
    const allowedAttributes: { [key in Tag]?: string[] } = {
      a: ["href", "title"],
      img: ["src", "alt", "width", "height"],
      div: ["style"],
      p: ["style"],
      span: ["style"],
      table: ["style", "border", "cellspacing", "cellpadding"],
      td: ["style", "colspan", "rowspan"],
      th: ["style", "colspan", "rowspan"],
      ul: ["style"],
      ol: ["style"],
      li: ["style"],
    };

    // Allowed inline CSS properties
    const allowedStyles = [
      "color",
      "background-color",
      "font-size",
      "font-family",
      "font-weight",
      "font-style",
      "text-align",
      "margin",
      "padding",
      "border",
      "width",
      "height",
    ];

    // Clean and filter HTML
    const cleanDoc = document.createElement("div");
    const walk = (node: Node) => {
      if (node.nodeType === Node.ELEMENT_NODE) {
        const el = node as HTMLElement;
        const tag = el.tagName.toLowerCase();

        // Skip if tag is not allowed
        if (!allowedTags.includes(tag)) {
          el.remove();
          return;
        }

        // Clean attributes with type guard
        if (tag in allowedAttributes) {
          Array.from(el.attributes).forEach((attr) => {
            if (!allowedAttributes[tag as Tag]?.includes(attr.name)) {
              el.removeAttribute(attr.name);
            } else if (attr.name === "style") {
              // Filter style properties
              const styles = attr.value.split(";").reduce((acc, style) => {
                const [property, value] = style.split(":").map((s) => s.trim());
                if (property && allowedStyles.includes(property)) {
                  acc.push(`${property}: ${value}`);
                }
                return acc;
              }, [] as string[]);
              el.setAttribute("style", styles.join("; "));
            }
          });
        } else {
          // Remove all attributes for tags not in allowedAttributes
          Array.from(el.attributes).forEach((attr) =>
            el.removeAttribute(attr.name)
          );
        }

        // Recursively process child nodes
        Array.from(node.childNodes).forEach(walk);
      }

      if (node.parentNode === null) {
        cleanDoc.appendChild(node.cloneNode(true));
      }
    };

    Array.from(doc.body.childNodes).forEach(walk);

    // Ensure at least a minimal valid HTML structure
    const cleanedHtml = cleanDoc.innerHTML.trim();
    return cleanedHtml || "<p></p>";
  };

  return (
    <>
      <div className="flex justify-content-between align-items-center mb-2">
        <label
          htmlFor="input-quill"
          className="block text-900 font-medium"
        >
          {label}
        </label>

        <Button
          label={isHtmlMode ? "Switch to Rich Text" : "Switch to HTML"}
          icon={isHtmlMode ? "pi pi-pencil" : "pi pi-code"}
          onClick={() => setIsHtmlMode(!isHtmlMode)}
          disabled={!isEnabled}
          className="p-button-text p-button-sm"
        />
      </div>
      {isHtmlMode ? (
        <InputTextarea
          id="input-quill"
          name="input-quill"
          value={textAreaHtmlValue}
          onChange={(e) => handleChange(e.target.value)}
          disabled={!isEnabled}
          rows={10}
          className="w-full"
          style={{
            fontFamily: "monospace",
            resize: "vertical",
            minHeight: "150px",
            maxHeight: "500px",
          }}
        />
      ) : (
        <ReactQuill
          theme="snow"
          modules={modules}
          formats={formats}
          value={textAreaHtmlValue}
          onChange={(e) => handleChange(e)}
          readOnly={!isEnabled}
          placeholder="Write something awesome..."
          className="quill-editor"
        />
      )}
    </>
  );
}
