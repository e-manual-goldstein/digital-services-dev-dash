# Coverlet JSON samples

Sample files for **Coverlet Viewer** parser tests and manual upload demos.

## Formats supported

### Summary format (`coverage.sample.json`)

Module → `Classes` → `Methods` with a `Summary` block (common in newer Coverlet JSON output).

### Legacy file format (`coverage.legacy.sample.json`)

Module (`.dll`) → source **file** → **type** → **method** (`TypeName::MethodName()`) with `Lines` and `Branches` hit maps. Metrics are derived from those maps when `Summary` is absent.

## Files

| File | Format |
|------|--------|
| `coverage.sample.json` | Summary / Classes / Methods |
| `coverage.legacy.sample.json` | Module → file → type → method → Lines / Branches |

Generate a real report locally:

```bash
dotnet test --collect:"XPlat Code Coverage" /p:CoverletOutputFormat=json
```

(Exact output path and shape depend on your Coverlet version and settings.)
