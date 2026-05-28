# OrgTrack — Frontend Client

The frontend for **OrgTrack**, a national organization management & tracking platform. Built with Vue 3, TypeScript, and Vite.

## Tech Stack

| Technology | Purpose |
|---|---|
| **Vue 3** (Composition API + `<script setup>`) | UI framework |
| **TypeScript** | Type safety |
| **Vite** | Build tool & dev server |
| **Tailwind CSS v4** | Utility-first styling |
| **Pinia** | State management |
| **Vue Router** | Client-side routing |
| **Axios** | HTTP client with JWT interceptors |
| **Chart.js** + **vue-chartjs** | Analytics charts |
| **Vue Flow** | Interactive org tree visualization |
| **Lucide Icons** | Icon system |
| **VueUse** | Composable utilities (dark mode, etc.) |

## Project Structure

```
src/
├── api/                    # Axios instance & service modules
│   ├── axios.ts            # Interceptors (JWT refresh, 403 handling)
│   └── services/           # API service files (organization, tasks, events, analytics, invite)
├── components/
│   ├── layout/             # AppLayout, Header, Sidebar
│   └── unit/               # UnitMembersTab, KanbanBoard, EventsTab, AnalyticsTab
├── router/                 # Vue Router config with auth guards
├── stores/                 # Pinia stores (auth, org, toast)
├── types/                  # TypeScript interfaces (auth, organization, unit)
└── views/                  # Page-level components
    ├── HomeView.vue         # Dashboard with upcoming events, deadlines, my units
    ├── OrganizationView.vue # Interactive org tree (Vue Flow)
    ├── UnitDashboardView.vue# Unit detail with tabs (Members, Kanban, Events, Analytics)
    ├── NationalDashboardView.vue # Aggregate stats for national-level users
    ├── ProfileView.vue      # Current user's profile
    ├── UserProfileView.vue  # Public profile of any member
    ├── MyEventsView.vue     # Personal event calendar with RSVP
    ├── MyTasksView.vue      # Personal task overview
    ├── LoginView.vue        # Google OAuth login
    ├── DevLoginView.vue     # Developer login (dev environment only)
    └── InviteView.vue       # Invite link acceptance flow
```

## Key Features

- **Google OAuth Login** — Sign in with Google, automatic profile picture sync
- **Live Search** — Debounced member search in the header with avatar dropdown
- **Interactive Org Tree** — Zoomable, draggable visualization of the full hierarchy
- **Kanban Boards** — Drag-and-drop task management per unit with sub-tasks
- **Event Management** — Create events, RSVP, calendar view with month navigation
- **Analytics Dashboards** — Activity scores, bar charts, leaderboards per unit
- **National Dashboard** — Aggregated stats across all descendant units
- **Member Profiles** — View any member's profile, email, roles & units
- **Invite Links** — Generate time-limited invite links for onboarding
- **Role-Based UI** — Sidebar links, tabs, and actions adapt to user permissions
- **Dark/Light Mode** — Toggle with persistent preference
- **Toast Notifications** — Inline feedback for user actions

## Getting Started

### Prerequisites
- Node.js **v18+**
- Backend API running (see root README)

### Setup

```bash
# Install dependencies
npm install

# Create environment file
cp .env.example .env
# Edit .env with your API URL and Google Client ID

# Start dev server
npm run dev
```

### Available Scripts

| Script | Description |
|---|---|
| `npm run dev` | Start Vite dev server with HMR |
| `npm run build` | Type-check with `vue-tsc` then production build |
| `npm run preview` | Preview production build locally |

### Environment Variables

| Variable | Description | Example |
|---|---|---|
| `VITE_API_URL` | Backend API base URL | `http://localhost:5106/api` |
| `VITE_GOOGLE_CLIENT_ID` | Google OAuth 2.0 Client ID | `xxxxx.apps.googleusercontent.com` |

## Authentication Flow

1. User clicks "Sign in with Google" → Google Identity Services SDK
2. Google returns an `idToken` → sent to `POST /api/auth/google`
3. Backend validates token, creates/updates user, returns JWT + Refresh Token
4. Axios interceptor attaches JWT to every request
5. On 401, interceptor silently refreshes the token and retries the request
6. On refresh failure, user is redirected to `/login`

---

*Part of the OrgTrack Bachelor's Degree Project*
