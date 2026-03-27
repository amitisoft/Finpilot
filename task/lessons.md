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
