# Backend tests

The libraries and versions of StepinFlow's backend tests, on .NET 8 like the app. They pin what the
code does today: a test that reads oddly may be pinning a bug, and its comment says so. The app was
changed for one thing they found, the security holes below.

3,897 tests, half a minute. Run from this folder, or from Visual Studio's Test Explorer (`GymCRM.sln`,
folder Tests):

```powershell
dotnet test --solution Tests.slnx
```

Coverage, with a page to read it on (`coveragereport/index.html`):

```powershell
./coverage.ps1
```

## How they work

- **`Shared/Process`**: every test process runs in UTC (as the server does), in English and with no network.
- **`Shared/Database`**: a real SQLite database in memory per test, built by the real migrations. Never `API/sqlite.db`.
- **`API.Tests/Host`**: starts the real `Program.cs` on a free local port. Mails are kept, Google is a stub.
- **`*.approved.txt`**: text a person approved. When it changes the test writes `.received.txt` beside it; if the
  change is meant, replace the approved file with it.

| Project | What it holds |
| --- | --- |
| `Core.Tests` | Every dto's rules, the helpers, the translation files |
| `DataAccess.Tests` | Schema, migrations, delete rules, every column, the rows made at start |
| `Business.Tests` | Booking rules (583,200 combinations, in C# and in SQL), calendar, mails, the repository |
| `API.Tests` | Every endpoint through real requests, mapping, json, startup |
| `Architecture.Tests` | Which project may use which, and what every controller and table has in common |

Four approved lists catch what no single test looks at: every address with who it is open to
(`Contracts/Endpoints`), the json of every request and answer (`Contracts/Requests`, `Contracts/Answers`),
every mapping (`AutoMapper/Maps`) and every column (`DataAccess.Tests/Configurations/Model`).

## Not covered

6,036 of 6,038 lines and 2,123 of 2,154 branches. The two lines:

- `Translator.cs:96` needs a file in `\src\Core\Translations` at the root of the drive.
- `UsersController.cs:329` cannot be reached: line 288 throws on the same filter first.

The branches are the other halves of checks for something that cannot be missing there.

## Do they notice

The code was broken on purpose 1,865 times in a copy, one small change at a time: a comparison turned round, a
number one off, a line or an attribute taken away. 1,711 of them failed a test. The other 154 were read one by
one: some twenty were gaps and have a test now, the rest change nothing anybody could see (a default that is set
twice, a field nobody reads). The lines the security fixes changed were broken 175 times more: four gaps, which have
a test now, and one that changes nothing.

## Security: found on the way, and closed

| It was | It is |
| --- | --- |
| A member could give their own role every permission | Adding, editing and deleting a role takes `Roles_Add`, `Roles_Edit`, `Roles_Delete` |
| Any member could mail every other | `Mails/Send` takes `Mails_Add` |
| Anybody's phone numbers could be read and changed | Their own, or `Users_View` to read and `Users_Edit` to change |
| Any member could call a session off | It takes `TrainGroups_Edit` |
| Anybody's skipped dates could be listed, read and moved | Their own, or staff; nobody moves one |
| `Subscriptions/{id}` showed anybody's | Their own, or `SubscriptionsAdmin_View` |
| The plans grid sent every member's email, address and medical history along, inside their status | A status goes out, and is written, without its members |
| `PasswordChange` changed the account the form named | The caller's own |
| A link to choose a new password lived for a week | 24 hours, as its mail says. Refresh tokens have options of their own (`RefreshTokenProvider`) |
| A refresh token that was replaced went on working | The latest two are good; an older one ends the session (`RefreshTokenService`). Every sign-in is a session, and a row in `AspNetUserTokens` |
| A deleted `Admin` came back with the password in the source | The three accounts are made where there is nobody at all |
| The Gmail client secret and refresh token were in the log and on the console | Neither is written out |
| The Google callback needed no sign-in and its state was fixed | It takes `Roles_Edit`, and its state is signed, for one person and ten minutes |
| MailKit 4.13.0 and MimeKit 4.13.0, one advisory each | 4.16.0 |

Left as it is:

- AutoMapper 14.0.0 has three advisories, and NuGet says so at every build: the versions without them need a licence.
  All three are a map that leads back to itself. There is none, and a test holds that there is none.
- Of the packages that come with others, `dotnet list package --vulnerable --include-transitive` names SQLite's own
  library, WebUtilities, and System.Security.Cryptography `Xml` and `Pkcs`.

## Bugs: found on the way, pinned as they are

- In Danish and in Finnish every date is sent as `18.30.15` and no page can read it.
- A role can be neither read by its id nor deleted: both answer 500.
- A weekday of a group that also names a day of the month makes the booking page answer 500, for everybody, on that date.
- A calendar entry's id is written with the number format of the request's language, so a booking made in
  Greek and cancelled in English stays in the member's calendar.
- An admin's change to a member that is refused (a name that is taken) has already changed their role.
- Assigning a role that does not exist takes the old one away first.
- An edit through the generic controller writes the row the body names, not the one in the address, and
  empties what the form leaves out: who created it and when, and of an attendance which session it was.
- Registering drops the address and the medical history, and with no phone number it complains about train groups.
- Adding a group with the same date twice says "duplicate participant", and a duplicate participant is not noticed.
- Adding a weekday to a group with a fixed day on it is not checked. A day of the month may be 40.
- The same date can be skipped twice in one request, and the same member added or marked present twice.
- Booking a single session together with a day of the month that falls on the same date books it twice, past
  the group's size.
- The mail about the last lesson does not name a session that was written by hand, and is in the trainer's language.
- A date that is `null` answers 500 instead of a bad request.
- A grid's second sort goes the way of the first, and sorting by status beside another column counts for nothing.
- `GenericRepository.ToList` takes the tracking flag the opposite way to `ToListAsync`.
- Off UTC every time moves by the machine's zone (session starts, calendar, json). The server is in UTC, a laptop is not.
- The address limit is 100 characters; its message says 200. A week out of range is said to be "required".

Never used: the four resolvers in `API/AutoMapper`, `JsonDateOnlyConverter`, `JsonTimeSpanConverter`,
`DayOfWeekEnum`, and the answer set up in `InvalidModelStateResponseFactory` (which is empty).
