# Feature Specification: Ranked Choice Voting Web Application

**Feature Branch**: `001-web-app`
**Created**: 2025-10-26
**Status**: Draft
**Input**: User description: "let's make a spec for just the web app portion based on the information in our prd: @tasks/prd-v1.md ultrathink"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - User Authentication (Priority: P1)

Users can authenticate to the platform using their existing Slack, Teams, Google, Apple, or Microsoft accounts without creating a new password-based account.

**Why this priority**: Authentication is foundational - no other features can work without secure user identity. This enables single vote per user enforcement and poll creator privileges.

**Independent Test**: Can be fully tested by attempting to sign in with each supported provider and verifying successful authentication delivers a user session that persists across page refreshes.

**Acceptance Scenarios**:

1. **Given** a user visits the web app for the first time, **When** they click "Sign in with Slack", **Then** they are redirected to Slack OAuth, authorize the app, and return authenticated with their Slack identity
2. **Given** a user visits the web app, **When** they click "Sign in with Teams", **Then** they are redirected to Microsoft OAuth, authorize the app, and return authenticated with their Teams identity
3. **Given** a user visits the web app, **When** they click "Sign in with Google", **Then** they are redirected to Google OAuth, authorize the app, and return authenticated
4. **Given** a user visits the web app, **When** they click "Sign in with Apple", **Then** they are redirected to Apple OAuth, authorize the app, and return authenticated
5. **Given** a user visits the web app, **When** they click "Sign in with Microsoft", **Then** they are redirected to Microsoft OAuth, authorize the app, and return authenticated
6. **Given** a user is signed in, **When** they close the browser and return within 7 days, **Then** they remain signed in
7. **Given** a user is signed in, **When** they click "Sign out", **Then** their session ends and they are redirected to the login page

---

### User Story 2 - Create Poll (Priority: P1)

Poll creators can launch a new ranked choice poll by providing a title, description, list of options, and voting duration.

**Why this priority**: Poll creation is the core action that enables all downstream voting and results viewing. Without this, there's nothing for users to interact with.

**Independent Test**: Can be fully tested by creating a poll with various option counts and durations, then verifying the poll is saved and accessible via a unique URL.

**Acceptance Scenarios**:

1. **Given** an authenticated user on the home page, **When** they click "Create New Poll", **Then** they see a poll creation form with fields for title, description, options, and duration
2. **Given** a user on the poll creation form, **When** they enter a poll title "Best Campaign Strategy", add options "Strategy A", "Strategy B", "Strategy C", set duration to 7 days, and set description "Vote on our next campaign", **Then** the poll is created and they receive a unique poll URL
3. **Given** a user creating a poll, **When** they attempt to submit with fewer than 2 options, **Then** they see an error message "A poll must have at least 2 options"
4. **Given** a user creating a poll, **When** they attempt to submit without a title, **Then** they see an error message "Poll title is required"
5. **Given** a poll creator, **When** they create a poll and select "Results visible during voting", **Then** the poll is created with live results enabled
6. **Given** a poll creator, **When** they create a poll and select "Votes are anonymous", **Then** the poll is created with anonymous voting enabled
7. **Given** a poll creator, **When** they create a poll and select "Results visible only after poll closes", **Then** voters cannot see results until the poll end time

---

### User Story 3 - Vote in Poll (Priority: P1)

Voters can rank poll options in order of preference using an intuitive ranking interface, with the system enforcing one vote per user.

**Why this priority**: Voting is the primary user interaction and the reason for the platform's existence. This must work reliably for the MVP to deliver value.

**Independent Test**: Can be fully tested by accessing a poll URL, authenticating, ranking options, and verifying the vote is recorded and the user cannot vote again.

**Acceptance Scenarios**:

1. **Given** an unauthenticated user receives a poll link, **When** they click the link, **Then** they see the poll details and a "Sign in to Vote" button
2. **Given** an authenticated user on a poll page, **When** they see the list of options, **Then** they can rank them by selecting their 1st choice, 2nd choice, 3rd choice, etc.
3. **Given** a user ranking options, **When** they submit their ranked ballot with all options ordered, **Then** they see a confirmation message "Your vote has been recorded"
4. **Given** a user ranking options, **When** they submit a partial ballot (not all options ranked), **Then** their vote is still accepted with only their ranked choices counted
5. **Given** a user who has already voted, **When** they return to the poll page, **Then** they see a message "You have already voted in this poll" and cannot vote again
6. **Given** a user on the voting page, **When** they attempt to submit without ranking at least one option, **Then** they see an error message "Please rank at least one option"
7. **Given** a user on the voting page, **When** they attempt to select the same option as both 1st and 2nd choice, **Then** the system prevents duplicate rankings

