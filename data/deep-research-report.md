# Biocompound Intelligence Mapping Data Pack

## Executive summary

This deliverable selects a **manageable set of 10 “top-tier” compounds** spanning **Peptides, Supplements, Coenzymes, Pharmaceuticals, and Hormones**, and encodes each as a **structured JSON entry** suitable for seeding a BioStack-style backend database. The selection intentionally leans on compounds with either **FDA-labeled clinical dosing** (pharmaceuticals/hormones/approved peptides) or **high-quality government/position-statement monographs** (supplements/coenzymes). citeturn27view0turn10view0turn12view0turn11view0turn20view0turn22view0turn24view0turn16view0

Key implementation decision: For **prescription drugs** (e.g., semaglutide, tirzepatide, metformin, testosterone, tesamorelin), the “beginner/moderate/advanced” tiers are **not “biohacker escalation”**; tiers are encoded as **label-aligned targets** (e.g., different *maintenance* dose levels or titration endpoints) to avoid inventing non-evidence-based schedules. Where a requested field is **not clinically standardized** (e.g., week-by-week titration for testosterone), it is explicitly returned as **“Data not currently available”** or `[]`. citeturn27view0turn28view0turn12view0turn13view0turn11view0

Vial mixing: Outside a licensed sterile compounding workflow with validated stability/compatibility, **mixing multiple injectables in the same vial is not recommended**; risks include sterility failures and unknown stability. This report therefore defaults vial mixing to “Not recommended / Data not currently available” unless labeling provides explicit reconstitution instructions (e.g., tesamorelin/EGRIFTA WR). citeturn25view0turn25view1turn11view0

## Source priorities and evidence-tier rubric

### Primary source priority stack

Highest-weight sources used to build protocols and safety constraints:

1. **entity["organization","U.S. Food and Drug Administration","drug regulator us"]** approved prescribing information (dose schedules, contraindications, warnings, interactions). citeturn27view0turn10view0turn11view0  
2. **entity["organization","DailyMed","nlm drug label portal us"]** drug label reproductions for dosing, contraindications, warnings, and interactions (especially for generics). citeturn12view0turn13view0  
3. **entity["organization","National Institutes of Health Office of Dietary Supplements","ods nih fact sheets"]** health professional fact sheets for supplement dosing ceilings and clinically relevant drug–nutrient interactions. citeturn18view0turn20view0turn22view0  
4. **entity["organization","National Center for Complementary and Integrative Health","nih complementary health center"]** for supplement safety/interaction summaries (e.g., CoQ10). citeturn24view0  
5. **entity["organization","International Society of Sports Nutrition","sports nutrition society issn"]** position stand for creatine efficacy and dosing protocols (loading/maintenance). citeturn16view0turn17view4  
6. Major RCTs and outcome trials for evidence-tier justification (e.g., STEP-1, SURMOUNT-1). citeturn4search0turn4search2

### Evidence tier meaning in this dataset

- **Strong**: FDA-labeled therapy with multiple RCTs/outcome trials and clear dosing/safety constraints in labeling. citeturn27view0turn10view0turn12view0turn11view0turn13view0  
- **Moderate**: High-quality monograph + consistent RCT/meta-analytic evidence for at least one meaningful endpoint, but outcomes are mixed or indication-specific (typical for supplements/coenzymes). citeturn18view0turn24view0turn26search2  
- **Limited**: Predominantly preclinical or low-quality human evidence. (Not selected as “top-tier” in this 10-compound cut.) citeturn25view1

## Dosing escalation and tier comparison

This section summarizes how tiering is implemented **without exceeding official dosing** for prescription products.

### GLP-1/GIP titration timelines

```mermaid
flowchart LR
  A[Weeks 1–4\n0.25 mg weekly] --> B[Weeks 5–8\n0.5 mg weekly]
  B --> C[Weeks 9–12\n1 mg weekly]
  C --> D[Weeks 13–16\n1.7 mg weekly]
  D --> E[Weeks 17–20\n2.4 mg weekly]
  E --> F[Week 21+\n7.2 mg weekly (max)\nOnly if 2.4 mg tolerated ≥4 weeks\nand additional weight reduction indicated]
```

The 0.25→0.5→1→1.7 step-up is explicit in Table 1, with weekly dosing “any time of day, with or without meals.” The 7.2 mg increase is explicitly described as a maximum option after tolerating 2.4 mg for ≥4 weeks when additional weight reduction is clinically indicated. citeturn27view0

```mermaid
flowchart LR
  A[Weeks 1–4\n2.5 mg weekly\n(initiation only)] --> B[Weeks 5–8\n5 mg weekly]
  B --> C[Weeks 9–12\n7.5 mg weekly]
  C --> D[Weeks 13–16\n10 mg weekly]
  D --> E[Weeks 17–20\n12.5 mg weekly]
  E --> F[Week 21+\n15 mg weekly (max)]
```

Tirzepatide escalation is label-defined: start 2.5 mg weekly for 4 weeks, then increase to 5 mg; further increases occur in 2.5 mg increments after at least 4 weeks on the current dose. Maintenance doses for weight reduction are 5/10/15 mg (maximum 15 mg). Dosing can be at any time of day with or without meals. citeturn10view0turn28view0

### Quick comparison table

| Compound | Beginner tier target | Moderate tier target | Advanced tier target | Max labeled / max reported | Escalation rule (official) |
|---|---:|---:|---:|---:|---|
| Semaglutide (Wegovy injection) | 1.7 mg weekly maintenance | 2.4 mg weekly maintenance | 7.2 mg weekly maximum (Wegovy HD) | 7.2 mg weekly | Titrate every 4 weeks; 7.2 mg only after tolerating 2.4 mg ≥4 weeks (adult weight reduction). citeturn27view0 |
| Tirzepatide (Zepbound) | 5 mg weekly maintenance | 10 mg weekly maintenance | 15 mg weekly maintenance | 15 mg weekly | Increase by 2.5 mg steps after ≥4 weeks on current dose; 2.5 mg is initiation only. citeturn10view0turn28view0 |
| Metformin ER | 500 mg nightly | 1,000–1,500 mg nightly | 2,000 mg nightly | 2,000 mg nightly (ER max in label) | Increase 500 mg weekly to max 2,000 mg once daily. citeturn12view0 |

image_group{"layout":"carousel","aspect_ratio":"16:9","query":["Wegovy dose escalation chart 0.25 0.5 1 1.7 2.4 7.2","Zepbound tirzepatide dose escalation chart 2.5 5 7.5 10 12.5 15","metformin extended-release titration 500 mg weekly increments chart"],"num_per_query":1}

## Blending, vial compatibility, and mixing policy

### Why vial mixing defaults to “not recommended”

Most injectable products do **not** provide validated data to support multi-drug co-reconstitution or multi-drug storage in one vial. Risks include:
- particulate formation, pH shifts, potency loss, adsorption to vial materials, and time/temperature instability
- microbial contamination and endotoxin risk if sterile technique or component suitability is compromised

The FDA has repeatedly emphasized that sterile drug compounding requires **suitable components** and tight control of contamination risks, noting adverse events linked to inappropriate ingredients in sterile products. Separately, FDA notes compounded drugs are not FDA-approved (no premarket verification of safety/effectiveness/quality). citeturn25view0turn25view1

### When “compatibleBlends” is populated

Only when labeling explicitly instructs reconstitution (example: EGRIFTA WR specifies using only the provided bacteriostatic water and defines vial-use duration). citeturn11view0

