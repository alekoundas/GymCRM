import { Avatar } from "primereact/avatar";
import { Tag } from "primereact/tag";
import {
  UserNameParts,
  formatUserInitials,
  formatUserName,
  toProfileImageSrc,
} from "../../../services/UserNameService";

export interface UserChipUser extends UserNameParts {
  profileImage?: string | null;
  // The status colour comes either way: on a user, or flat on a lookup option.
  userStatus?: { color?: string | null } | null;
  userColor?: string | null;
}

interface IField {
  user?: UserChipUser | null;
  // The name on the member's status colour, as the plans list and the member
  // dropdowns have always shown it. Elsewhere it is plain text.
  isStatusColorShown?: boolean;
  className?: string;
}

// The picture (or initials) and "First Last (username)". Every member column and
// member dropdown goes through this, so they all read the same.
export default function UserChipComponent({
  user,
  isStatusColorShown,
  className,
}: IField) {
  if (!user) return <></>;

  const name = formatUserName(user);
  const imageSrc = toProfileImageSrc(user.profileImage);
  const color = user.userStatus?.color ?? user.userColor;

  return (
    <div className={`flex align-items-center gap-2 min-w-0 ${className ?? ""}`}>
      <Avatar
        image={imageSrc}
        label={imageSrc ? undefined : formatUserInitials(user)}
        shape="circle"
        className="flex-none"
      />
      {isStatusColorShown && color ? (
        <Tag
          className="opacity-100"
          style={{ backgroundColor: "#" + color }}
          value={name}
        />
      ) : (
        <span>{name}</span>
      )}
    </div>
  );
}
