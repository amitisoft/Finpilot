# Lessons

- After frontend UX changes, verify the deployed shell behavior in the browser-style layout, not just the build. Ensure only the main content pane scrolls while navigation/account chrome stays fixed.
- Avoid overcrowding the dashboard with repeated empty-state cards. For a new or low-data user, the first page should stay concise and action-oriented.
- Controlled numeric fields should not bind raw Number(e.target.value) directly when the UI needs editable/clearable values. Preserve the display string during editing so defaults like 0 do not stick or prepend to user input.
- When a backend rule intentionally rejects invalid financial data, surface that rule clearly in the UI before submit. Do not leave the user with a generic Bad Request for a preventable validation case like budget allocations exceeding the total limit.
- For date-like financial inputs such as budget month, prefer semantic choices like Jan-Dec instead of raw numeric fields. It reduces user error and makes it obvious which period the budget will track.
- Chat-style pages should be built as full-height workspaces with localized scrolling. Do not make the whole page scroll when the real intent is for only chat history or side rails to scroll.
- For assistant-style surfaces, prioritize one clear conversation pane and move prompts/results into a right rail. Exposing too many context selectors inside the chat area makes the product feel like a form instead of a chatbot.

- On assistant pages, do not leave hidden or secondary context controls visually competing with the conversation. If the user asks for a chatbot experience, keep the main pane to messages plus composer and move only lightweight prompt help to the side rail.

- Do not introduce paid external AI providers or assume key-based integrations are acceptable without explicit user approval. If the user wants a self-contained or hackathon-safe setup, stay local/deterministic unless they opt in.
- For hackathon deployments, do not keep optional infrastructure like Redis as a hard dependency unless the user explicitly wants that extra service. Prefer simpler in-memory fallbacks first.
- The dashboard should stay a concise command center. Do not embed large empty-state sections for accounts, budgets, goals, or other features when dedicated pages already exist; show brief summaries and route users into the relevant page instead.

- Segmented controls like Login/Register should be built with nowrap-safe sizing and explicit flex behavior. Do not let auth mode toggles wrap or collapse under narrower content widths.

- When the user asks for SCSS-heavy cleanup, move touched pages/components to semantic BEM-style classes and avoid introducing new Tailwind utility markup in the implementation slice.

- When migrating pages away from utility styling, add dedicated page-level SCSS partials and import them immediately in index.scss to keep the build green while the migration is in progress.

- Avoid return-state phrasing like 'Welcome back' when the UI cannot actually know if the user is returning; prefer neutral financial-workspace copy.

- When extending the design system, move core finance CRUD pages onto shared page-intro/button/form-field patterns instead of leaving them on mixed old utility markup.

- For dense finance workspaces, keep desktop navigation as a compact icon rail with hover labels and move account actions into an avatar menu. Avoid permanently occupying large sidebar space with marketing copy when the user asks for a minimal full-width layout.

- When an analytics page grows taller than the viewport with secondary charts and report bundles, keep the top of the page reserved for summary decisions and move deep detail into reusable modal or drawer views launched from a short list of cards.

- When a viewport-bounded shell is requested, use explicit height: 100vh plus child height: 100% and min-height: 0 on flex containers. Using only min-height in the shell can let side rails visually extend past the screen.

- Raw serialized JSON should not be exposed directly in user-facing audit history. Prefer a readable field list and, when both old and new states exist, simple tabs or toggles instead of raw payload dumps.

- When the user provides a generated design mock or HTML/Tailwind prototype, translate its interaction and hierarchy into the app's SCSS/BEM component system instead of copying utility-heavy markup directly.
