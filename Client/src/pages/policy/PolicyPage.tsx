import { Button } from "primereact/button";
import { Card } from "primereact/card";

export default function PolicyPage() {
  return (
    <>
      <Card>
        <div className="card-header">
          <span className="card-title">Privacy Policy</span>
        </div>
        <div className="p-card-content">
          <p>
            <strong>Effective Date: October 29, 2025</strong>
          </p>
          <p>
            RosaCoreLab ("we", "us", or "our") is committed to protecting your
            privacy. This Privacy Policy explains how we collect, use, disclose,
            and safeguard your information when you use our web application (the
            "App"), hosted in Greece and compliant with the General Data
            Protection Regulation (GDPR). By using the App, you consent to the
            practices described herein. We do not sell your personal data and
            use it solely to provide and improve our services.
          </p>

          <h2>1. Information We Collect</h2>
          <p>
            We collect limited personal information necessary to provide our
            services. This includes:
          </p>
          <ul>
            <li>
              <strong>Account Information:</strong> When you create an account,
              we collect your email address, name, and gym-related details
              (e.g., membership ID) to manage bookings and notifications.
            </li>
            <li>
              <strong>Usage Data:</strong> Automatically collected information
              such as IP address, device type, browser type, pages visited, and
              time spent on the App.
            </li>
            <li>
              <strong>Local Storage Data:</strong> We use browser local storage
              (not cookies) to store non-personal data like API authentication
              tokens, selected language, logged-in user ID, and theme
              preferences for a seamless user experience. This data is stored on
              your device and is not accessible to us directly.
            </li>
            <li>
              <strong>Email Data:</strong> For transactional purposes, we may
              send automated emails (e.g., booking confirmations) from our admin
              Gmail account via the Gmail API. We do not read, store, or access
              your email content or personal emails.
            </li>
          </ul>
          <p>
            We do not collect sensitive personal data (e.g., health or financial
            information) unless voluntarily provided for booking purposes, and
            even then, it is minimized and secured.
          </p>

          <h2>2. How We Use Your Information</h2>
          <p>We use the collected information to:</p>
          <ul>
            <li>
              Provide and improve App services, such as managing gym bookings
              and sending notifications.
            </li>
            <li>
              Personalize your experience (e.g., language and theme via local
              storage).
            </li>
            <li>
              Send transactional emails only (no marketing without consent).
            </li>
            <li>
              Comply with legal obligations under GDPR and Greek data protection
              laws.
            </li>
            <li>
              Analyze usage to enhance functionality (anonymized where
              possible).
            </li>
          </ul>
          <p>
            Email sending complies with Google's Limited Use Policy: We only
            send messages for legitimate GymCRM notifications and do not use
            data for advertising, profiling, or unauthorized purposes.
          </p>

          <h3>Cookies and Tracking</h3>
          <p>
            We do not use cookies or third-party trackers. All personalization
            is handled via local storage on your device.
          </p>

          <h2>3. Sharing Your Information</h2>
          <p>
            We do not sell, trade, or share your personal information.
            Disclosures are limited to:
          </p>
          <ul>
            <li>
              Service providers (e.g., hosting, email API) bound by
              GDPR-compliant contracts (e.g., Google for Gmail API, which
              processes data in the EEA).
            </li>
            <li>
              Legal requirements (e.g., court orders) or to protect our rights.
            </li>
          </ul>
          <p>
            No data is transferred outside the EEA without adequacy decisions or
            standard contractual clauses.
          </p>

          <h2>4. Data Security</h2>
          <p>
            We implement reasonable security measures (e.g., encryption, access
            controls) to protect your data. Local storage data is device-bound.
            However, no system is infallible—please safeguard your account
            credentials.
          </p>

          <h2>5. Your Rights Under GDPR</h2>
          <p>As an EU resident, you have rights including:</p>
          <ul>
            <li>Access, rectification, or erasure of your data.</li>
            <li>Restriction or objection to processing.</li>
            <li>Data portability.</li>
            <li>Withdraw consent at any time.</li>
          </ul>
          <p>
            To exercise these, contact us at [rosacorelab@gmail.com]. We respond
            within one month. You may also lodge complaints with the Hellenic
            Data Protection Authority (HDPA).
          </p>

          <h2>6. Children's Privacy</h2>
          <p>
            The App is not intended for children under 16. We do not knowingly
            collect data from minors.
          </p>

          <h2>7. Retention</h2>
          <p>
            We retain data only as long as necessary for service provision
            (e.g., account data until deletion request) or legal requirements,
            then securely delete it.
          </p>

          <h2>8. Changes to This Policy</h2>
          <p>
            We may update this policy. Changes will be posted here with the
            updated date. Continued use constitutes acceptance.
          </p>

          <h2>9. Contact Us</h2>
          <p>
            For questions, contact: RosaCoreLab, Email: [rosacorelab@gmail.com].
          </p>
        </div>
      </Card>
    </>
  );
}
