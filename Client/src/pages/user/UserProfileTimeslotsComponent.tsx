import { useUserStore } from "../../stores/UserStore";
import { TimeSlotResponseDto } from "../../model/TimeSlotResponseDto";
import { useEffect, useRef, useState } from "react";
import ApiService from "../../services/ApiService";
import { TimeSlotRequestDto } from "../../model/TimeSlotRequestDto";
import FullCalendar from "@fullcalendar/react";
import dayGridPlugin from "@fullcalendar/daygrid";
import timeGridPlugin from "@fullcalendar/timegrid";
import listPlugin from "@fullcalendar/list";
import { TrainGroupDateTypeEnum } from "../../enum/TrainGroupDateTypeEnum";
import { ThemeService } from "../../services/ThemeService";

export default function UserProfileTimeslotsComponent() {
  const { userDto, updateUserDto } = useUserStore();
  const calendarRef = useRef<FullCalendar>(null);
  const [events, setEvents] = useState<any[]>([]);
  const [timeSlots, setTimeSlots] = useState<TimeSlotResponseDto[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchTimeSlots = async (currentDate: Date) => {
    const timeSlotDto = new TimeSlotRequestDto();

    const dateCleaned = new Date(
      currentDate.getFullYear(),
      currentDate.getMonth(),
      currentDate.getDate(),
      0,
      0,
      0,
      0
    );

    timeSlotDto.selectedDate = dateCleaned.toISOString();
    const response = await ApiService.timeslots("Users/TimeSlots", timeSlotDto); // Replace with your API endpoint

    // Generate 7 days starting from sunday of current week
    const calendarDates = Array.from({ length: 7 }, (_, i) => {
      const date = new Date(currentDate);
      date.setDate(currentDate.getDate() + i);
      return date;
    });

    if (response) {
      const mappedEvents = response.flatMap((slot) =>
        slot.recurrenceDates.map((x) => {
          let startDate: Date;

          // Fixed Date
          if (x.trainGroupDateType === TrainGroupDateTypeEnum.FIXED_DAY)
            startDate = new Date(x.date);

          // Date Of Month
          if (x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_MONTH)
            startDate = new Date(new Date().setDate(new Date(x.date).getDay()));

          // Date Of Week
          if (x.trainGroupDateType === TrainGroupDateTypeEnum.DAY_OF_WEEK) {
            const dateOfWeek = calendarDates.find(
              (y) => y.getDay() === new Date(x.date).getDay()
            );
            if (dateOfWeek) {
              startDate = dateOfWeek;
            }
          }

          // One off participant
          if (!x.trainGroupDateId) {
            startDate = new Date(x.date);
          }

          startDate = new Date(
            startDate!.getFullYear(),
            startDate!.getMonth(),
            startDate!.getDate(),
            new Date(slot.startOn).getHours(),
            new Date(slot.startOn).getMinutes(),
            0
          );

          // Calculate endDate
          const durationMinutes =
            new Date(slot.duration).getHours() * 60 +
            new Date(slot.duration).getMinutes();
          const endDate = new Date(
            startDate.getTime() + durationMinutes * 60 * 1000
          );

          return {
            // title: `${formatDate(x.date)} (${slot.title})`,
            title: `${slot.title}`,
            start: startDate,
            end: endDate,
            backgroundColor: x.isUserJoined ? "#007ad9" : "#ced4da", // Joined: blue, not: gray
            extendedProps: { isUserJoined: x.isUserJoined },
          };
        })
      );
      setEvents(mappedEvents);
      setLoading(false);
      setTimeSlots(response);
    }

    setLoading(false);
  };
  // Load data.
  useEffect(() => {
    setLoading(false);
    // fetchTimeSlots();
  }, []);

  // Listen for theme changes
  useEffect(() => {
    // Initial call on mount to set starting theme
    handleThemeChange();

    // Listen to custom event (dispatched from switcher)
    document.addEventListener("primeThemeChange", handleThemeChange);
    return () =>
      document.removeEventListener("primeThemeChange", handleThemeChange);
  }, []);

  const handleThemeChange = () => {
    const palette = ThemeService.getCurrentThemeColors();
    const calendarApi = calendarRef.current?.getApi();

    // DOM manipulation for header styling (no CSS required)
    if (calendarApi && palette.primaryColor) {
      // Update global event colors (overrides per-event if set)
      calendarApi.setOption("eventBackgroundColor", palette.primaryColor);
      calendarApi.setOption("eventBorderColor", palette.primaryColor);
      calendarApi.setOption("eventTextColor", palette.textColor);

      // Re-render events with updated colors
      calendarApi.refetchEvents();
      // Toolbar header (top bar)
      const toolbar = document.querySelector(
        ".fc-header-toolbar"
      ) as HTMLElement | null;
      if (toolbar) {
        toolbar.style.backgroundColor = palette.surfaceCard;
        toolbar.style.color = palette.textColor;
      }

      // Day headers (e.g., "Fri 1/1") - .fc-col-header-cell
      const dayHeaders = document.querySelectorAll(
        ".fc-col-header-cell"
      ) as NodeListOf<HTMLElement>;
      dayHeaders.forEach((header) => {
        header.style.backgroundColor = palette.surfaceCard;
        header.style.color = palette.textColor;
        // header.style.borderBottom = `1px solid ${palette.surfaceBorder}`;
      });
      // Day headers (e.g., "Fri 1/1") - .fc-col-header-cell
      const dayHeader = document.querySelectorAll(
        ".fc-timegrid-axis"
      ) as NodeListOf<HTMLElement>;
      dayHeader.forEach((header) => {
        header.style.backgroundColor = palette.surfaceCard;
        header.style.color = palette.textColor;
        // header.style.borderBottom = `1px solid ${palette.surfaceBorder}`;
      });

      // Grid lines - .fc-timegrid-cols .fc-timegrid-col borders (vertical/horizontal)
      const gridCols = document.querySelectorAll(
        ".fc-timegrid-cols .fc-timegrid-col"
      ) as NodeListOf<HTMLElement>;
      gridCols.forEach((col) => {
        // col.style.borderRight = `1px solid ${palette.surfaceBorder}`; // Dynamic grey from theme
        // col.style.borderBottom = `1px solid ${palette.surfaceBorder}`; // Dynamic grey for horizontal
      });

      // Time slot lines (horizontal grid in time axis)
      const timeSlots = document.querySelectorAll(
        ".fc-timegrid-slot"
      ) as NodeListOf<HTMLElement>;
      timeSlots.forEach((slot) => {
        // slot.style.borderTop = `1px solid ${palette.surfaceBorder}`; // Dynamic grey for time grid lines
      });
    }
  };

  const formatDate = (dateStr: string): string => {
    const date = new Date(dateStr);
    return `${date.getDate()}/${date.getMonth() + 1}/${date.getFullYear()}`;
  };

  if (loading) {
    return <div className="text-center p-4">Loading...</div>;
  }

  return (
    <div className="p-4">
      <FullCalendar
        ref={calendarRef}
        plugins={[dayGridPlugin, timeGridPlugin, listPlugin]}
        initialView="timeGridWeek"
        initialDate={new Date()}
        allDaySlot={false} // Hide the All Day row
        dayHeaderFormat={{
          weekday: "short",
          day: "numeric",
          month: "numeric",
          omitCommas: true,
        }} // Custom format: "Sun 14/9"
        slotLabelFormat={{
          hour: "2-digit",
          minute: "2-digit",
          hour12: false,
          omitZeroMinute: false,
        }} // 24-hour format for time axis: "14:00"
        dayMaxEvents={false}
        headerToolbar={{
          left: "prev,next today",
          center: "title",
          right: "timeGridWeek,timeGridDay,listWeek",
        }}
        events={events}
        eventContent={(arg) => (
          <div className="">
            <b>{arg.event.title}</b>
            <p>{arg.timeText}</p>
          </div>
        )}
        height="auto"
        editable={false} // Optional: Allow drag-and-drop
        themeSystem="standard" // Enables CSS vars theming (default, but explicit)
        datesSet={(x) => {
          fetchTimeSlots(x.start);

          // Apply theme after dates are rendered (includes headers, day cells, time grid)
          const timer = setTimeout(handleThemeChange, 0);
          return () => clearTimeout(timer);
        }} // Callback after dates render (modern equivalent for post-render DOM manipulation)
      />
    </div>
  );
}
