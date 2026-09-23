# Coverlet JSON samples

Sample files for **Coverlet Viewer** parser tests and manual upload demos.

## `coverage.sample.json`

Minimal report in the standard Coverlet JSON shape: root object keyed by module/assembly name, each module containing `Classes` → `Methods` with `Summary` metrics.

Generate a real report locally:

```bash
dotnet test --collect:"XPlat Code Coverage" /p:CoverletOutputFormat=json
```

(Exact output path depends on your test project and Coverlet settings.)
