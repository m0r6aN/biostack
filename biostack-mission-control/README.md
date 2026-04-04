# BioStack Mission Control

## A personal bio-protocol operating system with intelligence, safety scaffolding, and observability

Track compounds, log check-ins, correlate timelines, and explore bio-intelligence.

---

## 🚀 Vision
BioStack Mission Control is a high-fidelity platform designed for researchers and individuals to manage complex protocols with precision. It moves beyond simple logging into **true observability**, using a structured knowledge base to correlate substances, biological pathways, and personal outcomes.

---

## 💎 Core Features

### 🧪 Advanced Compound Management
*   **Guided Intelligence Entry**: A 5-step intuitive flow to log compounds based on **Category** and **Goal** (e.g., Peptides → Improved Healing).
*   **Dynamic Auto-Population**: Compounds are instantly suggested from the internal knowledge base, including established peptides (BPC-157, TB-500, MOTS-C) and coenzymes (NAD+).
*   **Financial Tracking**: Keep record of **Source** and **Price Paid** for every compound in your protocol to manage budget and supplier consistency.
*   **Manual Override**: Full flexibility to search for or manually enter substances not found in the seeded database.

### 🧬 Bio-Intelligence Engine
*   **Pathway Overlap Detection**: Advanced analysis of overlapping biological pathways (e.g., Tissue Repair, Mitochondrial Function) to identify synergies or risks.
*   **Personalized Protocol Guidance**: Dynamic recommendations tailored to your profile's **Age**, **Sex**, and **Weight** (e.g., age-dependent mitochondrial response notes).
*   **Synergy & Compatibility Mapping**: Visual insights into which compounds pair well, which should be avoided, and which are compatible for in-vial blending.
*   **Evidence-Based Reference**: Substance metadata categorized by **Evidence Tier** (Strong, Moderate, Limited) with full source citations and mechanism summaries.

### 📊 Protocol Observability
*   **Chronological Timeline**: A unified visualization of your protocol history, correlating compound starts/stops with subjective check-ins.
*   **Phase Tracking**: Organize your journey into distinct phases (e.g., "Loading Phase," "Maintenance") to analyze longitudinal efficacy.
*   **Multi-Dimensional Check-ins**: Log bio-markers and subjective feel to correlate substance use with performance and recovery outcomes.

### 🧮 Bio-Mathematical Calculators
*   **Precision Reconstitution**: Calculate exact volume requirements for lyophilized powders based on vial size and desired concentration.
*   **Dosage Conversions**: Convert between MCG, MG, and international units with built-in safety math.
*   **Volume & Concentration Mapping**: Visual guidance for drawing correct units based on syringe types.

### 👤 Profile Personalization
*   **Comprehensive Bio-Stats**: Track Weight (Imperial/Metric), Sex, and **Age** to personalize the intelligence engine's outputs.
*   **Goal Tracking**: Set high-level goals (e.g., "Mitochondrial Health," "Injury Recovery") that drive the guided compound selection engine.

---

## 🏗️ System Architecture

BioStack follows a **Clean Architecture** pattern, ensuring the logic is decoupled from the infrastructure and UI.

*   **API Layer**: Minimal APIs built with .NET 8, featuring Scalar for live documentation and health monitoring.
*   **Application Layer**: Decoupled use cases, validation logic, and protocol mapping services.
*   **Domain Layer**: Pure entities and value objects containing the core biometrics rules.
*   **Infrastructure Layer**: Local-first persistence using SQLite, with adapter-ready interfaces for scaling to PostgreSQL or Cloud SQL.
*   **React Frontend**: A premium, high-performance dashboard built with Next.js 16, TypeScript, and Recharts, featuring a dark-mode "Mission Control" aesthetic.

---

## 🛡️ Safety & Compliance Boundary

**BioStack is for educational and observational use only.**

1.  **Not Medical Advice**: This system does not provide medical dosing recommendations, injection instructions, or clinical diagnosis.
2.  **Mathematical Logic Only**: Calculator outputs are based on pure mathematical formulas and should always be verified manually.
3.  **Educational Reference**: The knowledge base is a tool for researchers to aggregate scientific literature; it is not a therapeutic guide.

---

## 📦 Seeded Knowledge Base 

The initial database includes deep-protocol data for:
*   **Peptides**: BPC-157, TB-500, MOTS-C, Retatrutide, Tirzepatide.
*   **Coenzymes**: NAD+, NMN.
*   **Pharmaceuticals**: Metformin, Investigational Agonists.
*   **Optimization**: Seeded guidelines for Protein/Carb intake, specific supplements (CoQ10, Creatine), and Exercise/Sleep hygiene targets.
