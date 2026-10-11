# Copilot Instructions

## Project Guidelines
- Use the current Google Health v4 REST endpoint https://health.googleapis.com/v4/users/me/dataTypes/steps/dataPoints:dailyRollUp; daily step responses provide steps.countSum values. Do not use a configurable or invented /dailySteps endpoint.
- When external API metadata retrieval is blocked or repeatedly canceled, stop retrying that approach and use the concrete provider error response plus local request construction to diagnose and implement the fix.
- Keep README.md current: whenever a change adds, removes, or alters a user-visible feature, page/route, service, configuration key, environment variable, migration/database behavior, or test coverage area, update the matching README section (Features, Architecture, Configuration, Testing) in the same change.
- All settings, configuration, and data must be user-scoped (multi-user app); never store per-user preferences globally, and add isolation tests for new account-backed data.
- Public SEO surfaces (/blog, /reviews, /fitness-tracker) must stay in `/sitemap.xml`; new public pages need canonical metadata, a sitemap entry, and a robots.txt rule.