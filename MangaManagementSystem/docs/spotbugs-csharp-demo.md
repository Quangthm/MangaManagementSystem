# SpotBugs Classroom Demos for a C# Repository

## What is SpotBugs?

[SpotBugs](https://spotbugs.github.io/) is a code-quality analysis tool that finds common programming mistakes in **compiled Java bytecode**. It is the successor to FindBugs and detects over 400 bug patterns including null pointer dereferences, bad coding practices, and performance issues.

SpotBugs analyzes `.class` files produced by the Java compiler. It does **not** analyze Java source text directly -- it works on the bytecode level.

## Why SpotBugs Cannot Directly Scan C# Files

SpotBugs operates exclusively on **Java Virtual Machine (JVM) bytecode**. It does not understand C# source code (`.cs`), .NET Intermediate Language (IL), or .NET assemblies (`.dll`/`.exe`). There is no plugin or configuration that enables SpotBugs to analyze C# code. This is a fundamental architectural difference between the JVM and .NET CLR platforms.

## What These Demos Do (Honestly)

This repository is primarily a **C# / .NET** project. We do not claim that SpotBugs scans C# code. Instead, we include small **Java sidecar modules** inside the `tools/` folder to provide correct, working classroom demonstrations of SpotBugs analyzing Java bytecode.

## JDK 8 Compatibility

Both demos are configured for **JDK 8**:

- SpotBugs Maven Plugin: `4.8.5.0` (compatible with JDK 8)
- Java source/target: `1.8`
- Encoding: `UTF-8`

Newer SpotBugs Maven Plugin versions (4.9.x+) may require JDK 11 or newer.

## Analyzers Used

**SpotBugs only.** No Checkstyle, PMD, SonarQube, Roslyn, StyleCop, ReSharper, or other analyzers are included in these demos.

---

## Demo 1: SpotBugs Report Generation

**Location:** `tools/spotbugs-demo/`

This demo compiles Java code with intentional simple mistakes and runs SpotBugs to **generate a report** listing the findings.

### Mistakes demonstrated

| Mistake | SpotBugs Pattern |
|---|---|
| Return internal mutable array | EI_EXPOSE_REP |
| Store external mutable array directly | EI_EXPOSE_REP2 |
| equals() without hashCode() | HE_EQUALS_NO_HASHCODE |
| String comparison with `==` | ES_COMPARING_STRINGS_WITH_EQ |
| Null dereference after null check | NP_NULL_ON_SOME_PATH |

### How to run manually

```bash
cd tools/spotbugs-demo
mvn clean compile spotbugs:spotbugs
```

### Report output

- XML report: `tools/spotbugs-demo/target/spotbugsXml.xml`

---

## Demo 2: SpotBugs Build Check

**Location:** `tools/spotbugs-check-demo/`

This demo shows how SpotBugs can act as a **build quality gate** using `spotbugs:check`. It contains two side-by-side Maven projects:

| Folder | Purpose | Expected Result |
|---|---|---|
| `buggy/` | Contains intentional programming mistakes | `spotbugs:check` **FAILS** the build |
| `fixed/` | Same code with all mistakes corrected | `spotbugs:check` **PASSES** the build |

### Mistakes and fixes

| BuggyExample.java | FixedExample.java | SpotBugs Pattern |
|---|---|---|
| Returns internal array directly | Returns `Arrays.copyOf()` | EI_EXPOSE_REP |
| Stores external array directly | Stores `Arrays.copyOf()` | EI_EXPOSE_REP2 |
| Dereferences after null check | Returns early when null | NP_NULL_ON_SOME_PATH |
| Compares strings with `==` | Uses `.equals()` | ES_COMPARING_STRINGS_WITH_EQ |
| equals() without hashCode() | Implements both | HE_EQUALS_NO_HASHCODE |

### How to run manually

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

---

## Running Both Demos Together

A single script runs Demo 1 and Demo 2 in sequence. It copies the files to `C:\SpotBugsClassDemo` (ASCII-only path) to avoid Windows encoding issues.

### From terminal

```powershell
powershell -ExecutionPolicy Bypass -File tools/run-spotbugs-two-demos.ps1
```

### Inside VS Code

1. Go to **Terminal** in the menu bar
2. Select **Run Task...**
3. Choose **Run SpotBugs Two Demos**

### Expected output summary

```
  Demo 1  : SpotBugs report generation     -> COMPLETED
  Demo 2a : Buggy code spotbugs:check      -> FAILED (as expected)
  Demo 2b : Fixed code spotbugs:check      -> PASSED (as expected)
```

---

## VS Code Integration

### Recommended extensions

Install via script:

```powershell
powershell -ExecutionPolicy Bypass -File tools/install-vscode-spotbugs-extension.ps1
```

Or install manually in VS Code (`Ctrl+Shift+X`):

- `shblue21.vscode-spotbugs` -- SpotBugs
- `redhat.java` -- Language Support for Java (Red Hat)
- `vscjava.vscode-java-pack` -- Java Extension Pack

### VS Code tasks

- **Run SpotBugs Demo** -- runs Demo 1 only
- **Run SpotBugs Two Demos** -- runs Demo 1 + Demo 2

---

## Prerequisites

- **JDK 8** (or newer) with `java` on PATH
- **Apache Maven** with `mvn` on PATH

---

## Presentation Script

> "This is a C# project, but SpotBugs is a Java bytecode code-quality analysis tool. It cannot analyze C# source code directly. To demonstrate SpotBugs correctly, we added small Java sidecar modules inside the repository with intentional simple programming mistakes. Demo 1 shows SpotBugs report generation -- it compiles the Java code and produces an XML report listing the findings. Demo 2 shows SpotBugs as a build quality gate -- the buggy version fails the SpotBugs check, and the fixed version passes after the mistakes are corrected. Both demos use SpotBugs only, with no other analyzers."
