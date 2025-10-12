import { useTranslator } from "../../services/TranslatorService";

export default function Home() {
  const { t } = useTranslator();

  return (
    <>
      <div className="flex flex-column justify-content-center align-content-center align-items-center h-full">
        <div className="w-8">
          {/* <p className="text-center">
            Στο ROSA CoreLab πιστεύουμε ότι η αληθινή δύναμη ξεκινά από μέσα
            μας.
          </p>

          <p className="text-center">
            Ο χώρος μας είναι ένας πολυχώρος αφιερωμένος στην αυτοβελτίωση, την
            ευεξία και την προσωπική ανάπτυξη.
          </p>

          <p className="text-center">
            Εδώ, κάθε άνθρωπος μπορεί να ανακαλύψει τον δικό του ρυθμό, να
            καλλιεργήσει το σώμα του αλλά και το πνεύμα του, να βρει ισορροπία
            και να προχωρήσει με σιγουριά στο επόμενο βήμα.
          </p>

          <p className="text-center">
            Προσφέρουμε δυνατότητες στοχευμένης φυσικής ενδυνάμωσης, μέσα και
            από τους θησαυρούς των τεχνικών των πολεμικών τεχνών, που
            αναπτύσσουν αντοχή, πειθαρχία και αυτοπεποίθηση. Παράλληλα, μέσω του
            life coaching και της συμβουλευτικής διατροφής, βοηθάμε τους
            ανθρώπους να βρουν καθαρότητα στόχων και να υιοθετήσουν έναν τρόπο
            ζωής που τους ταιριάζει.
          </p>

          <p className="text-center">
            Η ομάδα μας συνεργάζεται με επαγγελματίες διατροφολόγους και
            ειδικούς ψυχικής υγείας, γιατί πιστεύουμε ότι η πραγματική αλλαγή
            έρχεται όταν φροντίζουμε ολόκληρο τον εαυτό μας σώμα, νου και ψυχή.
          </p>

          <p className="text-center">
            Στο ROSA CoreLab, δεν χρειάζεται κανένα εφόδιο για να ξεκινήσεις.
            Χρειάζεται μόνο η επιθυμία να βελτιώνεσαι. Είμαστε εδώ για να σε
            στηρίξουμε στο ταξίδι σου προς μια πιο δυνατή, πιο φωτεινή εκδοχή
            του εαυτού σου. Γιατί για εμάς, η αυτοβελτίωση δεν είναι στόχος·
            είναι τρόπος ζωής.
          </p> */}

          <p className="text-center">
            {t(
              "At ROSA CoreLab, we believe that true strength starts from within us."
            )}
          </p>

          <p className="text-center">
            {t(
              "Our space is a multi-purpose venue dedicated to self-improvement, wellness, and personal development."
            )}
          </p>

          <p className="text-center">
            {t(
              "Here, every person can discover their own rhythm, cultivate their body as well as their mind, find balance, and proceed with confidence to the next step."
            )}
          </p>

          <p className="text-center">
            {t(
              "We offer opportunities for targeted physical strengthening, also through the treasures of martial arts techniques, which develop endurance, discipline, and self-confidence. At the same time, through life coaching and nutritional counseling, we help people find clarity in their goals and adopt a lifestyle that suits them."
            )}
          </p>

          <p className="text-center">
            {t(
              "Our team collaborates with professional nutritionists and mental health experts, because we believe that real change comes when we care for our entire self—body, mind, and soul."
            )}
          </p>

          <p className="text-center">
            {t(
              "At ROSA CoreLab, you dont need any equipment to start. All you need is the desire to improve. We are here to support you on your journey toward a stronger, brighter version of yourself. Because for us, self-improvement is not a goal; it is a way of life."
            )}
          </p>
        </div>
      </div>
    </>
  );
}
