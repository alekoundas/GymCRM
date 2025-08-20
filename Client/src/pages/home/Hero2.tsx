import React, { useEffect, useRef } from "react";
import { gsap } from "gsap";
import { ScrollTrigger } from "gsap/ScrollTrigger";
import { Button } from "primereact/button"; // Example PrimeReact integration

gsap.registerPlugin(ScrollTrigger);

const HomePage: React.FC = () => {
  const videoRef = useRef<HTMLVideoElement>(null);
  const parallaxRef1 = useRef<HTMLDivElement>(null);
  const parallaxRef2 = useRef<HTMLDivElement>(null);

  useEffect(() => {
    // Scroll-synced video: Scrub through frames based on scroll position
    const video = videoRef.current;
    if (video) {
      // video.pause(); // Ensure it's not auto-playing
      // video.currentTime = 0; // Start at beginning

      // gsap.to(video, {
      //   currentTime: video.duration || 0, // Scrub to end as you scroll
      //   ease: "none",
      //   scrollTrigger: {
      //     trigger: ".video-section",
      //     start: "top top", // Pin when section hits top of viewport
      //     end: "+=200%", // Extend scrub duration for smooth pacing (adjust based on video length)
      //     scrub: true, // Sync with scroll
      //     pin: true, // Pin the section while scrubbing
      //     anticipatePin: 1, // Smooth pinning
      //   },
      // });

      let tl = gsap.timeline({
        scrollTrigger: {
          trigger: "video",
          start: "top top",
          // end: "bottom center",
          end: "+=400%", // Extend scrub duration for smooth pacing (adjust based on video length)
          scrub: 1,
          pin: true,
          markers: true,
        },
      });

      video.onloadedmetadata = () => {
        tl.to(video, {
          currentTime: video.duration,
        });
      };
    }

    // // Parallax effect for image layers
    // gsap.to(parallaxRef1.current, {
    //   y: "20%", // Move slower vertically (adjust for depth)
    //   ease: "none",
    //   scrollTrigger: {
    //     trigger: ".parallax-section",
    //     start: "top bottom",
    //     end: "bottom top",
    //     scrub: true,
    //   },
    // });

    // gsap.to(parallaxRef2.current, {
    //   y: "40%", // Deeper layer moves more
    //   ease: "none",
    //   scrollTrigger: {
    //     trigger: ".parallax-section",
    //     start: "top bottom",
    //     end: "bottom top",
    //     scrub: true,
    //   },
    // });

    // Refresh ScrollTrigger on load/resize
    ScrollTrigger.refresh();
  }, []);

  return (
    <div className="homepage">
      <section
        className="video-section"
        style={{ height: "100vh", position: "relative", overflow: "hidden" }}
      >
        <video
          ref={videoRef}
          src="/Gymvideo6.mp4" // Replace with your gym video asset (muted, no controls)
          muted
          playsInline
          style={{
            width: "100%",
            height: "100%",
            objectFit: "cover",
            position: "absolute",
            top: 0,
            left: 0,
          }}
        />
        <div
          className="overlay"
          style={{
            position: "absolute",
            top: "50%",
            left: "50%",
            transform: "translate(-50%, -50%)",
            textAlign: "center",
            color: "white",
          }}
        >
          <h1>Welcome to Your Gym</h1>
          <Button
            label="Join Now"
            icon="pi pi-check"
          />
        </div>
      </section>

      <section
        className="parallax-section"
        style={{ height: "100vh", position: "relative", overflow: "hidden" }}
      >
        <div
          className="content"
          style={{
            position: "relative",
            top: "50%",
            textAlign: "center",
            color: "white",
          }}
        >
          <h2>Experience the Power</h2>
          <p>Scroll to see parallax in action.</p>
        </div>
      </section>

      <section
        style={{ height: "100vh", position: "relative", overflow: "hidden" }}
      >
        <div
          style={{
            position: "relative",
            top: "50%",
            textAlign: "center",
            color: "white",
          }}
        >
          <h2>Experience the Power</h2>
          <p>Scroll to see parallax in action.</p>
        </div>
      </section>

      {/* Additional Sections: Add more gym content */}
      <section
        style={{ height: "100vh", backgroundColor: "#f0f0f0", padding: "50px" }}
      >
        <h2>Our Workouts</h2>
        <p>Details about classes, equipment, etc.</p>
      </section>

      {/* Parallax Image Section: Images shift as you scroll */}
      <section
        className="parallax-section"
        style={{ height: "100vh", position: "relative", overflow: "hidden" }}
      >
        <div
          ref={parallaxRef1}
          style={{
            position: "absolute",
            top: 0,
            left: 0,
            width: "100%",
            height: "100%",
            backgroundImage: "url(/gym1.jpg)",
            backgroundSize: "cover",
          }}
        />
        <div
          className="content"
          style={{
            position: "relative",
            zIndex: 1,
            top: "50%",
            textAlign: "center",
            color: "white",
          }}
        >
          <h2>Experience the Power</h2>
          <p>Scroll to see parallax in action.</p>
        </div>
      </section>
    </div>
  );
};

export default HomePage;
