# Copilot Instructions

## Project Guidelines
- Use the current Google Health v4 REST endpoint https://health.googleapis.com/v4/users/me/dataTypes/steps/dataPoints:dailyRollUp; daily step responses provide steps.countSum values. Do not use a configurable or invented /dailySteps endpoint.
- When external API metadata retrieval is blocked or repeatedly canceled, stop retrying that approach and use the concrete provider error response plus local request construction to diagnose and implement the fix.