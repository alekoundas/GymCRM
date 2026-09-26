import { useEffect, useRef } from "react";
import ReactQuill from "react-quill-new";
import "react-quill-new/dist/quill.bubble.css";

interface IField {
  value: string;
}

// Rich text from the editor, shown as it was written. Through Quill rather than set
// straight into the page: it lays the html out the way the editor did, and drops
// anything the editor itself could not have produced.
export default function RichTextViewComponent({ value }: IField) {
  const quillRef = useRef<ReactQuill>(null);

  // Quill's theme sets its own small font and a padded box. Read inline with the
  // rest of the page, the text should look like the rest of the page.
  useEffect(() => {
    const root = quillRef.current?.getEditor().root;
    if (!root) return;

    root.style.padding = "0";
    const container = root.parentElement;
    if (container) {
      container.style.fontSize = "inherit";
      container.style.fontFamily = "inherit";
    }
  }, [value]);

  if (!value || value === "<p><br></p>") return <></>;

  return (
    <ReactQuill
      ref={quillRef}
      value={value}
      readOnly
      theme="bubble"
      modules={{ toolbar: false }}
    />
  );
}