---

### User Story 4 - View Results (Priority: P1)

Users can view poll results showing the round-by-round elimination process, final winner, and participation statistics.

**Why this priority**: Results viewing is the payoff for participation - it closes the loop and demonstrates the value of RCV. Without this, users don't see the outcome of their votes.

**Independent Test**: Can be fully tested by creating a poll, casting multiple votes with different rankings, closing the poll, and verifying the results page correctly displays the winner, elimination rounds, and statistics.

**Acceptance Scenarios**:

1. **Given** a poll has closed (end time reached), **When** a user visits the poll page, **Then** they see the full results page with winner announcement
2. **Given** a user on a closed poll results page, **When** they view the results, **Then** they see a round-by-round breakdown showing which option was eliminated in each round
3. **Given** a user viewing results, **When** they see the final results, **Then** they see participation statistics: total votes cast and percentage of invited users who voted
4. **Given** a poll ends in a tie, **When** a user views the results, **Then** they see all tied options clearly marked as "Tied for Winner"
5. **Given** a poll with live results enabled, **When** a user visits the poll page during voting, **Then** they see current vote totals and rankings
6. **Given** a poll with live results disabled, **When** a user tries to view results before the poll closes, **Then** they see a message "Results will be available when voting ends on [date]"
7. **Given** a poll creator viewing results of their own poll, **When** votes are set to public, **Then** they can see who voted for each option
8. **Given** a poll creator viewing results of their own poll, **When** votes are set to anonymous, **Then** they see only aggregate statistics without voter identities

---

### User Story 5 - Manage Polls (Priority: P2)

Poll creators can manage their polls by ending them early, deleting them, or editing poll settings.

**Why this priority**: Poll management features enhance creator control but are not essential for the core voting workflow. The platform is still useful without these.

**Independent Test**: Can be fully tested by creating a poll, performing management actions (end early, delete), and verifying the effects are applied correctly.

**Acceptance Scenarios**:

1. **Given** a poll creator viewing their active poll, **When** they click "End Poll Now", **Then** the poll closes immediately and results become visible
2. **Given** a poll creator viewing their poll list, **When** they select a poll and click "Delete", **Then** they see a confirmation dialog "Are you sure? This cannot be undone"
3. **Given** a poll creator confirming deletion, **When** they click "Yes, Delete", **Then** the poll is permanently removed and all associated votes are deleted
4. **Given** a poll creator viewing their active poll, **When** they attempt to change the poll title or options, **Then** they see a message "Poll details cannot be changed after creation"
5. **Given** a poll creator viewing their active poll, **When** they change the poll from "private votes" to "public votes", **Then** voter identities become visible in the results
6. **Given** a non-creator user viewing a poll they didn't create, **When** they look for management options, **Then** they see only "View Results" and "Vote" options, not "End" or "Delete"

---

### User Story 6 - Poll Dashboard (Priority: P2)

Users can view a dashboard listing all polls they created or participated in, with quick access to active and past polls.

**Why this priority**: A dashboard improves organization and navigation for active users but isn't required for single-poll participation. Users can still access polls via direct links.

**Independent Test**: Can be fully tested by creating multiple polls, voting in others, then verifying the dashboard displays all relevant polls with correct status indicators.

**Acceptance Scenarios**:

1. **Given** an authenticated user, **When** they click "My Polls" in the navigation, **Then** they see a dashboard with two sections: "Polls I Created" and "Polls I Voted In"
2. **Given** a user viewing their dashboard, **When** they see the "Polls I Created" section, **Then** each poll shows: title, status (Active/Closed), end date, and vote count
3. **Given** a user viewing their dashboard, **When** they see the "Polls I Voted In" section, **Then** each poll shows: title, status, their vote timestamp, and whether results are available
4. **Given** a user on their dashboard, **When** they click on an active poll they created, **Then** they navigate to the poll management page
5. **Given** a user on their dashboard, **When** they click on a closed poll, **Then** they navigate to the results page
6. **Given** a user with no polls, **When** they visit their dashboard, **Then** they see a helpful message "You haven't created any polls yet" with a "Create Your First Poll" button

---

### User Story 7 - First-Time User Experience (Priority: P3)

First-time users see a welcoming introduction that explains how the platform works and guides them to their first action.

**Why this priority**: Onboarding improves adoption and reduces confusion, but experienced users or those arriving via poll links don't need it. The platform is self-explanatory enough to work without this.

**Independent Test**: Can be fully tested by creating a new user account and verifying the welcome flow appears once and provides helpful context.

