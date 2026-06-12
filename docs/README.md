# RunCat 365 — GitHub Pages

The project website published at <https://runcat-dev.github.io/RunCat365/>.

It is a static site rendered in the browser by [lobster.js](https://hacknock.github.io/lobsterjs/), a Markdown parser loaded from a CDN. There is no build step, bundler, or dependency to install — editing a Markdown file and pushing is all it takes to update the page.

## Files

| File | Role |
| :--- | :--- |
| `index.html` | Loader shell for the landing page. Picks the content file by language and hands it to lobster.js. |
| `privacy_policy.html` | Loader shell for the privacy policy. Same mechanism as `index.html`. |
| `content.md` / `content.ja.md` | Landing page content (English / Japanese). |
| `privacy_policy.md` / `privacy_policy.ja.md` | Privacy policy content (English / Japanese). |
| `style.css` | Shared stylesheet targeting lobster.js `lbs-*` classes. |
| `images/` | Screenshots, demo GIF, Microsoft Store badge, and Open Graph thumbnail. |

The two `.html` files are thin loaders — almost all content lives in the `.md` files.

## Editing content

Edit the relevant `.md` file directly. lobster.js renders Markdown plus a few block extensions:

- `:::header` … `:::` — page title block
- `:::footer` … `:::` — footer block (links, language switcher, copyright)
- `:::warp <name>` … `:::` — a column referenced from a silent table (`~ | [~name] | …`); used for the feature cards and the two-column "What you can monitor" layout
- `:::details <summary>` … `:::` — collapsible FAQ entry
- `![alt](path =600x)` — image with an explicit width

Any text change is reflected on the next page load — no rebuild required. Keep the English and Japanese files in sync when you change wording or structure.

## Language switching

The language is chosen by the `?lang` query parameter, read in the `<script>` of each `.html` loader:

- no parameter (or anything other than `ja`) → English (`content.md`)
- `?lang=ja` → Japanese (`content.ja.md`)

The footer of each page links between the two. To add a new language:

1. Create `content.<lang>.md` (and `privacy_policy.<lang>.md`).
2. Extend the language-selection logic in `index.html` and `privacy_policy.html` to map the new `?lang` value to that file.
3. Add a link to the new language in every footer block.

## Styling

`style.css` targets the `lbs-*` class names that lobster.js emits (`.lbs-heading-1`, `.lbs-paragraph`, `.lbs-table-silent`, `.lbs-details`, `.lbs-footer`, …). The privacy policy page carries a `privacy-policy` body class so its title can be styled smaller than the landing-page hero.

## Previewing locally

lobster.js fetches the Markdown and its own module over HTTP, so opening the files with `file://` will not work. Serve the directory and open it over `http://`:

```sh
cd docs
python3 -m http.server 8000
# then open http://localhost:8000/index.html
# Japanese: http://localhost:8000/index.html?lang=ja
```

## Deployment

The site is served by GitHub Pages from this `docs/` directory on the default branch. Pushing changes here updates the live site; there is nothing to build or deploy manually.