## Optimization baseline used across entries

To keep entries consistent and seedable, optimization fields assume an “adult active baseline,” then are slightly tailored for weight-loss pharmacotherapy (lean-mass retention emphasis).

Protein: Meta-analysis of resistance-training studies shows fat-free mass gains plateau around ~**1.6 g/kg/day** protein (~**0.73 g/lb/day**), with many practice ranges extending to ~2.2 g/kg/day (~1.0 g/lb/day) depending on energy deficit and training status. citeturn2search2

Exercise: WHO adult guidance supports **150–300 min/week moderate** (or **75–150 min/week vigorous**) activity plus **muscle-strengthening ≥2 days/week**; this is used as the default minimum in optimizationExercise fields. citeturn2search7turn2search3

## Safety disclaimers

Information below is **educational and research-oriented** and not medical advice. Prescription drugs and hormones require clinician supervision, individualized risk screening, and monitoring. Injectables require sterile handling. “Tiered dosing” is **not a recommendation to escalate**, and for prescription products is constrained to **label-based** schedules; where tiered escalation is not reliably standardized, it is marked “Data not currently available.” citeturn25view1turn27view0turn10view0turn12view0

## JSON compound entries

```json
[
  {
    "canonicalName": "Semaglutide (Wegovy injection; includes Wegovy HD 7.2 mg)",
    "aliases": ["Wegovy", "Wegovy HD", "semaglutide", "GLP-1 receptor agonist"],
    "classification": "Pharmaceutical",
    "regulatoryStatus": "FDA-approved prescription drug for chronic weight management and additional label indications; Wegovy HD 7.2 mg is an FDA-approved higher dose for weight loss and long-term maintenance in certain adults.",
    "mechanismSummary": "GLP-1 receptor agonist that reduces appetite and energy intake and can slow gastric emptying; improves cardiometabolic risk factors and reduces cardiovascular events in select populations.",
    "evidenceTier": "Strong",
    "pathways": ["glp-1-receptor-agonism", "appetite-regulation", "gastric-emptying", "cardiometabolic-risk-reduction"],
    "benefits": [
      "clinically meaningful weight loss in adults with obesity or overweight plus comorbidity (in combination with diet and activity)",
      "cardiovascular risk reduction in adults with established cardiovascular disease and obesity/overweight (label indication)",
      "metabolic improvements (e.g., glycemia and blood pressure) in many treated populations"
    ],
    "pairsWellWith": [
      "reduced-calorie diet and increased physical activity (label context)",
      "resistance training and adequate protein to mitigate lean-mass loss during weight reduction",
      "fiber and hydration strategies to support GI tolerability"
    ],
    "avoidWith": [
      "other semaglutide-containing products",
      "other GLP-1 receptor agonists",
      "personal or family history of medullary thyroid carcinoma (MTC) or MEN2 (contraindication)",
      "use in populations where contraindications apply (clinician screening required)"
    ],
    "vialCompatibility": "Not applicable (single-dose pen). Do not mix or co-administer via shared vial; no manufacturer-supported multi-drug vial blending.",
    "compatibleBlends": [],
    "recommendedDosage": "Once-weekly subcutaneous injection with 4-week step-up titration; typical maintenance 1.7 mg or 2.4 mg. For adult weight reduction, can increase to max 7.2 mg weekly only after tolerating 2.4 mg for ≥4 weeks and if additional weight reduction is clinically indicated.",
    "standardDosageRange": "0.25 mg to 7.2 mg subcutaneous once weekly (dose-escalation then maintenance; 7.2 mg is maximum labeled for adult weight reduction).",
    "incrementalEscalationSteps": [
      "Weeks 1–4: 0.25 mg weekly",
      "Weeks 5–8: 0.5 mg weekly",
      "Weeks 9–12: 1 mg weekly",
      "Weeks 13–16: 1.7 mg weekly",
      "Week 17 onward: maintenance 1.7 mg or 2.4 mg weekly (indication/tolerability)",
      "If adult weight reduction: after tolerating 2.4 mg ≥4 weeks and additional weight reduction is clinically indicated, increase to 7.2 mg weekly (max)"
    ],
    "maxReportedDose": "7.2 mg subcutaneous once weekly (maximum labeled).",
    "frequency": "Weekly",
    "preferredTimeOfDay": "Any time of day, with or without meals; administer on the same day each week.",
    "weeklyDosageSchedule": [
      "Week 1–4: 0.25 mg weekly",
      "Week 5–8: 0.5 mg weekly",
      "Week 9–12: 1 mg weekly",
      "Week 13–16: 1.7 mg weekly",
      "Week 17+: maintenance 2.4 mg weekly (or 1.7 mg if needed for tolerability)",
      "If escalating to 7.2 mg for adult weight reduction: Week 17–20: 2.4 mg weekly (tolerate ≥4 weeks), Week 21+: 7.2 mg weekly (max)"
    ],
    "tieredDosing": {
      "beginner": {
        "startDose": "0.25 mg weekly",
        "escalation": "Every 4 weeks per label up to 1.7 mg; remain at 1.7 mg as tolerated maintenance (tier target).",
        "maxDose": "1.7 mg weekly (tier target; not maximum labeled).",
        "weeklySchedule": [
          "Week 1–4: 0.25 mg weekly",
          "Week 5–8: 0.5 mg weekly",
          "Week 9–12: 1 mg weekly",
          "Week 13–16: 1.7 mg weekly",
          "Week 17+: 1.7 mg weekly maintenance"
        ],
        "safetyNotes": "GI adverse reactions are common during escalation; if a dose is not tolerated, escalation may be delayed in 4-week increments (label guidance). Maintain hydration; monitor for severe abdominal pain (pancreatitis warning context), gallbladder symptoms, and dehydration-related renal issues."
      },
      "moderate": {
        "startDose": "0.25 mg weekly",
        "escalation": "Every 4 weeks per label to reach 2.4 mg maintenance (recommended maintenance for multiple indications).",
        "maxDose": "2.4 mg weekly (tier target; not maximum labeled for adult weight reduction).",
        "weeklySchedule": [
          "Week 1–4: 0.25 mg weekly",
          "Week 5–8: 0.5 mg weekly",
          "Week 9–12: 1 mg weekly",
          "Week 13–16: 1.7 mg weekly",
          "Week 17+: 2.4 mg weekly maintenance (recommended)"
        ],
        "safetyNotes": "Dose-limiting tolerability is typically GI-related; consider delaying escalation if not tolerated. Monitor for label-listed risks (thyroid C-cell tumor warning/contraindication screen, pancreatitis, gallbladder disease, renal adverse events related to dehydration)."
      },
      "advanced": {
        "startDose": "0.25 mg weekly",
        "escalation": "Escalate per label to 2.4 mg, maintain 2.4 mg for at least 4 weeks; if additional weight reduction is clinically indicated and 2.4 mg is tolerated, increase to 7.2 mg weekly (max).",
        "maxDose": "7.2 mg weekly (maximum labeled for adult weight reduction).",
        "weeklySchedule": [
          "Week 1–4: 0.25 mg weekly",
          "Week 5–8: 0.5 mg weekly",
          "Week 9–12: 1 mg weekly",
          "Week 13–16: 1.7 mg weekly",
          "Week 17–20: 2.4 mg weekly (tolerate ≥4 weeks)",
          "Week 21+: 7.2 mg weekly (max; adult weight reduction only when clinically indicated)"
        ],
        "safetyNotes": "Higher-dose (7.2 mg) clinical trials report higher rates of GI adverse reactions and notable sensory adverse reaction signals (e.g., dysesthesia) compared with 2.4 mg; careful clinician monitoring is warranted."
      }
    },
    "drugInteractions": [
      "Concomitant insulin or insulin secretagogues can increase hypoglycemia risk; clinician may adjust other agents.",
      "Delayed gastric emptying may affect absorption of some oral medications; monitor narrow-therapeutic-index drugs.",
      "Avoid concomitant use with other semaglutide products or any GLP-1 receptor agonist (limitation of use)."
    ],
    "optimizationProtein": "0.8–1.0 g/lb/day (lean-mass preservation focus during weight reduction).",
    "optimizationCarbs": "0.6–1.4 g/lb/day depending on activity; prioritize fiber and minimize ultra-refined carbohydrate sources for appetite control.",
    "optimizationSupplements": [
      "Creatine monohydrate (3–5 g/day) if resistance training",
      "Vitamin D (dose to reach sufficiency; avoid exceeding UL without clinician)",
      "Omega-3 EPA/DHA (1–2 g/day with meals if diet low in fatty fish)",
      "Magnesium (diet-first; supplement within tolerability and interaction constraints)"
    ],
    "optimizationSleep": "7–9 hours/night with consistent sleep schedule.",
    "optimizationExercise": "150–300 min/week moderate aerobic activity (or 75–150 vigorous) + resistance training 2–4 days/week; daily steps commonly targeted 7,000–10,000 (individualized)."
  },
  {
    "canonicalName": "Tirzepatide (Zepbound)",
    "aliases": ["Zepbound", "tirzepatide", "dual GIP/GLP-1 receptor agonist"],
    "classification": "Pharmaceutical",
    "regulatoryStatus": "FDA-approved prescription drug for chronic weight management in adults with obesity/overweight plus comorbidity; also indicated for moderate-to-severe obstructive sleep apnea in adults with obesity (label-specific).",
    "mechanismSummary": "Dual GIP and GLP-1 receptor agonist that reduces appetite and energy intake and improves metabolic regulation; produces substantial sustained weight loss in randomized trials.",
    "evidenceTier": "Strong",
    "pathways": ["gip-receptor-agonism", "glp-1-receptor-agonism", "appetite-regulation", "gastric-emptying", "glucose-regulation"],
    "benefits": [
      "substantial weight loss in adults with obesity or overweight plus comorbidity (with diet and activity)",
      "improvements in cardiometabolic risk factors",
      "improvement in OSA severity in obesity in studied populations (indication-specific)"
    ],
    "pairsWellWith": [
      "reduced-calorie diet and increased physical activity (label context)",
      "higher protein and resistance training for lean-mass preservation during weight reduction",
      "hydration/electrolyte strategy to reduce dehydration risk during GI side effects"
    ],
    "avoidWith": [
      "other tirzepatide-containing products (limitation of use)",
      "any GLP-1 receptor agonist (limitation of use)",
      "personal or family history of MTC or MEN2 (contraindication)",
      "severe gastrointestinal disease such as severe gastroparesis (not recommended per label)"
    ],
    "vialCompatibility": "Not recommended to mix with other drugs. Product is provided as single-dose pens or vials; no validated multi-drug vial blending guidance in labeling.",
    "compatibleBlends": [],
    "recommendedDosage": "Start 2.5 mg once weekly for 4 weeks (initiation only), then increase to 5 mg weekly; further increases in 2.5 mg steps after at least 4 weeks on current dose. Maintenance for weight reduction: 5 mg, 10 mg, or 15 mg weekly; maximum 15 mg weekly.",
    "standardDosageRange": "2.5 mg to 15 mg subcutaneous once weekly (initiation through maintenance; 15 mg is max).",
    "incrementalEscalationSteps": [
      "Weeks 1–4: 2.5 mg weekly (initiation only; not maintenance)",
      "Week 5+: 5 mg weekly",
      "Optional escalations (≥4 weeks per step): 7.5 mg → 10 mg → 12.5 mg → 15 mg (max)"
    ],
    "maxReportedDose": "15 mg subcutaneous once weekly (maximum labeled).",
    "frequency": "Weekly",
    "preferredTimeOfDay": "Any time of day, with or without meals; same day each week.",
    "weeklyDosageSchedule": [
      "Week 1–4: 2.5 mg weekly",
      "Week 5–8: 5 mg weekly",
      "Week 9–12: 7.5 mg weekly (if escalating)",
      "Week 13–16: 10 mg weekly (if escalating)",
      "Week 17–20: 12.5 mg weekly (if escalating)",
      "Week 21+: 15 mg weekly (max; if escalating)"
    ],
    "tieredDosing": {
      "beginner": {
        "startDose": "2.5 mg weekly for 4 weeks",
        "escalation": "Escalate to 5 mg weekly and maintain as the tier target; further escalation only if clinician-directed.",
        "maxDose": "5 mg weekly (tier target).",
        "weeklySchedule": ["Week 1–4: 2.5 mg weekly", "Week 5+: 5 mg weekly maintenance"],
        "safetyNotes": "GI adverse reactions are common; dehydration can precipitate acute kidney injury. Monitor for severe abdominal pain (pancreatitis warning context) and gallbladder symptoms. Hypoglycemia risk increases if combined with insulin or sulfonylureas."
      },
      "moderate": {
        "startDose": "2.5 mg weekly for 4 weeks",
        "escalation": "After 4 weeks at 2.5 mg, increase to 5 mg; after ≥4 weeks, increase to 7.5 mg, then to 10 mg (tier target).",
        "maxDose": "10 mg weekly (tier target).",
        "weeklySchedule": [
          "Week 1–4: 2.5 mg weekly",
          "Week 5–8: 5 mg weekly",
          "Week 9–12: 7.5 mg weekly",
          "Week 13+: 10 mg weekly maintenance"
        ],
        "safetyNotes": "Higher doses have higher incidence of severe GI adverse reactions reported in trials; ensure hydration and monitor renal function if significant vomiting/diarrhea. Maintain awareness of suicidal ideation warnings for weight management products per label."
      },
      "advanced": {
        "startDose": "2.5 mg weekly for 4 weeks",
        "escalation": "Increase in 2.5 mg steps after ≥4 weeks on each dose until 15 mg weekly (tier target/max).",
        "maxDose": "15 mg weekly (tier target and max labeled).",
        "weeklySchedule": [
          "Week 1–4: 2.5 mg weekly",
          "Week 5–8: 5 mg weekly",
          "Week 9–12: 7.5 mg weekly",
          "Week 13–16: 10 mg weekly",
          "Week 17–20: 12.5 mg weekly",
          "Week 21+: 15 mg weekly maintenance (max)"
        ],
        "safetyNotes": "Severe GI adverse reaction rates were higher at higher maintenance doses in trials; monitor for dehydration and renal issues. Consider drug–absorption effects on oral meds due to delayed gastric emptying."
      }
    },
    "drugInteractions": [
      "Insulin/sulfonylureas: increased hypoglycemia risk; clinician may reduce those agents.",
      "Delayed gastric emptying may affect absorption of oral medications; combined oral contraceptive exposure can be reduced after single 5 mg dose in interaction study (Cmax and AUC reductions observed).",
      "Avoid concomitant GLP-1 receptor agonists or other tirzepatide-containing products."
    ],
    "optimizationProtein": "0.8–1.0 g/lb/day (lean-mass preservation focus during weight reduction).",
    "optimizationCarbs": "0.6–1.4 g/lb/day depending on activity; emphasize fiber and stable meal patterns for GI tolerance.",
    "optimizationSupplements": [
      "Electrolytes (especially if nausea/vomiting/diarrhea)",
      "Fiber (as tolerated; consider gradual titration)",
      "Creatine (3–5 g/day) if doing resistance training",
      "Vitamin D and magnesium if diet or labs suggest insufficiency"
    ],
    "optimizationSleep": "7–9 hours/night with consistent schedule; sleep regularity supports appetite control and training recovery.",
    "optimizationExercise": "150–300 min/week moderate aerobic activity (or 75–150 vigorous) + resistance training 2–4 days/week; daily steps 7,000–10,000 (individualized)."
  },
  {
    "canonicalName": "Metformin (extended-release tablets)",
    "aliases": ["metformin ER", "metformin XR", "biguanide"],
    "classification": "Pharmaceutical",
    "regulatoryStatus": "FDA-approved prescription drug indicated as an adjunct to diet and exercise to improve glycemic control in adults with type 2 diabetes mellitus (ER formulation label).",
    "mechanismSummary": "Biguanide that improves glycemic control primarily by reducing hepatic glucose production and improving insulin sensitivity; also has gut-mediated effects.",
    "evidenceTier": "Strong",
    "pathways": ["hepatic-glucose-production-reduction", "insulin-sensitivity", "gut-mediated-glucose-effects"],
    "benefits": ["improves glycemic control in type 2 diabetes", "low hypoglycemia risk as monotherapy", "weight-neutral to modest weight loss in many users"],
    "pairsWellWith": ["diet and exercise program for glycemic control", "clinician-directed combination therapy when needed"],
    "avoidWith": [
      "severe renal impairment (eGFR <30 mL/min/1.73m2)",
      "acute or chronic metabolic acidosis including DKA",
      "excess alcohol intake (lactic acidosis risk)",
      "iodinated contrast procedures in at-risk patients without proper holding/restart protocol"
    ],
    "vialCompatibility": "Not applicable (oral tablet).",
    "compatibleBlends": [],
    "recommendedDosage": "Start 500 mg once daily with evening meal; increase by 500 mg weekly to max 2,000 mg once daily with evening meal (ER label).",
    "standardDosageRange": "500 mg to 2,000 mg/day (ER label).",
    "incrementalEscalationSteps": ["Increase by 500 mg/week based on glycemic control and tolerability to maximum 2,000 mg/day once daily with evening meal."],
    "maxReportedDose": "2,000 mg/day ER (maximum recommended in label).",
    "frequency": "Daily",
    "preferredTimeOfDay": "Evening meal (ER label).",
    "weeklyDosageSchedule": [
      "Week 1: 500 mg once daily with evening meal",
      "Week 2: 1,000 mg once daily with evening meal",
      "Week 3: 1,500 mg once daily with evening meal",
      "Week 4+: 2,000 mg once daily with evening meal (max recommended)"
    ],
    "tieredDosing": {
      "beginner": {
        "startDose": "500 mg nightly with evening meal",
        "escalation": "Increase by 500 mg weekly as tolerated; hold at 500 mg if GI intolerance persists.",
        "maxDose": "1,000 mg/day (tier target).",
        "weeklySchedule": ["Week 1: 500 mg nightly", "Week 2+: 1,000 mg nightly (if tolerated/needed)"],
        "safetyNotes": "GI side effects can limit early titration. Screen renal function; risk of lactic acidosis increases with renal impairment, hypoxia, and other risk factors. Monitor B12 over long-term use."
      },
      "moderate": {
        "startDose": "500 mg nightly with evening meal",
        "escalation": "Increase by 500 mg weekly to 1,500 mg nightly if needed and tolerated.",
        "maxDose": "1,500 mg/day (tier target).",
        "weeklySchedule": ["Week 1: 500 mg nightly", "Week 2: 1,000 mg nightly", "Week 3+: 1,500 mg nightly (if tolerated/needed)"],
        "safetyNotes": "Monitor renal function periodically; hold for iodinated contrast imaging in specified risk groups and reassess eGFR after 48 hours before restarting."
      },
      "advanced": {
        "startDose": "500 mg nightly with evening meal",
        "escalation": "Increase by 500 mg weekly to 2,000 mg nightly (max recommended). If glycemic control not achieved at 2,000 mg once daily, label suggests considering 1,000 mg twice daily (clinician-directed).",
        "maxDose": "2,000 mg/day ER (max recommended).",
        "weeklySchedule": [
          "Week 1: 500 mg nightly",
          "Week 2: 1,000 mg nightly",
          "Week 3: 1,500 mg nightly",
          "Week 4+: 2,000 mg nightly (max recommended)"
        ],
        "safetyNotes": "Higher total dose may increase GI intolerance. Lactic acidosis risk remains the critical boxed warning concern; risk is not a 'dose tier' issue alone but risk-factor dependent (renal dysfunction, hypoperfusion, hypoxia, alcohol)."
      }
    },
    "drugInteractions": [
      "Carbonic anhydrase inhibitors may increase lactic acidosis risk; consider more frequent monitoring.",
      "Drugs that reduce metformin clearance can increase exposure (examples listed in label include cimetidine and others).",
      "Alcohol potentiates metformin effects on lactate metabolism; counsel against excessive intake.",
      "Insulin/sulfonylureas: increased hypoglycemia risk when combined; clinician may adjust."
    ],
    "optimizationProtein": "0.7–1.0 g/lb/day (if resistance training); otherwise ≥0.36 g/lb/day minimum (RDA equivalent).",
    "optimizationCarbs": "Individualized for glycemic control; common approach is consistent, fiber-forward distribution across meals (grams depend on energy needs).",
    "optimizationSupplements": [
      "Vitamin B12 monitoring (metformin can lower B12; manage deficiencies clinically)",
      "Magnesium (diet-first; supplement if low intake)",
      "Vitamin D (sufficiency-based dosing)",
      "Fiber strategies (food-first; adjunct if needed for GI tolerance)"
    ],
    "optimizationSleep": "7–9 hours/night.",
    "optimizationExercise": "150–300 min/week moderate aerobic activity (or 75–150 vigorous) + strength training ≥2 days/week; daily walking routine supports insulin sensitivity."
  },
  {
    "canonicalName": "Tesamorelin (EGRIFTA WR)",
    "aliases": ["EGRIFTA WR", "tesamorelin", "GHRH analog"],
    "classification": "Peptide",
    "regulatoryStatus": "FDA-approved prescription drug indicated for reduction of excess abdominal fat in HIV-infected adults with lipodystrophy; not indicated for weight loss management.",
    "mechanismSummary": "Synthetic GHRH analog that stimulates pituitary growth hormone release and increases IGF-1; reduces visceral adipose tissue in HIV-associated lipodystrophy.",
    "evidenceTier": "Strong",
    "pathways": ["growth-hormone-axis", "IGF-1", "visceral-adipose-tissue"],
    "benefits": ["reduction of excess abdominal fat in HIV-associated lipodystrophy (on-label)"],
    "pairsWellWith": ["antiretroviral therapy (as prescribed)", "clinician monitoring of IGF-1 and glucose parameters per label context"],
    "avoidWith": [
      "active malignancy (contraindication)",
      "pregnancy (contraindication)",
      "disruption of hypothalamic-pituitary axis (contraindication)",
      "use for general weight loss management (not indicated)"
    ],
    "vialCompatibility": "Do not blend with other drugs/peptides in the same vial. Label instructs reconstitution with only the provided diluent; vial is single-patient-use with defined beyond-use window.",
    "compatibleBlends": ["Bacteriostatic Water for Injection, USP (provided with product; label-directed diluent)"],
    "recommendedDosage": "1.28 mg (0.16 mL reconstituted solution) subcutaneously once daily into abdomen; rotate sites.",
    "standardDosageRange": "1.28 mg subcutaneous once daily (EGRIFTA WR label).",
    "incrementalEscalationSteps": ["No label-supported dose escalation; fixed daily dose."],
    "maxReportedDose": "1.28 mg subcutaneous once daily (recommended dose in EGRIFTA WR labeling).",
    "frequency": "Daily",
    "preferredTimeOfDay": "Data not currently available (label does not specify time-of-day requirement).",
    "weeklyDosageSchedule": ["Week 1+: 1.28 mg subcutaneous once daily (duration clinician-directed; no validated 'cycling' strategy in labeling)."],
    "tieredDosing": {
      "beginner": {
        "startDose": "1.28 mg once daily",
        "escalation": "No escalation (fixed dose).",
        "maxDose": "1.28 mg once daily",
        "weeklySchedule": ["Week 1+: 1.28 mg once daily"],
        "safetyNotes": "Monitor IGF-1 increase; assess glucose intolerance/diabetes risk; watch for fluid retention/carpal tunnel symptoms per label warnings."
      },
      "moderate": {
        "startDose": "1.28 mg once daily",
        "escalation": "No escalation (fixed dose).",
        "maxDose": "1.28 mg once daily",
        "weeklySchedule": ["Week 1+: 1.28 mg once daily"],
        "safetyNotes": "Same as beginner; clinical monitoring determines continuation based on visceral adipose response and risk/benefit."
      },
      "advanced": {
        "startDose": "1.28 mg once daily",
        "escalation": "No escalation (fixed dose).",
        "maxDose": "1.28 mg once daily",
        "weeklySchedule": ["Week 1+: 1.28 mg once daily"],
        "safetyNotes": "No higher label dose tier; advanced programming is not supported by labeling."
      }
    },
    "drugInteractions": ["Data not currently available (no key drug–drug interactions emphasized in highlights; clinician/pharmacist review required)."],
    "optimizationProtein": "0.7–1.0 g/lb/day if resistance training; prioritize adequate total calories if aiming to preserve lean mass.",
    "optimizationCarbs": "0.8–1.8 g/lb/day depending on activity; avoid large refined-carb loads if glucose control is a concern.",
    "optimizationSupplements": [
      "Creatine (3–5 g/day) if training",
      "Vitamin D and magnesium if diet/labs suggest insufficiency",
      "Omega-3 if diet low in fatty fish"
    ],
    "optimizationSleep": "7–9 hours/night.",
    "optimizationExercise": "Resistance training 2–4 days/week + aerobic activity per guidelines; daily walking 7,000–10,000 steps (individualized)."
  },
  {
    "canonicalName": "Testosterone cypionate (IM injection; replacement therapy)",
    "aliases": ["testosterone cypionate injection", "TRT (testosterone replacement therapy)", "androgen"],
    "classification": "Hormone",
    "regulatoryStatus": "FDA-approved prescription androgen for male hypogonadism due to specific medical conditions; Schedule III controlled substance (testosterone class).",
    "mechanismSummary": "Androgen replacement restores physiologic testosterone; binds androgen receptor and modulates gene expression; conversion to dihydrotestosterone contributes to tissue effects.",
    "evidenceTier": "Strong",
    "pathways": ["androgen-receptor-signaling", "HPT-axis-suppression", "erythropoiesis", "androgen-to-dht-conversion"],
    "benefits": [
      "improves symptoms and signs of testosterone deficiency in appropriately diagnosed men",
      "supports secondary sex characteristics and bone density in replacement context"
    ],
    "pairsWellWith": [
      "clinician monitoring (testosterone levels, hematocrit/hemoglobin, prostate assessment as indicated)",
      "sleep optimization and resistance training"
    ],
    "avoidWith": [
      "known or suspected prostate carcinoma (contraindication in label examples)",
      "pregnancy (category X; contraindicated)",
      "use for athletic performance enhancement (label states not established and warns against use for this purpose)"
    ],
    "vialCompatibility": "Not recommended to mix with other drugs in the same vial/syringe unless explicitly directed by clinician/pharmacist; compatibility is product- and vehicle-dependent.",
    "compatibleBlends": [],
    "recommendedDosage": "For replacement in the hypogonadal male: 50–400 mg intramuscularly every 2–4 weeks (label example; individualized).",
    "standardDosageRange": "50–400 mg IM every 2–4 weeks (label example; individualized).",
    "incrementalEscalationSteps": ["Data not currently available (label describes individualized adjustment based on response/adverse reactions; no standardized escalation)."],
    "maxReportedDose": "400 mg IM every 2–4 weeks appears in labeling as upper end of suggested replacement range; higher 'optimization' dosing is not supported by labeling.",
    "frequency": "Every 2–4 weeks (IM; label example)",
    "preferredTimeOfDay": "Data not currently available.",
    "weeklyDosageSchedule": "Data not currently available",
    "tieredDosing": {
      "beginner": {
        "startDose": "50–100 mg IM every 2–4 weeks (clinician-directed; within label range)",
        "escalation": "Data not currently available (individualized titration based on serum levels and clinical response).",
        "maxDose": "Data not currently available",
        "weeklySchedule": [],
        "safetyNotes": "Monitor hematocrit for polycythemia; monitor blood pressure; assess prostate/BPH risk where appropriate. Fertility suppression and endocrine effects require clinician counseling."
      },
      "moderate": {
        "startDose": "100–200 mg IM every 2–4 weeks (clinician-directed; within label range)",
        "escalation": "Data not currently available",
        "maxDose": "Data not currently available",
        "weeklySchedule": [],
        "safetyNotes": "Higher dose within replacement range may increase erythrocytosis risk; monitor lipids and cardiovascular risk factors; counsel on androgen adverse effects (acne, edema, mood changes)."
      },
      "advanced": {
        "startDose": "200–400 mg IM every 2–4 weeks (upper end of label range; clinician-directed)",
        "escalation": "Data not currently available",
        "maxDose": "400 mg IM every 2–4 weeks (upper end of label replacement range noted).",
        "weeklySchedule": [],
        "safetyNotes": "High end of replacement range increases likelihood of adverse effects (polycythemia, edema, BP elevation). Not indicated for performance enhancement."
      }
    },
    "drugInteractions": [
      "Androgens may increase sensitivity to oral anticoagulants; anticoagulant dose may require reduction.",
      "In diabetic patients, androgens may decrease blood glucose and insulin requirements.",
      "Concurrent oxyphenbutazone may increase serum levels (label example)."
    ],
    "optimizationProtein": "0.7–1.0 g/lb/day if resistance training.",
    "optimizationCarbs": "0.8–2.0 g/lb/day depending on training volume and body composition goals.",
    "optimizationSupplements": ["Vitamin D (sufficiency-based)", "Omega-3 EPA/DHA", "Creatine (3–5 g/day if training)", "Magnesium (diet-first)"],
    "optimizationSleep": "7–9 hours/night; chronic sleep restriction can impair endocrine function.",
    "optimizationExercise": "Resistance training 2–4 days/week + aerobic activity per guidelines; avoid extreme overtraining without recovery."
  },
  {
    "canonicalName": "Creatine monohydrate",
    "aliases": ["creatine", "CrM", "creatine monohydrate"],
    "classification": "Supplement",
    "regulatoryStatus": "Dietary supplement (OTC).",
    "mechanismSummary": "Increases intramuscular creatine/phosphocreatine stores, supporting rapid ATP regeneration for high-intensity efforts and improving training capacity/adaptation.",
    "evidenceTier": "Strong",
    "pathways": ["phosphocreatine-system", "high-intensity-performance", "training-adaptation-support"],
    "benefits": ["increased high-intensity exercise capacity", "greater strength/lean mass gains with resistance training", "supports tolerance of heavy training loads"],
    "pairsWellWith": ["resistance training", "adequate total protein intake", "carbohydrate or carbohydrate+protein co-ingestion can improve retention (optional)"],
    "avoidWith": ["known kidney disease without clinician oversight", "use of untrusted supplement sources (contamination risk in sport settings)"],
    "vialCompatibility": "Not applicable (oral supplement).",
    "compatibleBlends": [],
    "recommendedDosage": "Loading option: ~0.3 g/kg/day (~5 g 4x/day) for 5–7 days, then maintenance 3–5 g/day. No-load option: 3 g/day for ~28 days to saturate gradually.",
    "standardDosageRange": "3–5 g/day maintenance; optional short loading up to ~20 g/day for 5–7 days.",
    "incrementalEscalationSteps": [
      "Loading: ~0.3 g/kg/day split doses for 5–7 days",
      "Maintenance: 3–5 g/day (some larger athletes 5–10 g/day)"
    ],
    "maxReportedDose": "Position stand notes safety data up to 30 g/day for up to 5 years in healthy individuals (not a routine recommendation).",
    "frequency": "Daily",
    "preferredTimeOfDay": "Any time; adherence consistency is more important than timing. Often taken with meals or post-workout for habit.",
    "weeklyDosageSchedule": [
      "Option A (loading): Week 1: ~0.3 g/kg/day split; Week 2+: 3–5 g/day",
      "Option B (no loading): Weeks 1–4: 3 g/day; Week 5+: 3–5 g/day"
    ],
    "tieredDosing": {
      "beginner": {
        "startDose": "3 g/day",
        "escalation": "Optional increase to 5 g/day after 2–4 weeks based on training goals/tolerance.",
        "maxDose": "5 g/day (tier target).",
        "weeklySchedule": ["Week 1–4: 3 g/day", "Week 5+: 3–5 g/day"],
        "safetyNotes": "Possible water-weight increase; occasional GI upset if taken in large boluses—split dosing can help."
      },
      "moderate": {
        "startDose": "5 g/day",
        "escalation": "No escalation typically required; maintain daily.",
        "maxDose": "5 g/day (tier target).",
        "weeklySchedule": ["Week 1+: 5 g/day"],
        "safetyNotes": "Maintain hydration; avoid if renal disease without clinician guidance."
      },
      "advanced": {
        "startDose": "~0.3 g/kg/day (split) for 5–7 days",
        "escalation": "Transition to 3–5 g/day maintenance after loading; larger athletes may need 5–10 g/day to maintain stores.",
        "maxDose": "Data not currently available (routine max not established; high-dose safety reported in literature but not a standard protocol).",
        "weeklySchedule": ["Week 1: ~0.3 g/kg/day split doses", "Week 2+: 3–5 g/day (or 5–10 g/day for larger athletes)"],
        "safetyNotes": "Loading increases GI upset risk—split dosing and taking with meals can help."
      }
    },
    "drugInteractions": ["Data not currently available (no well-established major interactions in healthy adults; use caution with renal disease and nephrotoxic medication contexts)."],
    "optimizationProtein": "0.7–1.0 g/lb/day for resistance training adaptations.",
    "optimizationCarbs": "1.4–2.3 g/lb/day for moderate training; adjust to goals (surplus/maintenance/deficit).",
    "optimizationSupplements": ["Vitamin D (sufficiency-based)", "Omega-3 EPA/DHA", "Magnesium (diet-first)"],
    "optimizationSleep": "7–9 hours/night.",
    "optimizationExercise": "Progressive resistance training 2–4 days/week + aerobic work per guidelines; daily walking 7,000–10,000 steps (individualized)."
  },
  {
    "canonicalName": "Omega-3 fatty acids (EPA + DHA)",
    "aliases": ["fish oil", "EPA", "DHA", "omega-3"],
    "classification": "Supplement",
    "regulatoryStatus": "Dietary supplement (OTC). Prescription omega-3 products exist with distinct labeling and indications.",
    "mechanismSummary": "Long-chain omega-3s lower triglycerides (dose-dependent) and influence inflammatory lipid mediators; at high doses can affect platelet aggregation and bleeding time.",
    "evidenceTier": "Moderate",
    "pathways": ["triglyceride-lowering", "lipid-mediator-modulation", "platelet-function-dose-dependent"],
    "benefits": ["triglyceride reduction (dose-dependent)", "cardiometabolic support in select contexts; outcomes depend on dose and population"],
    "pairsWellWith": ["heart-healthy diet pattern", "statin therapy when clinically indicated (medical supervision)"],
    "avoidWith": [
      "high-dose use with anticoagulants without monitoring",
      "advanced-dose use (e.g., 4 g/day) in patients at high AF risk without clinician context"
    ],
    "vialCompatibility": "Not applicable (oral supplement).",
    "compatibleBlends": [],
    "recommendedDosage": "Typical supplement range 1–2 g/day combined EPA+DHA with meals; higher doses (e.g., 4 g/day) used for triglycerides usually under medical supervision.",
    "standardDosageRange": "0.5 g/day to 5 g/day EPA+DHA combined (5 g/day cited as safe upper for supplements when used as recommended).",
    "incrementalEscalationSteps": ["Start 1 g/day; increase to 2 g/day after 2–4 weeks; consider 4 g/day only with clinician oversight for triglycerides."],
    "maxReportedDose": "5 g/day EPA+DHA combined from supplements cited as safe when used as recommended; 4 g/day in large trials associated with slightly increased atrial fibrillation risk in certain high-risk populations.",
    "frequency": "Daily",
    "preferredTimeOfDay": "With meals (often divided doses) for tolerability/absorption.",
    "weeklyDosageSchedule": ["Week 1–2: 1 g/day", "Week 3+: 2 g/day (if needed and tolerated)", "Advanced (medical): 4 g/day (goal-specific)"],
    "tieredDosing": {
      "beginner": {
        "startDose": "1 g/day EPA+DHA combined",
        "escalation": "Maintain 1 g/day; consider increase only for target triglycerides under clinical guidance.",
        "maxDose": "1 g/day (tier target).",
        "weeklySchedule": ["Week 1+: 1 g/day with meals"],
        "safetyNotes": "Mild GI side effects possible; ensure product quality and verify EPA/DHA content on label."
      },
      "moderate": {
        "startDose": "2 g/day EPA+DHA combined",
        "escalation": "Split into 2 doses with meals; reassess lipids after ~8–12 weeks if triglyceride goal.",
        "maxDose": "2 g/day (tier target).",
        "weeklySchedule": ["Week 1+: 2 g/day split with meals"],
        "safetyNotes": "Monitor if on antiplatelet/anticoagulant therapy; bleeding-time effects are dose-dependent."
      },
      "advanced": {
        "startDose": "4 g/day EPA+DHA combined (goal-specific; typically clinician-directed)",
        "escalation": "Maintain if medically indicated; monitor adverse effects and arrhythmia risk context.",
        "maxDose": "5 g/day (upper safety ceiling cited for supplement EPA+DHA when used as recommended; not a routine target).",
        "weeklySchedule": ["Week 1+: 4 g/day split with meals (clinician-directed)"],
        "safetyNotes": "High-dose omega-3 can increase bleeding time; large trials reported slightly increased atrial fibrillation risk at 4 g/day in high-risk CVD populations. Monitor and individualize."
      }
    },
    "drugInteractions": [
      "Warfarin and similar anticoagulants: fish oil may prolong clotting time (INR) at high doses; most data show 3–6 g/day may not materially change anticoagulation status, but periodic INR monitoring is advised.",
      "Antiplatelet effects at high doses; monitor bleeding risk with antithrombotic regimens."
    ],
    "optimizationProtein": "0.7–1.0 g/lb/day if training; otherwise meet minimum adequacy.",
    "optimizationCarbs": "1.0–2.0 g/lb/day depending on training; choose fiber-forward sources supportive of lipid and glycemic control.",
    "optimizationSupplements": ["Vitamin D (sufficiency-based)", "Magnesium (diet-first)", "Creatine (if training)"],
    "optimizationSleep": "7–9 hours/night.",
    "optimizationExercise": "Aerobic base per guidelines + resistance training ≥2 days/week; consistent daily movement supports lipid metabolism."
  },
  {
    "canonicalName": "Vitamin D3 (cholecalciferol)",
    "aliases": ["vitamin D", "cholecalciferol", "calciferol"],
    "classification": "Supplement",
    "regulatoryStatus": "Dietary supplement (OTC); also present in prescription formulations.",
    "mechanismSummary": "Fat-soluble vitamin converted to 25(OH)D and then active 1,25(OH)2D (calcitriol); supports calcium/phosphate homeostasis and bone mineralization.",
    "evidenceTier": "Strong",
    "pathways": ["calcium-homeostasis", "bone-mineralization", "vitamin-d-receptor-signaling"],
    "benefits": ["corrects vitamin D insufficiency/deficiency", "supports bone health and reduces risk of rickets/osteomalacia in deficiency states"],
    "pairsWellWith": ["adequate calcium intake primarily from food", "weight-bearing and resistance training"],
    "avoidWith": ["chronic high-dose supplementation above UL without monitoring", "thiazide diuretics plus high vitamin D without clinician monitoring in susceptible people"],
    "vialCompatibility": "Not applicable (oral supplement).",
    "compatibleBlends": [],
    "recommendedDosage": "Adults: RDA 600 IU/day (19–70) and 800 IU/day (>70). Adult UL generally 4,000 IU/day. Deficiency treatment is clinician-directed.",
    "standardDosageRange": "600–2,000 IU/day common supplementation; UL 4,000 IU/day for adults.",
    "incrementalEscalationSteps": ["Increase slowly toward target based on labs/clinical context; avoid exceeding 4,000 IU/day without clinician. Recheck 25(OH)D as indicated."],
    "maxReportedDose": "4,000 IU/day for adults (UL).",
    "frequency": "Daily",
    "preferredTimeOfDay": "Any time; often with meals for adherence.",
    "weeklyDosageSchedule": ["Week 1+: daily intake per tier; reassess after 8–12 weeks if correcting deficiency under clinician guidance."],
    "tieredDosing": {
      "beginner": {
        "startDose": "600–800 IU/day",
        "escalation": "Maintain unless labs indicate insufficiency; clinician-directed if deficiency.",
        "maxDose": "800 IU/day (tier target).",
        "weeklySchedule": ["Week 1+: 600–800 IU/day"],
        "safetyNotes": "Avoid combining high-dose calcium + vitamin D without indication; toxicity primarily from excessive supplement dosing."
      },
      "moderate": {
        "startDose": "1,000–2,000 IU/day",
        "escalation": "Maintain; monitor 25(OH)D if using higher daily doses long-term.",
        "maxDose": "2,000 IU/day (tier target).",
        "weeklySchedule": ["Week 1+: 1,000–2,000 IU/day"],
        "safetyNotes": "Monitor interactions with orlistat, steroids, and thiazide diuretics in at-risk patients."
      },
      "advanced": {
        "startDose": "4,000 IU/day",
        "escalation": "Do not exceed UL without clinician monitoring; consider lab testing to avoid hypercalcemia.",
        "maxDose": "4,000 IU/day (UL; adult).",
        "weeklySchedule": ["Week 1+: 4,000 IU/day (UL; use only if justified by clinician/labs)"],
        "safetyNotes": "Risk of hypercalcemia/hypercalciuria rises with excessive dosing; thiazides can increase hypercalcemia risk when combined with vitamin D."
      }
    },
    "drugInteractions": [
      "Thiazide diuretics: may increase hypercalcemia risk with vitamin D.",
      "Orlistat can reduce vitamin D absorption.",
      "Steroids can impair vitamin D metabolism and calcium absorption."
    ],
    "optimizationProtein": "0.7–1.0 g/lb/day if training; ensure adequacy for bone and muscle health.",
    "optimizationCarbs": "0.8–2.0 g/lb/day depending on training and energy needs.",
    "optimizationSupplements": ["Magnesium (diet-first)", "Omega-3 EPA/DHA if diet low in fatty fish", "Creatine if training"],
    "optimizationSleep": "7–9 hours/night.",
    "optimizationExercise": "Weight-bearing and resistance exercise ≥2 days/week + aerobic movement per guidelines."
  },
  {
    "canonicalName": "Magnesium (supplemental magnesium; elemental dosing)",
    "aliases": ["magnesium glycinate", "magnesium citrate", "magnesium oxide", "elemental magnesium"],
    "classification": "Supplement",
    "regulatoryStatus": "Dietary supplement (OTC); also present in antacids/laxatives (different dosing contexts).",
    "mechanismSummary": "Essential mineral and enzyme cofactor involved in neuromuscular function, energy metabolism, blood glucose control, and blood pressure regulation.",
    "evidenceTier": "Moderate",
    "pathways": ["enzyme-cofactor", "neuromuscular-function", "electrolyte-balance", "blood-pressure-regulation-support"],
    "benefits": ["corrects magnesium inadequacy/deficiency", "supports muscle/nerve function"],
    "pairsWellWith": ["dietary magnesium-rich foods (leafy greens, legumes, nuts, whole grains)", "hydration/electrolytes for heavy sweating"],
    "avoidWith": ["significant renal impairment (hypermagnesemia risk)", "high-dose supplemental magnesium causing diarrhea and GI distress"],
    "vialCompatibility": "Not applicable (oral supplement).",
    "compatibleBlends": [],
    "recommendedDosage": "RDA (all sources) roughly 400–420 mg/day adult men and 310–320 mg/day adult women. Supplemental UL for adults is 350 mg/day (supplements/medications only).",
    "standardDosageRange": "100–350 mg/day elemental supplemental magnesium (within adult UL); diet provides the remainder toward RDA.",
    "incrementalEscalationSteps": ["Increase by 50–100 mg elemental increments every 1–2 weeks based on GI tolerance; do not exceed 350 mg/day supplemental UL without clinician guidance."],
    "maxReportedDose": "350 mg/day supplemental magnesium UL for adults (supplements/medications only; excludes food magnesium).",
    "frequency": "Daily",
    "preferredTimeOfDay": "Any time; some prefer evening for adherence.",
    "weeklyDosageSchedule": ["Week 1: 100 mg/day", "Week 2: 200 mg/day (if tolerated)", "Week 3+: 200–350 mg/day (as needed; stay within UL)"],
    "tieredDosing": {
      "beginner": {
        "startDose": "100–200 mg/day elemental (supplement) with food",
        "escalation": "Increase only if needed; prioritize dietary sources.",
        "maxDose": "200 mg/day (tier target).",
        "weeklySchedule": ["Week 1+: 100–200 mg/day"],
        "safetyNotes": "Diarrhea is the common limiting effect; separate dosing from certain antibiotics and bisphosphonates to prevent absorption issues."
      },
      "moderate": {
        "startDose": "200–350 mg/day elemental (supplement) with food",
        "escalation": "Adjust within UL based on tolerance.",
        "maxDose": "350 mg/day (tier target; UL).",
        "weeklySchedule": ["Week 1+: 200–350 mg/day"],
        "safetyNotes": "Monitor interactions: bisphosphonates, tetracyclines, quinolones; diuretics and PPIs can affect magnesium status."
      },
      "advanced": {
        "startDose": "350 mg/day elemental (supplement) with food",
        "escalation": "Do not exceed supplemental UL routinely; higher dosing requires clinician oversight, especially with renal risk.",
        "maxDose": "350 mg/day (UL; adult).",
        "weeklySchedule": ["Week 1+: 350 mg/day (UL)"],
        "safetyNotes": "Risk of hypermagnesemia rises with renal impairment; very large doses can cause hypotension, arrhythmia, and severe toxicity."
      }
    },
    "drugInteractions": [
      "Oral bisphosphonates: magnesium decreases absorption; separate by at least 2 hours.",
      "Tetracycline and quinolone antibiotics: chelation reduces absorption; separate by 2 hours before or 4–6 hours after magnesium.",
      "Diuretics can increase magnesium loss (loop/thiazide); PPIs can cause hypomagnesemia with long-term use; FDA advises monitoring in long-term PPI therapy contexts."
    ],
    "optimizationProtein": "0.7–1.0 g/lb/day if training.",
    "optimizationCarbs": "1.0–2.0 g/lb/day depending on training and energy needs.",
    "optimizationSupplements": ["Vitamin D (sufficiency-based)", "Omega-3 EPA/DHA if diet low in fatty fish", "Creatine if training"],
    "optimizationSleep": "7–9 hours/night.",
    "optimizationExercise": "Aerobic movement per guidelines + resistance training ≥2 days/week; ensure recovery and hydration."
  },
  {
    "canonicalName": "Coenzyme Q10 (CoQ10)",
    "aliases": ["CoQ10", "ubiquinone", "ubiquinol"],
    "classification": "Coenzyme",
    "regulatoryStatus": "Dietary supplement (OTC) in the United States; not FDA-approved as a drug.",
    "mechanismSummary": "Endogenous cofactor in mitochondrial electron transport and cellular energy production; antioxidant activity. Supplementation studied in cardiovascular and neurodegenerative contexts with mixed outcomes.",
    "evidenceTier": "Moderate",
    "pathways": ["mitochondrial-electron-transport", "cellular-energy-metabolism", "antioxidant-defense"],
    "benefits": [
      "evidence is mixed across conditions; potential adjunct benefit in some cardiovascular contexts",
      "not supported for statin muscle pain reduction as a general claim per NIH summary"
    ],
    "pairsWellWith": ["fat-containing meal to improve absorption (fat-soluble)", "clinician-directed cardiovascular regimen where studied"],
    "avoidWith": ["warfarin without INR monitoring (interaction potential)", "diabetes medications without monitoring if hypoglycemia risk context (interaction potential)"],
    "vialCompatibility": "Not applicable (oral supplement).",
    "compatibleBlends": [],
    "recommendedDosage": "Common studied dosing ranges in heart failure trials are often 60–300 mg/day; higher-dose neurodegenerative trials used 1,200–2,400 mg/day without benefit for Parkinson’s progression in a large RCT.",
    "standardDosageRange": "100–300 mg/day commonly used; high-dose research up to 2,400 mg/day in RCTs (not routine).",
    "incrementalEscalationSteps": ["Start 100 mg/day; increase to 200 mg/day after 2–4 weeks; consider 300 mg/day based on goal/tolerance; high-dose protocols are research-only and require clinician oversight."],
    "maxReportedDose": "2,400 mg/day in a randomized clinical trial (Parkinson’s disease RCT), reported as high-dose arm; not a routine supplement target.",
    "frequency": "Daily",
    "preferredTimeOfDay": "With food containing fat; some prefer earlier in day if insomnia occurs.",
    "weeklyDosageSchedule": ["Week 1–2: 100 mg/day", "Week 3–4: 200 mg/day", "Week 5+: 200–300 mg/day (goal/tolerance based)"],
    "tieredDosing": {
      "beginner": {
        "startDose": "100 mg/day",
        "escalation": "Maintain or increase to 200 mg/day after 2–4 weeks depending on goal and tolerability.",
        "maxDose": "200 mg/day (tier target).",
        "weeklySchedule": ["Week 1–2: 100 mg/day", "Week 3+: 200 mg/day"],
        "safetyNotes": "Usually well tolerated; mild GI upset or insomnia possible."
      },
      "moderate": {
        "startDose": "200 mg/day",
        "escalation": "Increase to 300 mg/day if needed; split doses with meals if GI effects occur.",
        "maxDose": "300 mg/day (tier target).",
        "weeklySchedule": ["Week 1+: 200–300 mg/day"],
        "safetyNotes": "Interaction potential with warfarin and insulin noted by NIH; coordinate with clinician if on these drugs."
      },
      "advanced": {
        "startDose": "300 mg/day",
        "escalation": "Data not currently available for a standardized safe escalation beyond 300 mg/day for general use; high-dose RCTs exist but are not routine protocols.",
        "maxDose": "2,400 mg/day (research RCT maximum arm; not routine).",
        "weeklySchedule": ["Data not currently available"],
        "safetyNotes": "High-dose CoQ10 has been studied in RCTs (e.g., 1,200 and 2,400 mg/day arms) but is not a routine supplement approach; interaction risk with anticoagulants and diabetes drugs remains relevant."
      }
    },
    "drugInteractions": [
      "May interact with warfarin (blood thinner) and insulin (diabetes drug) per NIH; monitor clinically.",
      "Potential incompatibility with some cancer treatments (NIH summary warning)."
    ],
    "optimizationProtein": "0.7–1.0 g/lb/day if training; ensure adequate energy intake for mitochondrial support goals.",
    "optimizationCarbs": "1.0–2.0 g/lb/day depending on training and metabolic goals.",
    "optimizationSupplements": ["Omega-3 EPA/DHA", "Vitamin D (sufficiency-based)", "Magnesium (diet-first)"],
    "optimizationSleep": "7–9 hours/night.",
    "optimizationExercise": "Aerobic base per guidelines + resistance training ≥2 days/week; consider zone-2 aerobic work 2–4 sessions/week depending on conditioning."
  }
]
```