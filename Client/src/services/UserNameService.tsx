// One way of naming a member everywhere: "First Last (username)". The username tells
// two people with the same name apart, and is all there is to show when someone has
// not filled their names in. A screen that does not have the username (a trainer
// shown to members, say) gets the name on its own.

export interface UserNameParts {
  firstName?: string | null;
  lastName?: string | null;
  userName?: string | null;
}

// Names are often typed in lower case on sign-up; only the first letter is raised.
const capitalize = (value: string): string =>
  value.charAt(0).toLocaleUpperCase() + value.slice(1);

export const formatUserName = (user?: UserNameParts | null): string => {
  if (!user) return "";

  const name = [user.firstName, user.lastName]
    .map((x) => (x ?? "").trim())
    .filter((x) => x.length > 0)
    .map(capitalize)
    .join(" ");
  const userName = (user.userName ?? "").trim();

  if (name.length === 0) return userName;
  if (userName.length === 0) return name;
  return `${name} (${userName})`;
};

// Two letters for the avatar, or the username's first when there are no names.
export const formatUserInitials = (user?: UserNameParts | null): string => {
  if (!user) return "";

  const initials = [user.firstName, user.lastName]
    .map((x) => (x ?? "").trim().charAt(0))
    .join("");

  return (initials || (user.userName ?? "").trim().charAt(0)).toLocaleUpperCase();
};

// Pictures arrive as plain base64 from the server and as data urls from the uploader.
export const toProfileImageSrc = (
  profileImage?: string | null,
): string | undefined => {
  if (!profileImage) return undefined;
  if (profileImage.startsWith("data:")) return profileImage;
  return `data:image/png;base64,${profileImage}`;
};
