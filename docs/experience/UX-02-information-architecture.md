# Information Architecture

**Document ID:** UX-02  
**Phase:** Experience Design  
**Status:** Draft  
**Depends On:** UX-01 Experience Principles

---

# Purpose

The purpose of this document is to define how information is organised throughout DiaperScout.

Rather than reflecting the underlying database structure, the information architecture is designed around the way Scouts naturally think while exploring products and places.

Users should never need to understand the internal data model.

Instead, the application should present a small number of clear destinations that reflect user intent.

---

# Design Philosophy

DiaperScout is organised around questions rather than entities.

People do not think:

> "I want to browse Retailers."

Instead they think:

- "What's happening?"
- "I'm looking for a product."
- "What's this?"
- "Where can I find things?"
- "What have I collected?"

The navigation reflects these intentions.

---

# Mental Model

The application consists of five primary destinations.

```
Explore
Products
Scan
Atlas
Backpack
```

Each destination has a distinct purpose.

There should be as little overlap as possible.

---

# Primary Navigation

## 🌿 Explore

Explore is the living heartbeat of DiaperScout.

It is not a dashboard.

It is not a home page.

Instead it surfaces activity from across the community.

Typical content includes:

- Recent discoveries
- Community observations
- Atlas highlights
- Nearby activity
- Newly added products
- Scout Tasks (authenticated users)
- Seasonal or featured content

Explore should encourage curiosity.

Users should regularly discover something unexpected.

---

## 📦 Products

Products is the primary destination for exploring the product catalogue.

This section contains the tools required to locate and compare products.

Capabilities include:

- Search
- Browse
- Filters
- Categories
- Brands
- Product comparison
- Recently added products
- Recently updated products

Search is considered a capability within Products rather than a separate destination.

Products answer the question:

> "What am I looking for?"

---

## 📷 Scan

Scan is one of DiaperScout's defining interactions.

Selecting Scan immediately opens the barcode scanner.

Barcode scanning performs a product lookup using the scanned barcode.

Possible outcomes are:

### Product Found

Open the existing product page.

### Product Unknown

Transition directly into the "Become the First Scout" discovery workflow.

Scanning is not considered a contribution feature.

It is a product identification feature.

---

## 🗺️ Atlas

Atlas represents geographical exploration.

It is not simply a map.

Atlas allows Scouts to explore places where products have been observed.

Atlas contains multiple views including:

- Interactive map
- Retailer list
- Nearby locations
- Online retailers
- Branch pages

Atlas answers the question:

> "Where can I explore?"

---

## 🎒 Backpack

Backpack represents the Scout's personal expedition.

Rather than functioning purely as a profile page, Backpack contains everything belonging to the Scout.

Examples include:

- Draft observations
- Saved products
- Collections
- Observation history
- Profile
- Settings
- Downloads
- Offline content

Backpack answers:

> "What belongs to me?"

---

# Core Information Model

The information architecture intentionally differs from the database architecture.

Users primarily navigate between:

- Products
- Places
- Their own journey

rather than database entities.

---

## Products

Products represent individual absorbent products.

Products contain:

- Product information
- Gallery
- Reviews
- Observations
- Availability evidence
- History

---

## Observations

Observations are the foundation of DiaperScout.

Every observation represents evidence collected by a Scout.

Observations connect:

- Product
- Location
- Time
- Scout
- Evidence

Many features throughout the application derive from observations.

---

## Retailers

Retailers represent organisations capable of stocking products.

Examples include:

- Supermarkets
- Pharmacies
- Mobility retailers
- ABDL retailers
- Online retailers

Retailers are explored primarily through Atlas.

---

## Locations

Locations represent specific physical branches.

Examples:

- Tesco Extra Yate
- Boots Bristol Broadmead

Observations belong to locations rather than retailers.

This distinction allows different branches of the same retailer to maintain independent histories.

---

# Evidence-Driven Atlas

Atlas is not a business directory.

Atlas is a collection of verified discoveries.

A retailer or location becomes part of the Atlas only after a Scout has documented at least one relevant observation there.

This ensures every place within Atlas has demonstrated relevance to the community.

The Atlas grows organically through exploration.

---

# Evidence Philosophy

DiaperScout distinguishes between observed evidence and inferred information.

The platform does **not** assume:

- Every Tesco stocks the same products.
- Every pharmacy stocks continence products.
- Historical observations represent current availability.

Instead, DiaperScout records what Scouts have actually observed.

For example:

Correct:

> TENA Slip Maxi was observed at Tesco Extra Yate on 3 August 2026.

Incorrect:

> Tesco stocks TENA Slip Maxi.

Future integrations with retailer stock systems may provide additional evidence sources.

These complement community observations rather than replacing them.

---

# Navigation Principles

Every primary destination answers a different user intention.

| Destination | User Question |
|--------------|---------------|
| Explore | What's happening? |
| Products | What am I looking for? |
| Scan | What is this? |
| Atlas | Where can I explore? |
| Backpack | What belongs to me? |

This separation minimises cognitive load while encouraging exploration.

---

# Contextual Experiences

DiaperScout may provide contextual suggestions throughout the application.

Examples include:

- Nearby retailer suggestions
- Continue unfinished observation
- Offline upload reminders
- Recent scans
- Scout Tasks

Context should remain supportive rather than interruptive.

Suggestions should appear as lightweight cards rather than modal dialogs wherever possible.

---

# Future Evolution

The information architecture is intentionally flexible.

Future additions should reinforce existing destinations rather than creating new top-level navigation.

Examples include:

- Retail stock integrations
- Route planning
- Collection challenges
- Personal recommendations
- Advanced search
- Community events

Whenever possible, new functionality should integrate into the existing five-destination model.

---

# Summary

DiaperScout is organised around user intent rather than internal data structures.

The platform encourages exploration through five clear destinations:

- 🌿 Explore
- 📦 Products
- 📷 Scan
- 🗺️ Atlas
- 🎒 Backpack

This architecture reflects the philosophy that DiaperScout is an explorer's companion rather than a searchable database.

Everything ultimately derives from evidence gathered by Scouts, allowing the Atlas to grow organically through genuine community discovery.