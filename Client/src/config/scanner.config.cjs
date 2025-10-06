module.exports = {
  input: [
    "src/**/*.tsx", // Scan TSX for t()
    "!src/**/*.d.ts", // Skip type defs
    "!src/enum/**", // Skip pure enums (no t())
    "!src/model/**", // Skip DTOs (no t())
  ],
  // output: "./src/model/core/translation-types", // FIXED: JSON output (scanner is for JSON, not TS)
  options: {
    debug: true,
    sort: false,
    func: {
      list: ["i18next.t", "i18n.t", "t"], // t() detection
      trans: false,
      extensions: [".tsx"],
    },
    trans: {
      component: "Trans",
      i18nKey: "i18nKey",
      extensions: [".js", ".jsx"], // JS only for Trans
      fallbackKey: (ns, value) => value,
      acorn: {
        ecmaVersion: 2022, // Modern JS/TS tolerance
        sourceType: "module",
        allowReturnOutsideFunction: true,
        allowImportExportEverywhere: true,
      },
    },
    lngs: ["en", "el"],
    ns: ["translation"],
    defaultLng: "en",
    defaultNs: "translation",
    defaultValue: "__STRING_NOT_TRANSLATED__",
    resource: {
      loadPath: "public/translations/{{lng}}.json",
      savePath: "public/translations/{{lng}}.json", // Updates JSON
      jsonIndent: 2,
      lineEnding: "\n",
    },
    nsSeparator: ":",
    keySeparator: false,
    interpolation: {
      prefix: "{{",
      suffix: "}}",
    },
    removeUnusedKeys: false, // Keep all
  },
};
