// Node
const fs = require("fs");
const path = require("path");

// Paths
const jsonFilePath = path.join(__dirname, "../../public/translations/en.json");
const outputPath = path.join(
  __dirname,
  "../model/core/translation-types/TranslationTypes.tsx"
);

const langs = ["en", "el"];

// Collect all unique keys from JSON
if (fs.existsSync(jsonFilePath)) {
  const json = JSON.parse(fs.readFileSync(jsonFilePath, "utf8"));
  const allKeys = Object.keys(json);
  const translationTypes = allKeys
    .map((key) => `  '${key}': string;`)
    .join("\n");

  const content = `

    // Auto-generated from JSON
    //  - Run 'npm run generate-translation-types' to update 
    //    or  'npm run generate-translations' to scan and update
    
    export interface TranslationTypes {
    ${translationTypes}
    }


  `;

  fs.writeFileSync(outputPath, content, "utf8");
  console.log(`Generated types for ${allKeys.length} keys in ${outputPath}`);
}
