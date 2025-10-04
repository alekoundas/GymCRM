import React from "react";

interface DividerComponentProps {
  children?: React.ReactNode; // The centered element (e.g., badge)
}

export const DividerComponent: React.FC<DividerComponentProps> = ({
  children,
}) => {
  return (
    <div className="flex align-items-center justify-content-center">
      <div
        className="flex-auto p-0 m-2"
        style={{ height: "1px", backgroundColor: "#ccc" }}
      />

      {children}

      <div
        className="flex-auto p-0 m-2"
        style={{ height: "1px", backgroundColor: "#ccc" }}
      />
    </div>
  );
};
