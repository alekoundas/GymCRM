# V3 changes

Agreed with the owner in November 2026. Items are built one at a time, in the order below unless you pick another. Tick an item's box when it's done.

| # | Change | Price | Done |
|---|---|---|---|
| 6 | "Ραντεβού" garbled by browser translation | free | [x] |
| 1 | One click opens an exercise for editing | free | [x] |
| 7 | Edit rules while a member is training | 20 | [x] |
| 10 | Admin starts and stops a member's workout | free | [x] |
| 3 | "First Last (username)" in every grid and dropdown, search by any of them | free | [x] |
| 2 | Category colours, categories on the booking calendar | 30 | [x] |
| 5a | Deload week | 40 | [ ] |
| 5b | Sound at 1 hour, keep screen on | 20 | [ ] |
| 9 | Dialog after Stop: tiredness 1-20, then "any problem?" | 40 | [ ] |
| 4 | Admin statistics | 40 | [ ] |
| 12 | Email fixes and a review of every app email | free | [ ] |
| 11 | Installable site + push notifications | 150 | waiting for the owner |

Item 8 was merged into 3. Item 11 is not built unless the owner confirms it.

## How every item is built

- Migrations are generated with the EF CLI and never edited by hand. Only items 2, 5a and 9 need one, one migration each.
- Testing runs on port 5099 against a new, empty database with made-up rows. Never a copy of `API/sqlite.db`, and never ports 8080 or 5173.
- UI uses PrimeReact components and classes, or inline styles.
- Nothing is committed until you say so. Each item is its own commit.
- Leave `API/sqlite.db` out of commits. It's tracked in git and may hold real data; the live database is `/root/app/sqlite.db`, a different file.

---

## 6. "Ραντεβού" garbled by browser translation (free)

The word already uses a Greek Ρ (checked byte by byte in `el.json`). The real cause is `<html lang="en">` in `Client/index.html`: Chrome thinks the Greek page is English and "translates" it.

- Set `document.documentElement.lang` from the language the member picked, on load and whenever it changes.
- Fixes every word on the site, not only this one.

**Done.** `Client/src/config/i18n.tsx` sets the page language on every language change; `Client/index.html` now starts as `lang="el"` (Greek is the default). Checked in a browser: first visit `el`, switching to German `de`, reload with English `en`, back to Greek `el`. Also fixed the English menu label "Apointment" → "Appointment" in `en.json` (the translation key stays as it was).

## 1. One click opens an exercise for editing (free)

- Tapping an exercise row in a workout plan opens the form in Edit mode, or View mode if the person can't edit.
- The ⋮ menu stays.
- File: `ExerciseListItemComponent.tsx`.

**Done.** Tapping an exercise opens Edit when the person may edit and the page isn't opened for viewing; otherwise the read-only form. The ⋮ button, the menu and the "Watch video" link stay outside the tap area, so they do only what they did before. Also works with the keyboard (Tab, then Enter). Checked on a test database as a member (edit page: only Weight editable, save reached the server; view page: read-only) and as the admin (edit page: every field; view page: read-only; ⋮ → Delete opened only its confirmation).

## 7. Edit rules while a member is training (20)

Rules are read only at Start, and each recording keeps its own week number, so the lock protected almost nothing.

- Server (`WorkoutPlanRulesController.cs`): remove the `IsRuleLockedAsync` check from update; keep it on delete.
- Client: replace the "locked" state with a note: changes apply from each member's next Start.
- If a week a member is on gets removed, the existing code already sends them back to week 1.

