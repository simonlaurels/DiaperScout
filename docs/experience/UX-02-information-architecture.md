# Information Architecture

**Document ID:** UX-02  
**Phase:** Experience Design  
**Status:** Draft  
**Depends On:** UX-01 Experience Principles

---

# Purpose

This document defines how information is organised throughout DiaperScout.

The information architecture is intentionally designed around user intentions rather than technical entities or database structures.

Visitors should always know where to go without needing to understand how the platform is implemented.

---

# Philosophy

DiaperScout is organised around exploration.

People do not think in terms of products, retailers or observations.

Instead they naturally ask questions such as:

- What's happening?
- I'm looking for something.
- What is this?
- Where can I explore?
- What have I collected?

The information architecture is designed around these questions.

---

# Mental Model

DiaperScout consists of five primary destinations.

- 🌿 Explore
- 📦 Products
- 📷 Scan
- 🗺️ Atlas
- 🎒 Backpack

Each destination has a clear purpose.

Overlap should be minimised.

Users should rarely wonder where a feature belongs.

---

# Primary Navigation

## 🌿 Explore

Explore is the living heartbeat of DiaperScout.

It is the place where the world feels alive.

Rather than acting as a dashboard, Explore surfaces interesting activity from across the Atlas.

Typical content includes:

- Recent discoveries
- Community observations
- Newly documented products
- Atlas highlights
- Nearby activity
- Ways to help
- Seasonal stories
- Interesting historical discoveries

Explore exists to encourage curiosity.

Visitors should regularly discover something they were not actively searching for.

---

## 📦 Products

Products is the primary destination for discovering products.

It contains everything needed to browse and understand the product catalogue.

Capabilities include:

- Search
- Categories
- Brands
- Filters
- Product comparison
- Recently added
- Recently updated

Search is considered a capability within Products rather than a separate destination.

Products answer one question:

> "What am I looking for?"

---

## 📷 Scan

Scan is one of DiaperScout's defining experiences.

Selecting Scan immediately opens the barcode scanner.

Possible outcomes include:

### Product recognised

Open the existing product page.

### Product unknown

Invite the visitor to help document the product for the community.

Scanning is primarily an identification experience.

Contribution begins only after the user chooses to help.

---

## 🗺️ Atlas

Atlas represents places.

It is not simply a map.

Atlas allows visitors to explore the real-world locations where products have been observed.

Atlas contains multiple views including:

- Interactive map
- Nearby places
- Retailer list
- Online retailers
- Individual locations

Atlas answers:

> "Where can I explore?"

---

## 🎒 Backpack

Backpack represents the visitor's personal journey.

Rather than functioning as a traditional profile page, Backpack contains everything belonging to the individual.

Examples include:

- Draft contributions
- Saved products
- Collections
- Contribution history
- Settings
- Downloads
- Offline items

Backpack answers:

> "What belongs to me?"

---

# Core Concepts

Although navigation is organised around user intentions, several concepts underpin the platform.

## Products

Individual absorbent products.

Products contain:

- Information
- Images
- Reviews
- Observations
- Product history
- Availability evidence

---

## Observations

Observations are the foundation of DiaperScout.

Every observation records a genuine discovery made at a specific place and time.

Observations connect:

- Product
- Location
- Time
- Contributor
- Evidence

Everything within the Atlas ultimately derives from observations.

---

## Retailers

Retailers represent organisations that sell products.

Examples include:

- Supermarkets
- Pharmacies
- Mobility retailers
- Specialist retailers
- Online retailers

Retailers are primarily explored through Atlas.

---

## Locations

Locations represent individual branches or online destinations.

Examples include:

- Tesco Extra Yate
- Boots Broadmead
- Rearz

Observations belong to locations rather than retailer brands.

This distinction allows every location to build its own independent history.

---

# The Atlas

The Atlas is not a business directory.

It is a record of verified discoveries.

A location becomes part of the Atlas only after at least one relevant observation has been recorded there.

The Atlas therefore grows naturally through community exploration.

Every new contribution expands the world.

---

# Evidence Model

DiaperScout presents evidence rather than assumptions.

For example:

Correct:

> TENA Slip Maxi was observed at Tesco Extra Yate on 3 August 2026.

Avoid:

> Tesco stocks TENA Slip Maxi.

Observations describe what has been seen.

They do not imply current stock availability.

Future retailer integrations may provide additional evidence.

These should complement community observations rather than replace them.

---

# Navigation Principles

Every destination answers a different question.

| Destination | User Question |
|--------------|---------------|
| 🌿 Explore | What's happening? |
| 📦 Products | What am I looking for? |
| 📷 Scan | What is this? |
| 🗺️ Atlas | Where can I explore? |
| 🎒 Backpack | What belongs to me? |

This separation keeps navigation simple while encouraging exploration.

---

# Contextual Experiences

DiaperScout may provide helpful contextual suggestions throughout the experience.

Examples include:

- Continue unfinished contribution
- Nearby discoveries
- Products recently observed nearby
- Ways to improve existing information

Context should support rather than interrupt.

Suggestions should appear naturally within existing screens instead of taking control of the experience.

---

# Future Growth

The information architecture is intentionally designed to evolve.

Future capabilities should strengthen existing destinations rather than introducing additional top-level navigation.

Examples include:

- Retail stock integrations
- Route planning
- Personal recommendations
- Collection challenges
- Advanced filtering
- Community events

The five-destination model should remain stable as the application grows.

---

# Summary

DiaperScout is organised around exploration rather than data structures.

Its five destinations—

- 🌿 Explore
- 📦 Products
- 📷 Scan
- 🗺️ Atlas
- 🎒 Backpack

—reflect how people naturally think while discovering products and places.

Everything ultimately grows from observations, allowing the Atlas to evolve organically through genuine community discoveries while remaining transparent about what is known and how that knowledge was obtained.