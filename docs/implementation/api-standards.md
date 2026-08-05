# API Standards

**Document Status:** Draft  
**Version:** 1.0  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-02

---

# 1. Purpose

This document defines the standards for designing and implementing the DiaperScout HTTP API.

The API represents the primary interface to the DiaperScout platform.

All clients communicate through this API.

The objective is to provide APIs that are:

- Consistent
- Predictable
- Discoverable
- Secure
- Versionable
- Easy to consume

---

# 2. API Philosophy

The API exposes platform capabilities rather than database tables.

Endpoints should represent user intentions.

Good examples:

- Submit Review
- Search Products
- Create Observation
- Approve Observation

Poor examples:

- Insert Review
- Update ProductTable
- Delete Row

The API should communicate in the language of the domain.

---

# 3. API Style

DiaperScout uses a RESTful HTTP API.

Standard HTTP methods should be used consistently.

| Method | Purpose |
|---------|----------|
| GET | Retrieve information |
| POST | Create resources or perform actions |
| PUT | Replace resources |
| PATCH | Partially update resources |
| DELETE | Remove resources |

HTTP status codes should accurately reflect the outcome of requests.

---

# 4. API Versioning

Public APIs should be versioned.

Initial release:

```
/api/v1/
```

Future breaking changes should result in a new API version.

Existing versions should remain supported for an appropriate migration period.

---

# 5. JSON

JSON is the standard request and response format.

Naming conventions should follow standard ASP.NET Core JSON serialization.

Responses should be predictable and consistent across the platform.

---

# 6. Resource Design

Resources should represent domain concepts.

Examples include:

- Products
- Reviews
- Observations
- Scouts
- Manufacturers
- Brands

Nested resources should only be used where relationships are obvious.

Example:

```
GET /products/{id}/reviews
```

---

# 7. Searching

Searching is considered a first-class platform capability.

Search endpoints should support:

- Partial matching
- Typo tolerance
- Ranking
- Pagination

A failed search should not be considered an error.

Where appropriate, the API should support workflows that encourage users to create new products when none are found.

---

# 8. Pagination

Collection endpoints should always support pagination.

Responses should include:

- Current page
- Page size
- Total results
- Total pages

Large datasets should never be returned without explicit limits.

---

# 9. Filtering

Filtering should use query parameters.

Example:

```
GET /products?brand=BetterDry
```

Filters should remain simple and predictable.

---

# 10. Sorting

Sorting should be explicit.

Example:

```
GET /products?sort=name
```

Supported sort options should be documented for each endpoint.

---

# 11. Authentication

Anonymous users should be able to access public information.

Authentication is required for actions such as:

- Creating products
- Submitting reviews
- Uploading images
- Creating observations
- Voting
- Moderation

Authentication uses passwordless identity through:

- Email magic links
- Passkeys

---

# 12. Authorisation

Authentication identifies the user.

Authorisation determines what the user may do.

Typical roles include:

- Anonymous
- Scout
- Moderator
- Administrator

Role checks should occur within the application layer rather than presentation components.

---

# 13. Validation

Validation should occur before application workflows execute.

Validation includes:

- Required fields
- Invalid formats
- Invalid ranges
- Business rule violations

Validation failures should produce clear, user-friendly error responses.

---

# 14. Error Responses

Error responses should be:

- Consistent
- Machine readable
- Human understandable

Internal implementation details should never be exposed.

Unexpected exceptions should be logged rather than returned to clients.

---

# 15. Performance

Endpoints should be designed with performance in mind.

Avoid:

- Returning unnecessary data
- Excessive database queries
- Large payloads

Support efficient client interactions wherever possible.

---

# 16. Idempotency

Operations should behave predictably when repeated.

Where appropriate, repeated requests should not produce duplicate side effects.

This is particularly important for:

- Product creation
- Observation submission
- Review submission

---

# 17. Documentation

Public endpoints should be automatically documented using OpenAPI.

Documentation should remain synchronized with the implementation.

Interactive API exploration should be available during development.

---

# 18. Future Clients

The API should remain client-neutral.

It should not contain behaviour specific to:

- Blazor
- iOS
- Android

Every client should interact with the same platform capabilities.

---

# 19. Design Philosophy

The API is not simply a transport mechanism.

It is the public contract of the DiaperScout platform.

Changes to the API should be considered carefully, prioritising consistency, backwards compatibility and clarity.

A well-designed API allows new clients and new features to be introduced without requiring architectural redesign.