**Acceptance Scenarios**:

1. **Given** a user signs in for the first time, **When** they reach the home page, **Then** they see a welcome modal with a brief introduction: "Welcome to Ranked Choice Voting! Create polls, vote on decisions, and see transparent results."
2. **Given** a user viewing the welcome modal, **When** they click "Get Started", **Then** the modal closes and they see prominent buttons for "Create a Poll" and "View Sample Poll"
3. **Given** a user has seen the welcome modal once, **When** they sign in again, **Then** the welcome modal does not appear
4. **Given** a user arrives via a direct poll link, **When** they authenticate and land on the poll page, **Then** the welcome modal does not interrupt their voting experience

---

### Edge Cases

- **What happens when a user's OAuth provider becomes unavailable?** System displays an error message "Unable to connect to [Provider]. Please try again later or use a different sign-in method."
- **How does the system handle concurrent poll edits?** Only the poll creator has edit permissions, so concurrent edits from the same user are processed in order; concurrent views by different users show real-time updates.
- **What happens when a poll creator deletes a poll while users are actively voting?** Active voters see a message "This poll has been deleted by the creator" when they attempt to submit their vote.
- **How does the system handle ties in RCV elimination?** When multiple options tie for last place in a round, one is randomly selected for elimination (using the RCV NuGet package's tie-breaking logic).
- **What happens when a user tries to vote after a poll has closed?** They see the poll page with a message "Voting has ended on [date]" and a "View Results" button.
- **How does the system handle extremely long poll titles or option names?** UI truncates display text after 100 characters with "..." and shows full text on hover/focus.
- **What happens when a user loses internet connection while voting?** The form retains their selections locally; when connection restores and they submit, the vote is sent to the server. If submission fails, they see a "Connection lost - please try again" error with their selections preserved.
- **How does the system handle expired authentication sessions?** When a user's session expires (after 7 days of inactivity), they are redirected to sign in again; after re-authenticating, they return to the page they were on.
- **What happens when a user clears cookies or switches browsers?** They need to sign in again on the new browser/session, but their vote history is preserved server-side and visible after re-authentication.
- **How does the system handle polls with only one option?** Poll creation form requires minimum 2 options; attempting to create with 1 option shows error "A poll must have at least 2 options."
- **What happens when a poll receives zero votes?** Results page shows "No votes were cast in this poll" with participation statistics showing 0%.

## Requirements *(mandatory)*

### Functional Requirements

#### Authentication & User Management

- **FR-001**: System MUST support OAuth 2.0 authentication with five providers: Slack, Microsoft Teams, Google, Apple, and Microsoft (personal accounts)
- **FR-002**: System MUST NOT implement password-based authentication or user account creation forms
- **FR-003**: System MUST create a user record on first successful OAuth authentication, storing minimal data: provider ID, provider name, display name, and email
- **FR-004**: System MUST maintain user sessions for 7 days of inactivity, requiring re-authentication after expiration
- **FR-005**: System MUST map user identity by provider-specific user ID to enforce one vote per user per poll
- **FR-006**: System MUST provide a sign-out function that terminates the user session and clears authentication cookies
- **FR-007**: System MUST handle OAuth provider errors gracefully by displaying user-friendly error messages and offering alternative sign-in methods

#### Poll Creation

- **FR-008**: System MUST allow authenticated users to create new polls with the following fields: title (required, max 200 characters), description (optional, max 1000 characters), list of options (minimum 2, maximum 20), and voting end date/time (required)
- **FR-009**: System MUST validate poll creation input: title is not empty, at least 2 options are provided, end date is in the future, and option names are unique within a poll
- **FR-010**: System MUST assign each new poll a unique identifier and generate a shareable URL
- **FR-011**: System MUST allow poll creators to configure visibility settings: whether results are visible during voting (live results) or only after poll closes
- **FR-012**: System MUST allow poll creators to configure vote privacy: whether individual votes are anonymous or publicly visible
- **FR-013**: System MUST record the creating user as the poll owner with special management privileges
- **FR-014**: System MUST NOT allow modification of poll title, options, or end date after poll creation
- **FR-015**: System MUST display a confirmation message and unique poll URL immediately after successful poll creation

#### Voting

- **FR-016**: System MUST allow authenticated users to rank poll options in order of preference (1st choice, 2nd choice, 3rd choice, etc.)
- **FR-017**: System MUST support partial ballots where users rank only a subset of available options
- **FR-018**: System MUST prevent users from ranking the same option multiple times in a single ballot
- **FR-019**: System MUST prevent users from submitting an empty ballot (at least one option must be ranked)
- **FR-020**: System MUST enforce one vote per user per poll, identified by user's provider-specific ID
- **FR-021**: System MUST reject duplicate vote attempts from the same user with a clear message "You have already voted in this poll"
- **FR-022**: System MUST prevent voting after a poll's end date/time has passed
- **FR-023**: System MUST display a confirmation message immediately after successful vote submission
- **FR-024**: System MUST persist votes immediately upon submission to prevent data loss
- **FR-025**: System MUST validate that all ranked option IDs exist in the poll before accepting a vote

#### Results & Statistics

- **FR-026**: System MUST calculate poll results using the Rcv.Core NuGet package's instant-runoff voting algorithm
- **FR-027**: System MUST display poll results only to authenticated users who have voted or to the poll creator
- **FR-028**: System MUST display results according to poll visibility settings: immediately during voting if live results enabled, or only after poll closes if disabled
- **FR-029**: System MUST display the winning option prominently, or clearly indicate a tie if multiple options tie for the win
- **FR-030**: System MUST display round-by-round elimination data showing which option was eliminated in each round and vote transfers
- **FR-031**: System MUST display participation statistics: total votes cast, total invited users (if invitation feature exists), participation percentage
- **FR-032**: System MUST display per-option statistics: first-choice vote count, vote count in each elimination round
- **FR-033**: System MUST display individual voter identities and their votes if poll is configured with public votes AND viewer is the poll creator
- **FR-034**: System MUST NOT display individual voter identities if poll is configured with anonymous votes
- **FR-035**: System MUST display "Results will be available when voting ends on [date]" message when live results are disabled and poll is still active

#### Poll Management

- **FR-036**: System MUST allow poll creators to end their polls early, immediately closing voting and making results visible
- **FR-037**: System MUST allow poll creators to delete their polls permanently, removing all poll data and associated votes
- **FR-038**: System MUST display a confirmation dialog before poll deletion: "Are you sure? This cannot be undone"
- **FR-039**: System MUST allow poll creators to change vote privacy settings (anonymous ↔ public) at any time
- **FR-040**: System MUST allow poll creators to change results visibility settings (live ↔ post-close) at any time
- **FR-041**: System MUST NOT allow non-creators to perform management actions on polls they didn't create
- **FR-042**: System MUST display management options (End Early, Delete, Change Settings) only to poll creators when viewing their own polls

#### User Dashboard

- **FR-043**: System MUST provide a dashboard page listing all polls created by the authenticated user
- **FR-044**: System MUST provide a dashboard page listing all polls the authenticated user has voted in
- **FR-045**: System MUST display for each poll in the dashboard: title, status (Active/Closed), creation date, end date, vote count
- **FR-046**: System MUST allow users to navigate from the dashboard to poll details, voting, or results pages
- **FR-047**: System MUST display polls in reverse chronological order (most recent first)
- **FR-048**: System MUST indicate which polls have new results available since the user last viewed them

#### Accessibility & UX

- **FR-049**: System MUST be fully navigable using keyboard only (no mouse required)
- **FR-050**: System MUST meet WCAG 2.2 Level AA accessibility requirements for all text and interactive elements
- **FR-051**: System MUST provide ARIA labels and semantic HTML for screen reader compatibility
- **FR-052**: System MUST be responsive and functional on mobile devices (minimum screen width: 320px)
- **FR-053**: System MUST display a welcome modal to first-time users explaining the platform's purpose
- **FR-054**: System MUST NOT show the welcome modal to users who have seen it before or who arrive via direct poll links
- **FR-055**: System MUST display clear error messages when OAuth authentication fails, form validation fails, or server errors occur
- **FR-056**: System MUST provide a retry mechanism when transient errors occur (network failures, server timeouts)
- **FR-057**: System MUST preserve user input in forms when validation errors occur (don't clear the form)

#### Data Persistence

- **FR-058**: System MUST persist all polls, votes, and user records in Azure SQL Database
- **FR-059**: System MUST encrypt sensitive data at rest and in transit
- **FR-060**: System MUST perform database operations using Entity Framework Core
- **FR-061**: System MUST handle database connection failures gracefully with user-friendly error messages
- **FR-062**: System MUST implement database transactions for vote submission to ensure atomic operations
- **FR-063**: System MUST log all failed database operations for debugging and monitoring

### Key Entities

- **User**: Represents an authenticated platform user. Key attributes: unique ID, OAuth provider name (Slack/Teams/Google/Apple/Microsoft), provider-specific user ID, display name, email address, first sign-in date, last sign-in date.

- **Poll**: Represents a ranked choice voting poll. Key attributes: unique ID, title, description, creator (relationship to User), creation timestamp, end timestamp, list of options (relationship to Option), visibility settings (live results enabled/disabled), vote privacy settings (public/anonymous), status (Active/Closed).

- **Option**: Represents a single choice in a poll. Key attributes: unique ID, label/name, display order, poll (relationship to Poll).

- **Vote**: Represents a single user's ballot in a poll. Key attributes: unique ID, poll (relationship to Poll), voter (relationship to User), ranked option IDs (ordered list of Option IDs), submission timestamp.

- **RcvResult**: Represents calculated poll results (generated by Rcv.Core NuGet package, may be cached). Key attributes: poll (relationship to Poll), winner (relationship to Option, nullable for ties), list of tied options (for ties), round-by-round elimination data (list of RoundSummary objects), final vote totals by option, calculation timestamp.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete account creation (OAuth authentication) in under 30 seconds from clicking a sign-in button
- **SC-002**: Users can create a new poll in under 2 minutes, including entering title, description, options, and settings
- **SC-003**: Users can cast their vote in under 1 minute from landing on the poll page
- **SC-004**: Poll results are displayed within 5 seconds of poll closure or page load
- **SC-005**: The platform handles 1,000 concurrent users voting simultaneously without errors or performance degradation
- **SC-006**: The platform achieves 99.9% uptime measured over a 30-day period
- **SC-007**: 90% of users successfully complete their intended task (create poll, vote, view results) on first attempt without errors
- **SC-008**: Average page load time is under 2 seconds on a standard broadband connection (10 Mbps)
- **SC-009**: The platform passes WCAG 2.1 Level AA accessibility audit with zero critical violations
- **SC-010**: Mobile users (screen width < 768px) can complete all tasks with the same success rate as desktop users
- **SC-011**: The platform supports Safari, Chrome, Firefox, and Edge browsers (latest 2 versions each) without functional differences
- **SC-012**: Zero votes are lost or corrupted due to system errors (verified through database integrity checks)
- **SC-013**: Users rate the platform's ease of use at 4.0/5.0 or higher in post-task surveys
- **SC-014**: Poll creators successfully share poll URLs and receive votes from invited participants in 80% of polls created
- **SC-015**: The platform correctly calculates RCV results matching the Rcv.Core NuGet package output in 100% of test cases

## Assumptions

1. **Authentication Providers**: All five OAuth providers (Slack, Teams, Google, Apple, Microsoft) have stable APIs and will not introduce breaking changes during development.

2. **User Behavior**: Users will primarily access the platform via desktop or mobile web browsers, not expecting native mobile apps at launch.

3. **Poll Scale**: Typical polls will have 2-10 options and receive 10-100 votes. The system should optimize for this common case while supporting the specified maximums (20 options, 1000 concurrent users).

4. **Network Reliability**: Users have reasonably stable internet connections. Brief disconnections are handled, but the platform is not designed for fully offline operation.

5. **Browser Compatibility**: Users have modern browsers (latest 2 versions of major browsers). No support for Internet Explorer or browsers older than 2 years.

6. **Poll Sharing**: Poll creators will share poll URLs via their preferred communication channels (email, Slack, Teams, etc.). The platform provides the URL but does not implement built-in sharing or invitation mechanisms in this phase.

7. **Data Retention**: Polls and votes are retained indefinitely unless explicitly deleted by the creator. No automatic archival or purging is implemented in this phase.

8. **Email Notifications**: No email notifications are sent by the platform (e.g., "poll closing soon", "new votes received"). Users must manually check for updates.

9. **Multi-Tenancy**: The platform is a shared environment where all users can create polls visible to anyone with the link. No organization-level isolation or private workspaces are implemented in this phase.

10. **Localization**: The platform is English-only at launch. No internationalization (i18n) or translation features are implemented.

11. **Payment/Pricing**: The platform is free to use with no payment or subscription features. No rate limiting on poll creation or voting is implemented.

12. **User Profiles**: User profiles are minimal (name, email from OAuth provider only). No editable profiles, avatars, or bio fields are implemented.

13. **Poll Templates**: No predefined poll templates or quick-start wizards are provided. Users create polls from scratch each time.

14. **Analytics**: Only basic participation statistics are displayed (vote count, participation rate). No advanced analytics (voting patterns, demographic breakdowns, time-series charts) are implemented in this phase.

15. **Azure Resources**: Azure SQL Database (Serverless tier) provides sufficient capacity and performance for the expected user base. Auto-scaling is handled by Azure platform.
