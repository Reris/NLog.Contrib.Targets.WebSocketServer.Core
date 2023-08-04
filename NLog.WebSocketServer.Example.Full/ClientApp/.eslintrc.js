module.exports = {
  "env": {
    "browser": true,
    "es2021": true
  },
  "overrides": [
    {
      extends: [
        "eslint:recommended",
        "plugin:@typescript-eslint/recommended"
      ],
      "files": ["src/**/*.js", "src/**/*.ts"],
      "parserOptions": {
        "tsconfigRootDir": __dirname,
        "project": "tsconfig.json",
        "ecmaVersion": "latest",
        "sourceType": "module"
      }
    }
  ]
}
