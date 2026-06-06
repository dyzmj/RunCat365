---
name: add-localization
description: Adds a new language locale to RunCat365. Use when adding a new language, translating the UI, creating a .resx resource file, or updating SupportedLanguage.cs with a new language code.
argument-hint: <language name or BCP 47 code>
---

# Add a New Localization to RunCat365

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

Add the language to the `SupportedLanguage` enum:

```csharp
enum SupportedLanguage
{
    English,
    Japanese,
    Spanish,
    French,
}
```

Add the ISO code to `GetCurrentLanguage()`:

```csharp
return culture.TwoLetterISOLanguageName switch
{
    "ja" => SupportedLanguage.Japanese,
    "es" => SupportedLanguage.Spanish,
    "fr" => SupportedLanguage.French,
    _ => SupportedLanguage.English,
};
```

Add the culture to `GetDefaultCultureInfo()`:

```csharp
return language switch
{
    SupportedLanguage.Japanese => new CultureInfo("ja-JP"),
    SupportedLanguage.Spanish => new CultureInfo("es-ES"),
    SupportedLanguage.French => new CultureInfo("fr-FR"),
    _ => new CultureInfo("en-US"),
};
```

Add the font to `GetFontName()` — use `"Consolas"` for Latin-script languages:

```csharp
return language switch
{
    SupportedLanguage.Japanese => "Noto Sans JP",
    SupportedLanguage.Spanish => "Consolas",
    SupportedLanguage.French => "Consolas",
    _ => "Consolas",
};
```

Add the full-width flag to `IsFullWidth()` — use `false` for Latin-script languages:

```csharp
return language switch
{
    SupportedLanguage.Japanese => true,
    SupportedLanguage.Spanish => false,
    SupportedLanguage.French => false,
    _ => false,
};
```

### 3. Update `CLAUDE.md`

Update the Localization notes to include the new language:

- Add the new `Strings.{lc}.resx` line under the `**Settings:**` → `Properties/Strings.resx` bullet, keeping the existing alphabetical order by display name
- Update the `**Localization notes:**` bullet for "Add new strings to all N `.resx` files simultaneously" so the count matches the new total
- Add a font line for the new language if it uses a font other than `"Consolas"`; otherwise group it with the existing `"Consolas"` line

### 4. Update `README.md`

Add the new language to the **Installation** → **Language:** list, preserving the existing alphabetical order by English display name (e.g. `French` between `English (default)` and `German`). Match the existing two-space indentation under the `- Language:` bullet.

## Checklist

- [ ] Created `Strings.{lc}.resx` with all keys translated
- [ ] Added language to the `SupportedLanguage` enum
- [ ] Added ISO code to `GetCurrentLanguage()`
- [ ] Added culture to `GetDefaultCultureInfo()`
- [ ] Added font to `GetFontName()`
- [ ] Added full-width flag to `IsFullWidth()`
- [ ] Updated Localization notes in `CLAUDE.md`
- [ ] Added the language to the Installation language list in `README.md`
- [ ] Built in Visual Studio and verified UI with OS language set to the target language