**Done.** Saving a rule is no longer blocked while someone trains; deleting still is, with the message "Ο κανόνας δεν μπορεί να διαγραφεί όσο ένα μέλος προπονείται με αυτόν." The Edit form now shows an info note in place of the lock ("Ένα μέλος προπονείται αυτή τη στιγμή με αυτόν τον κανόνα" and that changes apply from each member's next start), and the grid tag reads "Προπόνηση σε εξέλιξη" instead of "Κλειδωμένος". Checked on a test database with a member training on week 3: the admin changed messages and limits, removed week 3, and saved from the form; delete was refused; the running workout carried on and stopped on week 3; the member's next start went back to week 1; once stopped, the tag and note disappeared and delete worked.

**Also fixed in item 7: "Ημέρες απουσίας πριν ζητηθεί η εβδομάδα" was never saved.** The add form didn't send it and the server ignored it on edit, so every rule used 12 days whatever the trainer typed. Now:

- It saves on Add and on Edit. The server accepts 1-365 and refuses anything else.
- The field is required. A new rule starts at 12; a rule saved before this fix (no number stored) shows 12, which is what it was using anyway. No migration.
- The hint under it reads "Η προεπιλογή είναι 12 ημέρες". Emptying the box turns it red with "Συμπλήρωσε αριθμό ημερών" and switches Save off until a number is back.

Checked on a test database: 20 on create and 30 on edit were stored; 0, -5, 366 and 400 were refused. With the member's last workout 5 days ago, 3 days made the next Start ask for the week and 10 days carried on with the same week. In the browser: Add starts at 12 and saved 15; an older rule opened at 12 and saved 25; View shows the number read-only; Save goes off while the box is empty and comes back after Cancel.

## 10. Admin starts and stops a member's workout (free)

- Server (`WorkoutPlanRecordingService.cs`): `StartAsync` and `StopAsync` currently allow only the plan's owner. Also allow callers with the recordings admin-view permission.
- The recording stays under the member; `CreatedBy_Id` records the admin who started it.
- Client: Start and Stop on the admin's view of a member's plan. The member sees it running on their phone.
- No after-workout dialog (item 9) when an admin presses Stop.

**Done.** Anyone with both "WorkoutPlanRecordings_Add" and "WorkoutPlanRecordingsAdmin_View" can press Start and Τέλος on a member's plan in the admin pages. Without both, the old "Μόνο το μέλος ξεκινά και σταματά το χρονόμετρο" note stays. The admin's Start dialog opens with "Η προπόνηση καταγράφεται για το μέλος: <name>". The recording is filed under the member, with the admin in `CreatedBy_Id`.

Two devices can now act on one plan, the member's phone and the admin's desk, so:

- Start asks the server again first. If the other device already started, the bar switches to the running timer instead of opening the dialog.
- A Start or Stop that fails (for example, the other device already stopped it) shows the server's message and refreshes the bar and the plan.
- Item 9 still has to skip its dialog when the admin presses Stop. Nothing to do here until item 9 is built.

Also in item 10: the Start dialog's data (the start context) is only given to the plan's member or a recordings admin. Before, any logged-in member could read it for any plan number. Someone else's plan now answers "not found". On the admin pages the timer bar is left out for staff without the recordings admin permission, instead of showing them an error.

**Also in item 10: workout plans locked down (security fix).** Found while testing. Any logged-in member could read any plan by its number, including the owner's email, address and medical history, and could rename another member's plan or change its weights. Now:

| Endpoint | Before | Now |
|---|---|---|
| `GET WorkoutPlans/{id}` | anyone logged in | the plan's member or a plans admin; anyone else gets "not found" |
| Plan owner in every plan reply (page and grid) | the member's whole record, medical history included | name, username, photo and status colour only (`WorkoutPlanOwnerDto`) |
| `PUT WorkoutPlans/{id}` (Edit plan) | any member | plans admins only (members never had the button) |
| `PUT Exercises/{id}` | any member, any exercise, every field | the plan's member changes the weight only, as the form already allowed; plans admins change everything |
| `GET Exercises/{id}`, `GET ExerciseHistories/{id}`, `GET WorkoutPlanRecordings/{id}` | anyone logged in | the plan's member or an admin |
| `PUT ExerciseHistories/{id}` | any member could rewrite history | plans admins only (the app never edits history) |
| `POST WorkoutPlanRecordings` (the generic create) | open to members | refused; Start is the only way to create a recording |

Saving a plan no longer writes anything to the member's record. `PUT Exercises` used to save whichever exercise id was in the request body after checking the one in the URL; for members it now saves the checked exercise only.

Checked on a test database with made-up personal data: 37 checks covering every row above (other members refused, own data and admin access still working, the member's record unchanged after an admin save). In the browser: the admin plans list still shows names and status colours; the admin's Edit plan saves; the member changed a weight by tapping the exercise and it stayed after reload; opening another member's plan by URL shows "Δεν βρέθηκαν δεδομένα."

Checked on a test database: the admin started, stopped, and stopped a workout the member started; the member stopped one the admin started; the member saw the admin's workout running. A member, and a trainer without the recordings permissions, were refused on someone else's plan. In the browser, a member page left open showed Start; after the admin started, pressing Start showed the running timer (no dialog); after the admin stopped, pressing Τέλος showed the error and went back to Start, with the plan header updated. The member's own Start and Τέλος work as before, and their dialog has no admin line.

## 3. "First Last (username)" everywhere (free)

Example: `Αλεξ Ψυχογυιος (apsychogyios)`. Username only when both names are empty.

- One shared way to show a member, used by every grid's user column (`chipTemplate` / `memberTemplate`), every user dropdown and autocomplete, and the grid filter dropdown (`DataTableFilterIdComponent.tsx`, which shows only the username today).
- Search: every typed word must match the first name, last name or username, ignoring accents and case (the existing `normalize()` database function). So "Γιάννης Παπ" finds Γιάννης Παπαδόπουλος.
- The Users admin grid keeps its separate first name / last name / username columns.
- Security: limit the Users `Lookup`, `AutoComplete` and `GetDataTable` endpoints to admins. Today any logged-in member can read the whole member list. Check first which member screens call them.

**Done.**

*Display.* One shared piece (`UserChipComponent` with `formatUserName` in `services/UserNameService.tsx`) shows a member as picture or initials plus "First Last (username)". The first letter of each name is raised; a member with no names shows the username, and before this such a row crashed the page. It's used by:

- every member column: workout plans, recordings, subscriptions, subscription requests, attendances, participants (all three grids), train groups (trainer), emails, exercise history
- the member dropdowns
- the grid filter, whose labels now say "Name (username)", so two people with the same name can be told apart
- the email recipients box (name, username, and the email address beside it)
- the subscription-decision dialog, the attendance form and the admin Start dialog

The status colour stays where it was shown before (plans list, participant grids, dropdowns). The Users page keeps its separate columns. Not changed: the member's own name in the top bar and on their profile, and the plan header.

*Search.* Every typed word has to match first name, last name, username, email or phone, with no accents and any case, in any order. "γιαν παπ" finds Γιάννης Παπαδόπουλος only, and a pasted "Name (username)" works. Results are sorted by name. The grid filter used to throw away the server's matches with a second accent-sensitive check in the browser, so searching it by name never worked; fixed.

*Security (the user asked to include the whole Users controller).*

| Endpoint | Before | Now |
|---|---|---|
| `Users/Lookup`, `Users/AutoComplete` | every member, to anyone | everyone for staff holding a picker permission (`Users_View`, `TrainGroups_View`, `TrainGroupParticipants_View`, `WorkoutPlansAdmin_View`, `WorkoutPlanRecordingsAdmin_View`, `SubscriptionsAdmin_View`/`_Edit`, `Mails_View`/`_Add`); anyone else gets only themselves. AutoComplete no longer sends medical history or address |
| `Users/GetDataTable` | anyone | `Users_View` |
| `GET Users/{id}` | anyone, full record | your own, or `Users_View` |
| `PUT Users/{id}` | anyone could edit anyone, role included (a member could make themselves Administrator) | `Users_Edit` edits anyone as before; otherwise your own profile only, taking just first name, last name, address, medical history and photo |
| `DELETE Users/{id}` | anyone could delete anyone | `Users_Delete` |
| Trainer in `Users/TimeSlots` and `TrainGroupDates/TimeSlots` | the trainer's full record to every member | name and photo only (`TrainerDto.From`) |
| `TrainGroups/GetDataTable` (admin train-groups list) | anyone, each row carrying the trainer's full record | `TrainGroups_View` (staff). The trainer goes out as name, username and photo (`TrainerDto.ForStaff`) |
| `PUT TrainGroups/{id}` | the body's trainer object was mapped onto the saved group | the trainer is never written through a group; `trainerId` alone says who it is. The form no longer sends it |

Checked on a test database with made-up members (Γιάννης Παπαδόπουλος, Γιάννης Κωνσταντίνου, Ελένη Παπαδοπούλου, one with no names): 43 API checks covering search and the Users rows above, as member, trainer and admin, plus 26 for the train-groups rows (a member is refused, staff rows hold no email, address or medical history, the trainer filter works, a save carrying a changed trainer leaves the trainer's record untouched, the booking calendar is unchanged). In the browser:

- The plans list shows the new names, with status colour.
- The filter finds "γιαν παπ", filters the list, and restores from the URL.
- The plan dialog's member dropdown finds "ελενη" and "γιαν κων".
- The email box shows name, username and email.
- The member saved their own name and medical history.
- The booking calendar still shows the trainer.
- The admin train-groups list shows the trainer as "Κώστας Γυμναστής (Trainer)", and editing a group from it saves.

## 2. Category colours, categories on the booking calendar (30)

- New `Color` field on `TrainGroupCategory` (migration).
- Category form: at least 6 preset colours that read well in light and dark mode, plus a colour picker for a custom colour.
- Booking calendar: category filter buttons like the admin calendar's tabs, and a coloured category label on each session (mockup B).
- Admin calendar: sessions use their category colour.
- Day buttons keep their dots without colour; the dots already mean "you're booked that day".
- Sessions without a category: grey, with their own filter.

**Done.** One migration, generated with the EF CLI: `Migration_22` adds `Color` (text, up to 7 characters, empty by default) to `TrainGroupCategories`. Existing categories keep an empty colour and show a preset picked by their id until someone opens and saves them; saving stores the colour they were showing.

*Colours.* Eight presets (teal, blue, purple, pink, red, orange, amber, green) plus grey for "no category". The app runs on whichever of its ~30 PrimeReact themes the user picked, so each preset was measured: at least 3:1 contrast against every theme's card colour, white through the darkest. Labels never put text on the full colour: they use a light tint of it under the theme's own text colour, so any custom colour stays readable on any theme. Shared code: `Client/src/services/CategoryColorService.tsx` and `Client/src/components/core/category/CategoryTagComponent.tsx`.

*Category form.* The eight presets as round swatches, then a rainbow swatch that opens a colour picker for any other colour, the hex code, and a preview of the label as it shows on the calendar. A new category starts on the first preset no other category uses. The categories list has a colour column. The server accepts only `#rrggbb` (or empty), stores it in lower case, and members still cannot add or change categories.

*Booking page (mockup B).* Under the day's heading, a row of filter buttons: "Όλα", one per category with its colour dot, and "Χωρίς κατηγορία" on days that have such sessions (it stays while selected, so it doesn't vanish when the member changes day). Each shows its count, and the row scrolls sideways on a phone. Each session has a 4px bar in its colour down the left edge (grey for none) and a tinted category label beside the title. The day buttons keep their uncoloured dots. A gym with no categories sees the page as before.

*Admin calendar.* Each category tab has its colour dot, a "Χωρίς κατηγορία" tab is added on the same rule, and each session button is tinted in its colour with the bar on the left. The category dropdown in the train-group form shows the dots too.

*Bug fixed along the way.* Opening a train group's edit page by its link (or refreshing it) didn't load the group's category, and saving any field then removed the group from its category. `TrainGroupAdminFormPage` now loads it. Confirmed before and after on the test database.

Checked on a test database with made-up categories and sessions: 31 API checks (migration on a fresh database, valid and invalid colours on add and edit, lower-casing, member refused, every role reads the colours from the lookup, other lookups unchanged, each session carries its category). In the browser: the categories list and form (free preset preselected, custom colour, an old category saving its preset), the admin calendar tabs and buttons, the booking page filter and colours on a dark and a light theme, and at phone width with no sideways scroll.

Noticed, not changed: `API/JsonConverter/JsonDateTimeConverter.cs` turns stored times into UTC using the server's own time zone. On a computer set to Greek time (like the test PC) every time comes out 2 hours early and each save of a train group moves it 2 hours earlier. A server running on UTC is not affected.

## 5a. Deload week (40)

- New `IsDeload` checkbox on each rule week (`WorkoutPlanRuleWeek`, migration), in the rule form next to the week's message.
- The trainer's week message becomes the deload instruction ("Μείωσε τα κιλά 40%, ίδιες επαναλήψεις"). If there's none, a default line is shown.

What the member sees (no extra dialog to dismiss):

1. **Start dialog.** When the week about to start is a deload week, the dialog opens with a deload panel on top: "Εβδομάδα αποφόρτισης", the week number and the trainer's message. The button reads "Έναρξη deload προπόνησης". This is the moment that matters, before they pick their weights.
2. **On Start.** A short falling two-note sound plays (different from the 1-hour sound), and the deload banner slides in at the top of the plan.
3. **During the workout.** The banner stays pinned at the top while they scroll through exercises, and the timer bar at the bottom shows a "Deload" tag next to "In progress".
4. **Plan list.** A "Deload" badge on the plan while its current week is a deload week.

Colour: a calm teal, not amber or red, so it reads as "lighter week" and not as a warning.

The sound code is written here and reused by 5b.

## 5b. Sound at 1 hour, keep screen on (20)

- On the running workout (`WorkoutPlanRecordingActionsComponent.tsx`): a "keep screen on" toggle (Screen Wake Lock), remembered per phone.
- At 60 minutes from the recording's start (server time): a sound and a message.
- If the page was hidden at 60 minutes, the alert plays when the member comes back while the workout is still running.
- Limitation agreed with the owner: it works only while the workout page is open on screen.

## 9. Dialog after Stop (20 + 20)

Order from the owner: tiredness first, then "how did it go, any problem?".

- **Stop first.** The workout stops the moment they press Stop, as today. The dialog then saves to the stopped recording. Closing it or pressing "Παράλειψη" leaves the answers empty. Workouts that lapse after 3 hours have no answers.
- **Tiredness 1-20.** Twenty number buttons in five labelled rows of four: Πολύ ελαφριά, Ελαφριά, Μέτρια, Δύσκολη, Εξαντλητική, tinted green, then amber, then red. Required to save.
- **Problem.** Two cards: "Όλα καλά" and "Είχα πρόβλημα". The second opens a fixed label "25/11/2026 · nov26 day2" and a required text box. The save button becomes "Αποθήκευση και άνοιγμα ιστορικού".
- **Database (one migration).** On `WorkoutPlanRecording`, a nullable int for tiredness and a nullable bool for "had a problem". Both show in the recordings grid, and the grid can filter on "had a problem".
- **One new endpoint**, for example `POST WorkoutPlanRecordings/Feedback/{id}` with the tiredness and the optional text:
  - The caller must own the recording.
  - It saves the tiredness and the problem flag.
  - With text, it appends to the member's medical history (the editor's HTML): a bold "date · plan title" line, then the member's text, HTML-escaped.
  - The server builds the date and plan title from the recording (Greek time, not UTC, and the recording's stored plan title), so the client can't fake them.
- **After saving with a problem:**
  - Refresh the profile's copy of the user, or the tab shows the old text and a later Save there would erase the new entry.
  - Open the profile on the Medical History tab. `UserProfilePage.tsx` can't select a tab from the URL yet, so add that (for example `?tab=medical-history`).

## 4. Admin statistics (40)

On the existing admin charts page (`pages/administrator/ChartsComponent.tsx`):

- Subscriptions over time with a week / month / year switch. Number of subscriptions, no money.
- Busiest days and hours: a weekday × hour grid counting attendances.
- Top train groups by attendance.
- The last two share a 30 / 90 / 365-day selector, starting at 90 days.

## 12. Email fixes and a review of every app email (free)

**The bug the owner found.** The "one subscription left" email (`TrainGroupΑttendancesController.cs`, `GetAttendanceEmailBody`) has the title "only one subscription left" and "Remaining: 1", then ends with "This was your last remaining subscription". The last line contradicts the rest. It should say that one is left, and how to renew before it runs out.

**Emails the app sends:**

| Email | Where | Problems found so far |
|---|---|---|
| One subscription left | `TrainGroupΑttendancesController.cs` | The contradiction above |
| Subscription request answered / renewed | `SubscriptionsController.cs` | Review the wording |
| Bookings added / removed | `GmailEmailService.SendBookingEmailAsync` | Shows a "No bookings were added" section even when only removals happened, which reads like a system log |
| Password reset | `AuthController.cs` | Hard-coded English, not translated |
| Owner's own emails | `MailsController.cs` | Written by the owner; only the shared layout below applies |

**What the review covers:**

- Every email is checked for logic: numbers, dates and wording agree with each other and with what actually happened.
- Professional tone for a gym, in correct Greek and in the other languages the app ships.
- One shared layout for all of them: the gym's name at the top, a short greeting, the message, a button linking to the right page (profile, bookings), and a sign-off with the gym's name.
- The greeting doesn't use the member's name. Greek names change form when you address someone (Αλέξης → Αλέξη), and getting that wrong looks careless.
- Train group titles and names are HTML-escaped in emails.
- Sections that have nothing in them are left out.

**Language: always Greek (decided).** Today emails are written in the language of whoever triggered them (the trainer's or admin's browser language, sent with each request). Every app email will be built in Greek regardless of that, so no migration. The email texts still live in the translation files; only the Greek ones are used for email.
