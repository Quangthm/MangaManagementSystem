# SpotBugs Demo 2 -- Build Check (Java Sidecar)

This demo shows how SpotBugs can act as a **build quality gate**. It contains two Maven projects side by side:

| Folder | Purpose | Expected Result |
|---|---|---|
| `buggy/` | Contains intentional programming mistakes | `spotbugs:check` **FAILS** |
| `fixed/` | Same code with all mistakes corrected | `spotbugs:check` **PASSES** |

## Why a Java project inside a C# repository?

SpotBugs analyzes **compiled Java bytecode** (`.class` files). It cannot analyze C# source files or .NET assemblies. This Java sidecar exists solely to provide a correct, working classroom demonstration of SpotBugs.

## JDK 8 Compatibility

- SpotBugs Maven Plugin: `4.8.5.0` (compatible with JDK 8)
- Java source/target: `1.8`
- Encoding: `UTF-8`

## Mistakes in BuggyExample.java

| Mistake | SpotBugs Pattern | Fix in FixedExample.java |
|---|---|---|
| Returns internal array | EI_EXPOSE_REP | Returns `Arrays.copyOf()` |
| Stores external array | EI_EXPOSE_REP2 | Stores `Arrays.copyOf()` |
| Null dereference | NP_NULL_ON_SOME_PATH | Returns early when null |
| String `==` comparison | ES_COMPARING_STRINGS_WITH_EQ | Uses `.equals()` |
| equals without hashCode | HE_EQUALS_NO_HASHCODE | Implements both |

## How to Run

Buggy version (expected to **fail**):

```bash
cd tools/spotbugs-check-demo/buggy
mvn clean compile spotbugs:check
```

Fixed version (expected to **pass**):

```bash
cd tools/spotbugs-check-demo/fixed
mvn clean compile spotbugs:check
```

Or run both demos together from the repo root:

```powershell
powershell -ExecutionPolicy Bypass -File tools/run-spotbugs-two-demos.ps1
```

## Analyzers Used

**SpotBugs only.** No Checkstyle, PMD, SonarQube, Roslyn, StyleCop, or ReSharper.
