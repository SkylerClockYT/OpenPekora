import globals from "globals";
import tseslint from "typescript-eslint";
import { defineConfig } from "eslint/config";

export default defineConfig([
    {
        files: ["**/*.{js,mjs,cjs,ts}"],
        languageOptions: {
            globals: globals.node,
            parser: "@babel/eslint-parser",
            parserOptions: {
                "sourceType": "module",
            }
        },
        rules: {
            //"no-var": "error",
            "semi": "error",
            "no-extra-semi": "error"
        },
    },
    tseslint.configs.stylistic,
]);