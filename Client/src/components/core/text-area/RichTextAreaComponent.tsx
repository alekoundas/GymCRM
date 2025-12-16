import ReactQuill from "react-quill-new";
import "react-quill-new/dist/quill.snow.css"; // Ensure Quill CSS is imported
import { InputTextarea } from "primereact/inputtextarea";
import { Button } from "primereact/button";
import { useState } from "react";
import { useTranslator } from "../../../services/TranslatorService";

// Define allowed tag names
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
  | "li"
  | "h1"
  | "h2"
  | "h3"
  | "h4"
  | "h5"
  | "h6"
  | "blockquote"
  | "code"
  | "pre"
  | "strong"
  | "em"
  | "u"
  | "br";

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
  const { t } = useTranslator();
  const [textAreaHtmlValue, setTextAreaHtmlValue] = useState(value || "");
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

  const formats = [
    "header",
    "bold",
    "italic",
    "underline",
    "color",
    "background",
    "blockquote",
    "code-block",
    "list",
    "bullet",
    "align",
    "size",
    "link",
    "image",
  ];

  // const cleanHtml = (html: string): string => {
  //   if (!html) return "<p></p>";

  //   const parser = new DOMParser();
  //   const doc = parser.parseFromString(html, "text/html");

  //   // Remove unsafe tags
  //   const unsafeTags = [
  //     "script",
  //     "style",
  //     "iframe",
  //     "object",
  //     "embed",
  //     "form",
  //     "input",
  //   ];
  //   unsafeTags.forEach((tag) => {
  //     const elements = doc.querySelectorAll(tag);
  //     elements.forEach((el) => el.remove());
  //   });

  //   // Allowed tags and attributes
  //   const allowedTags = [
  //     "p",
  //     "div",
  //     "br",
  //     "strong",
  //     "em",
  //     "u",
  //     "a",
  //     "ul",
  //     "ol",
  //     "li",
  //     "img",
  //     "table",
  //     "tr",
  //     "td",
  //     "th",
  //     "thead",
  //     "tbody",
  //     "h1",
  //     "h2",
  //     "h3",
  //     "h4",
  //     "h5",
  //     "h6",
  //     "blockquote",
  //     "code",
  //     "pre",
  //     "span",
  //   ];

  //   const allowedAttributes: { [key in Tag]?: string[] } = {
  //     a: ["href", "title"],
  //     img: ["src", "alt", "width", "height"],
  //     div: ["style"],
  //     p: ["style"],
  //     span: ["style"],
  //     table: ["style", "border", "cellspacing", "cellpadding"],
  //     td: ["style", "colspan", "rowspan"],
  //     th: ["style", "colspan", "rowspan"],
  //     ul: ["style"],
  //     ol: ["style"],
  //     li: ["style"],
  //     h1: ["style"],
  //     h2: ["style"],
  //     h3: ["style"],
  //     h4: ["style"],
  //     h5: ["style"],
  //     h6: ["style"],
  //   };

  //   const allowedStyles = [
  //     "color",
  //     "background-color",
  //     "font-size",
  //     "font-family",
  //     "font-weight",
  //     "font-style",
  //     "text-align",
  //     "margin",
  //     "padding",
  //     "border",
  //     "width",
  //     "height",
  //   ];

  //   // Helper to convert Quill size classes to inline styles
  //   const convertSizeClassToStyle = (el: HTMLElement) => {
  //     const classList = el.className.split(/\s+/);
  //     let sizeValue = "";
  //     classList.forEach((cls) => {
  //       if (cls === "ql-size-small") sizeValue = "0.75em";
  //       else if (cls === "ql-size-large") sizeValue = "1.5em";
  //       else if (cls === "ql-size-huge") sizeValue = "2.5em";
  //       // 'ql-size-normal' is default, no need to set
  //     });

  //     if (sizeValue) {
  //       const currentStyle = el.getAttribute("style") || "";
  //       el.setAttribute(
  //         "style",
  //         `${currentStyle}; font-size: ${sizeValue};`.trim()
  //       );
  //       // Remove Quill size class to keep clean
  //       el.className = classList
  //         .filter((cls) => !cls.startsWith("ql-size-"))
  //         .join(" ");
  //     }
  //   };

  //   const walk = (node: Node) => {
  //     if (node.nodeType === Node.ELEMENT_NODE) {
  //       const el = node as HTMLElement;
  //       const tag = el.tagName.toLowerCase();

  //       if (!allowedTags.includes(tag)) {
  //         el.remove();
  //         return;
  //       }

  //       // Convert size classes to inline font-size
  //       convertSizeClassToStyle(el);

  //       // Clean attributes
  //       if (tag in allowedAttributes) {
  //         Array.from(el.attributes).forEach((attr) => {
  //           const attrName = attr.name.toLowerCase();
  //           if (!allowedAttributes[tag as Tag]?.includes(attrName)) {
  //             el.removeAttribute(attrName);
  //           } else if (attrName === "style") {
  //             // Filter allowed style properties
  //             const styles = attr.value.split(";").reduce((acc, style) => {
  //               const [property, value] = style.split(":").map((s) => s.trim());
  //               if (property && allowedStyles.includes(property)) {
  //                 acc.push(`${property}: ${value}`);
  //               }
  //               return acc;
  //             }, [] as string[]);
  //             el.setAttribute("style", styles.join("; "));
  //           }
  //         });
  //       } else {
  //         // Remove all attributes for tags without allowed list
  //         Array.from(el.attributes).forEach((attr) =>
  //           el.removeAttribute(attr.name)
  //         );
  //       }

  //       // Remove any remaining Quill classes (e.g., ql-align-*)
  //       el.className = el.className
  //         .split(/\s+/)
  //         .filter((cls) => !cls.startsWith("ql-"))
  //         .join(" ")
  //         .trim();

  //       // Recurse children
  //       Array.from(node.childNodes).forEach(walk);
  //     }
  //   };

  //   Array.from(doc.body.childNodes).forEach(walk);

  //   // Build cleaned HTML
  //   const cleanDoc = document.createElement("div");
  //   Array.from(doc.body.childNodes).forEach((node) => {
  //     cleanDoc.appendChild(node.cloneNode(true));
  //   });

  //   const cleanedHtml = cleanDoc.innerHTML.trim();
  //   return cleanedHtml || "<p></p>";
  // };

  return (
    <>
      <style>
        {`
          .quill-editor .ql-editor {
            font-size: 18px !important; /* Larger base font size in editor */
            line-height: 1.6;
          }
          .quill-editor .ql-editor h1 { font-size: 2.5em; }
          .quill-editor .ql-editor h2 { font-size: 2em; }
          .quill-editor .ql-editor h3 { font-size: 1.75em; }
          .quill-editor .ql-editor h4 { font-size: 1.5em; }
          .quill-editor .ql-editor h5 { font-size: 1.25em; }
          .quill-editor .ql-editor h6 { font-size: 1em; }
          .quill-editor .ql-editor ul, 
          .quill-editor .ql-editor ol { padding-left: 1.5em; }
        `}
      </style>

      <div className="flex justify-content-between align-items-center mb-2">
        <label
          htmlFor="input-quill"
          className="block text-900 font-medium"
        >
          {label}
        </label>

        <Button
          label={isHtmlMode ? t("Switch to Rich Text") : t("Switch to HTML")}
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
          onChange={handleChange}
          readOnly={!isEnabled}
          placeholder={t("Write something awesome!")}
          className="quill-editor"
        />
      )}
    </>
  );
}
