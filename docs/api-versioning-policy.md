# API Versioning Policy

## Breaking changes
- Removing or renaming fields
- Changing response status codes
- Tightening validation
- Changing default ordering
- Removing or changing required fields

## Additive changes
- Adding a new optional field
- Adding a new endpoint
- Adding an optional query parameter
- Adding a new response metadata field

## Sunset window
- V1 remains available at least 6 months after V2 ships.
- The team will announce the sunset date in headers, changelog, email, and calendar invite.

## Communication
- All V1 responses include `Deprecation`, `Sunset`, and `Link`.
- A CHANGELOG entry is published for every version release.
- Every team holding an API key receives email notification.
- A calendar invite is sent for the shutdown date.

## Skipping versions
- Clients may migrate directly from V1 to V3.
- They are not required to move through every intermediate version.