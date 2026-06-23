# SpotBugs Demo 1 -- Report Generation (Java Sidecar)

A minimal Maven Java project that demonstrates [SpotBugs](https://spotbugs.github.io/), a code-quality analysis tool for Java bytecode.

## Why a Java project inside a C# repository?

SpotBugs analyzes **compiled Java bytecode** (`.class` files). It cannot analyze C# source files (`.cs`) or .NET assemblies. This Java sidecar exists solely to provide a correct, working classroom demonstration of SpotBugs.

## JDK 8 Compatibility

- SpotBugs Maven Plugin: `4.8.5.0` (compatible with JDK 8)
- Java source/target: `1.8`
- Encoding: `UTF-8`

Newer SpotBugs Maven Plugin versions (4.9.x+) may require JDK 11 or newer.

## Intentional Mistakes in SpotBugsDemo.java

| Mistake | SpotBugs Pattern | Description |
|---|---|---|
| Return internal array | EI_EXPOSE_REP | `getScores()` returns mutable internal array |
| Store external array | EI_EXPOSE_REP2 | `setTags()` stores external array reference directly |
| equals without hashCode | HE_EQUALS_NO_HASHCODE | Overrides `equals()` but not `hashCode()` |
| String == comparison | ES_COMPARING_STRINGS_WITH_EQ | Compares strings with `==` instead of `.equals()` |
| Null dereference | NP_NULL_ON_SOME_PATH | Dereferences variable after null check path |

## How to Run

```bash
cd tools/spotbugs-demo
mvn clean compile spotbugs:spotbugs
```

Or using the combined script from the repo root:

```powershell
powershell -ExecutionPolicy Bypass -File tools/run-spotbugs-two-demos.ps1
```

## Report Output

- XML report: `target/spotbugsXml.xml`

## Analyzers Used

**SpotBugs only.** No Checkstyle, PMD, SonarQube, Roslyn, StyleCop, or ReSharper.
