import { useEffect, useRef } from "react";
import { gsap } from "gsap";
import { ScrollTrigger } from "gsap/ScrollTrigger";
import Hero2 from "./Hero2";

// Register GSAP ScrollTrigger
gsap.registerPlugin(ScrollTrigger);

function Home() {
  return (
    <>
      <div style={{ minHeight: "200vh" }}>
        {/* <div style={{ height: "100vh" }}></div> */}
        {/* <Hero /> */}
        <Hero2 />
      </div>
    </>
  );
}

export default Home;
