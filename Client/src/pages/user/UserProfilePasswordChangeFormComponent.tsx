import { useUserStore } from "../../stores/UserStore";
import { DialogChildProps } from "../../components/core/dialog/GenericDialogComponent";
import { Password } from "primereact/password";

interface IField extends DialogChildProps {}

export default function UserProfilePasswordChangeFormComponent({
  formMode,
}: IField) {
  const { userPasswordChangeDto, setUserPasswordChangeDto } = useUserStore();
  return (
    <>
      <div className="field">
        <label
          htmlFor="oldPassword"
          className="block text-900 font-medium mb-2"
        >
          Old Password *
        </label>
        <Password
          id="oldPassword"
          value={userPasswordChangeDto.oldPassword}
          onChange={(e) => {
            setUserPasswordChangeDto({
              ...userPasswordChangeDto,
              oldPassword: e.target.value,
            });
          }}
          placeholder="Enter old password"
          toggleMask
          feedback={false}
        />
      </div>
      <div className="field">
        <label
          htmlFor="newPassword"
          className="block text-900 font-medium mb-2"
        >
          New Password *
        </label>
        <Password
          id="newPassword"
          value={userPasswordChangeDto.newPassword}
          onChange={(e) => {
            setUserPasswordChangeDto({
              ...userPasswordChangeDto,
              newPassword: e.target.value,
            });
          }}
          placeholder="Enter new password"
          toggleMask
          feedback={true}
        />
      </div>
      <div className="field">
        <label
          htmlFor="confirmNewPassword"
          className="block text-900 font-medium mb-2"
        >
          Confirm New Password *
        </label>
        <Password
          id="confirmNewPassword"
          value={userPasswordChangeDto.confirmNewPassword}
          onChange={(e) => {
            setUserPasswordChangeDto({
              ...userPasswordChangeDto,
              confirmNewPassword: e.target.value,
            });
          }}
          placeholder="Confirm new password"
          toggleMask
          feedback={false}
        />
      </div>
    </>
  );
}
