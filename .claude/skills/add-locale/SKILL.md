---
name: add-locale
description: Adds a new locale to RunCat365. Use when adding a new language, creating a Strings.{lc}.resx file, registering a CultureInfo, or extending SupportedLanguage.cs with a new entry.
argument-hint: <language name or BCP 47 code>
---

# Add a New Locale to RunCat365

Modify 3 files to add support for `$ARGUMENTS`. French (`fr`) is used as an example.

## Steps

### 1. Create `RunCat365/Properties/Strings.{lc}.resx`

Read `RunCat365/Properties/Strings.resx` for all keys and their English values.
Use `Strings.es.resx` as the structural template.

- File name: `Strings.{lc}.resx` (e.g. `Strings.fr.resx`)
- Copy the XML header, schema, and `<resheader>` blocks verbatim from `Strings.es.resx`
- Include every key from `Strings.resx` — no more, no less — with each `<value>` translated
- Keep all attribute names, XML structure, and `<comment>` content in English; translate only `<value>` text
- Verify every key from `Strings.resx` is present in the new file

### 2. Edit `RunCat365/SupportedLanguage.cs`

Keep the `SupportedLanguage` enum and every `switch` arm sorted **alphabetically by enum identifier** (e.g. `French` before `German` before `Japanese`). Insert the new language at its alphabetical position in each location below — do not append.

Add the language to the `SupportedLanguage` enum:

```csharp
enum SupportedLanguage
{
    English,
    French,
    Japanese,
    Spanish,
}
```

Add the ISO code to `GetCurrentLanguage()` (arms ordered by enum identifier, so `"fr"` appears before `"ja"` even though the ISO codes aren't themselves alphabetical):

```csharp
return culture.TwoLetterISOLanguageName switch
{
    "fr" => SupportedLanguage.French,
    "ja" => SupportedLanguage.Japanese,
    "es" => SupportedLanguage.Spanish,
    _ => SupportedLanguage.English,
};
```

Add the culture to `GetDefaultCultureInfo()`:

```csharp
return language switch
{
    SupportedLanguage.French => new CultureInfo("fr-FR"),
    SupportedLanguage.Japanese => new CultureInfo("ja-JP"),
    SupportedLanguage.Spanish => new CultureInfo("es-ES"),
    _ => new CultureInfo("en-US"),
};
```

Add the font to `GetFontName()` — use `"Consolas"` for Latin-script languages:

```csharp
return language switch
{
    SupportedLanguage.French => "Consolas",
    SupportedLanguage.Japanese => "Noto Sans JP",
    SupportedLanguage.Spanish => "Consolas",
    _ => "Consolas",
};
```

Add the full-width flag to `IsFullWidth()` — use `false` for Latin-script languages:

```csharp
return language switch
{
    SupportedLanguage.French => false,
    SupportedLanguage.Japanese => true,
    SupportedLanguage.Spanish => false,
    _ => false,
};
```

### 3. Update `README.md`

Add the new language to the **Installation** → **Language:** list, preserving the existing alphabetical order by English display name (e.g. `French` between `English (default)` and `German`). Match the existing two-space indentation under the `- Language:` bullet.

## Checklist

- [ ] Created `Strings.{lc}.resx` with all keys translated
- [ ] Added language to the `SupportedLanguage` enum
- [ ] Added ISO code to `GetCurrentLanguage()`
- [ ] Added culture to `GetDefaultCultureInfo()`
- [ ] Added font to `GetFontName()`
- [ ] Added full-width flag to `IsFullWidth()`
- [ ] Added the language to the Installation language list in `README.md`
- [ ] Built in Visual Studio and verified UI with OS language set to the target language
